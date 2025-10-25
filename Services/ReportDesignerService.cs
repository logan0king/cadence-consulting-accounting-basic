using Microsoft.EntityFrameworkCore;
using CadenceAccounting.Data;
using CadenceAccounting.Models;
using Dapper;
using System.Data.SqlClient;
using CadenceAccounting.Services.ReportGeneration;
using System.Text.Json;

namespace CadenceAccounting.Services
{
    public interface IReportDesignerService
    {
        Task<List<DataSourceInfo>> GetDataSourcesAsync();
        Task<List<FieldInfo>> GetFieldsAsync(string sourceName);
        Task<ReportDefinition> SaveReportAsync(ReportDefinition report);
        Task<ReportDefinition?> GetReportAsync(Guid id);
        Task<bool> DeleteReportAsync(Guid id);
        Task<List<ReportDefinition>> GetReportsAsync();
        Task SaveReportComponentsAsync(Guid reportId, List<JsonElement> components);
        Task SaveReportRelationshipsAsync(Guid reportId, List<JsonElement> relationships);
        
        // Report Generation Methods
        Task<string> GenerateSqlAsync(Guid reportId);
        Task<List<Dictionary<string, object>>> ExecuteReportAsync(Guid reportId, Dictionary<string, object>? parameters = null);
        Task<string> ProcessReportDataAsync(Guid reportId, Dictionary<string, object>? parameters = null);
    }

    public class ReportDesignerService : IReportDesignerService
    {
        private readonly ApplicationDbContext _context;
        private readonly string _connectionString;

        public ReportDesignerService(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string not found");
        }

        public async Task<List<DataSourceInfo>> GetDataSourcesAsync()
        {
            var dataSources = new List<DataSourceInfo>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Get all tables
                var tables = await connection.QueryAsync<TableInfo>(@"
                    SELECT TABLE_NAME as Name, 'table' as Type
                    FROM INFORMATION_SCHEMA.TABLES 
                    WHERE TABLE_TYPE = 'BASE TABLE' 
                    AND TABLE_NAME NOT LIKE 'Report%'
                    ORDER BY TABLE_NAME");

                foreach (var table in tables)
                {
                    var fields = await GetFieldsAsync(table.Name);
                    dataSources.Add(new DataSourceInfo
                    {
                        Name = table.Name,
                        Type = "table",
                        DisplayName = FormatDisplayName(table.Name),
                        Fields = fields
                    });
                }

                // Get all views
                var views = await connection.QueryAsync<ViewInfo>(@"
                    SELECT TABLE_NAME as Name, 'view' as Type
                    FROM INFORMATION_SCHEMA.VIEWS 
                    ORDER BY TABLE_NAME");

                foreach (var view in views)
                {
                    var fields = await GetFieldsAsync(view.Name);
                    dataSources.Add(new DataSourceInfo
                    {
                        Name = view.Name,
                        Type = "view",
                        DisplayName = FormatDisplayName(view.Name),
                        Fields = fields
                    });
                }
            }

            return dataSources;
        }

        public async Task<List<FieldInfo>> GetFieldsAsync(string sourceName)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var fields = await connection.QueryAsync<FieldInfo>(@"
                    SELECT 
                        COLUMN_NAME as Name,
                        DATA_TYPE as DataType,
                        CASE WHEN IS_NULLABLE = 'YES' THEN 1 ELSE 0 END as IsNullable,
                        COLUMN_DEFAULT as DefaultValue,
                        CHARACTER_MAXIMUM_LENGTH as MaxLength,
                        NUMERIC_PRECISION as Precision,
                        NUMERIC_SCALE as Scale
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_NAME = @sourceName
                    ORDER BY ORDINAL_POSITION", 
                    new { sourceName });

                return fields.ToList();
            }
        }

        public async Task<ReportDefinition> SaveReportAsync(ReportDefinition report)
        {
            if (report.Id == Guid.Empty)
            {
                report.Id = Guid.NewGuid();
                report.CreatedAt = DateTime.UtcNow;
                _context.ReportDefinitions.Add(report);
            }
            else
            {
                report.UpdatedAt = DateTime.UtcNow;
                _context.ReportDefinitions.Update(report);
            }

            await _context.SaveChangesAsync();
            return report;
        }

        public async Task SaveReportComponentsAsync(Guid reportId, List<System.Text.Json.JsonElement> components)
        {
            // Remove existing components for this report
            var existingComponents = await _context.ReportComponents
                .Where(c => c.ReportId == reportId)
                .ToListAsync();
            _context.ReportComponents.RemoveRange(existingComponents);

            // Add new components
            foreach (var componentJson in components)
            {
                var component = new ReportComponent
                {
                    Id = componentJson.TryGetProperty("id", out var idElement) ? 
                         Guid.Parse(idElement.GetString()!) : Guid.NewGuid(),
                    ReportId = reportId,
                    ComponentType = componentJson.GetProperty("componentType").GetString()!,
                    PositionX = componentJson.GetProperty("positionX").GetInt32(),
                    PositionY = componentJson.GetProperty("positionY").GetInt32(),
                    Width = componentJson.GetProperty("width").GetInt32(),
                    Height = componentJson.GetProperty("height").GetInt32(),
                    Properties = componentJson.TryGetProperty("properties", out var propsElement) ? 
                                propsElement.GetString() : null,
                    DataBinding = componentJson.TryGetProperty("dataBinding", out var bindingElement) ? 
                                 bindingElement.GetString() : null,
                    ZIndex = componentJson.TryGetProperty("zIndex", out var zIndexElement) ? 
                            zIndexElement.GetInt32() : 0,
                    IsVisible = componentJson.TryGetProperty("isVisible", out var visibleElement) ? 
                               visibleElement.GetBoolean() : true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.ReportComponents.Add(component);
            }

            await _context.SaveChangesAsync();
        }

        public async Task SaveReportRelationshipsAsync(Guid reportId, List<System.Text.Json.JsonElement> relationships)
        {
            // Remove existing relationships for this report
            var existingRelationships = await _context.ReportRelationships
                .Where(r => r.ReportId == reportId)
                .ToListAsync();
            _context.ReportRelationships.RemoveRange(existingRelationships);

            // Add new relationships
            foreach (var relationshipJson in relationships)
            {
                var relationship = new ReportRelationship
                {
                    Id = relationshipJson.TryGetProperty("id", out var idElement) ? 
                         Guid.Parse(idElement.GetString()!) : Guid.NewGuid(),
                    ReportId = reportId,
                    FromDataSourceId = Guid.Parse(relationshipJson.GetProperty("fromDataSourceId").GetString()!),
                    ToDataSourceId = Guid.Parse(relationshipJson.GetProperty("toDataSourceId").GetString()!),
                    FromField = relationshipJson.GetProperty("fromField").GetString()!,
                    ToField = relationshipJson.GetProperty("toField").GetString()!,
                    JoinType = relationshipJson.TryGetProperty("joinType", out var joinElement) ? 
                              joinElement.GetString()! : "INNER",
                    IsActive = relationshipJson.TryGetProperty("isActive", out var activeElement) ? 
                              activeElement.GetBoolean() : true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.ReportRelationships.Add(relationship);
            }

            await _context.SaveChangesAsync();
        }

        public async Task<ReportDefinition?> GetReportAsync(Guid id)
        {
            return await _context.ReportDefinitions
                .Include(r => r.DataSources)
                    .ThenInclude(ds => ds.Fields)
                .Include(r => r.Relationships)
                .Include(r => r.Components)
                .Include(r => r.Filters)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<bool> DeleteReportAsync(Guid id)
        {
            var report = await _context.ReportDefinitions.FindAsync(id);
            if (report == null) return false;

            _context.ReportDefinitions.Remove(report);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<ReportDefinition>> GetReportsAsync()
        {
            return await _context.ReportDefinitions
                .Where(r => r.IsActive)
                .OrderBy(r => r.ReportGroup)
                .ThenBy(r => r.Title)
                .ToListAsync();
        }

        // Report Generation Methods
        public async Task<string> GenerateSqlAsync(Guid reportId)
        {
            var report = await GetReportAsync(reportId);
            if (report == null)
                throw new ArgumentException("Report not found", nameof(reportId));

            var components = report.Components?.ToList() ?? new List<ReportComponent>();
            var relationships = report.Relationships?.ToList() ?? new List<ReportRelationship>();

            var sqlBuilder = new SqlQueryBuilder(_context);
            return await sqlBuilder.BuildQueryAsync(report, components, relationships);
        }

        public async Task<List<Dictionary<string, object>>> ExecuteReportAsync(Guid reportId, Dictionary<string, object>? parameters = null)
        {
            var report = await GetReportAsync(reportId);
            if (report == null)
                throw new ArgumentException("Report not found", nameof(reportId));

            var sqlQuery = await GenerateSqlAsync(reportId);
            var sqlBuilder = new SqlQueryBuilder(_context);
            return await sqlBuilder.ExecuteQueryAsync(sqlQuery);
        }

        public async Task<string> ProcessReportDataAsync(Guid reportId, Dictionary<string, object>? parameters = null)
        {
            var report = await GetReportAsync(reportId);
            if (report == null)
                throw new ArgumentException("Report not found", nameof(reportId));

            var data = await ExecuteReportAsync(reportId, parameters);
            var components = report.Components?.ToList() ?? new List<ReportComponent>();

            var dataProcessor = new ReportDataProcessor();
            return await dataProcessor.ProcessReportDataAsync(report, data, components);
        }

        private string FormatDisplayName(string name)
        {
            // Convert PascalCase or snake_case to readable format
            return System.Text.RegularExpressions.Regex.Replace(
                name.Replace("_", " "),
                "([a-z])([A-Z])",
                "$1 $2"
            );
        }
    }

    public class DataSourceInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public List<FieldInfo> Fields { get; set; } = new List<FieldInfo>();
    }

    public class FieldInfo
    {
        public string Name { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public bool IsNullable { get; set; }
        public string? DefaultValue { get; set; }
        public int? MaxLength { get; set; }
        public int? Precision { get; set; }
        public int? Scale { get; set; }
    }

    public class TableInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }

    public class ViewInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }

    // Report Generation Models
    public class ReportData
    {
        public List<Dictionary<string, object>> Rows { get; set; } = new List<Dictionary<string, object>>();
        public List<ColumnInfo> Columns { get; set; } = new List<ColumnInfo>();
        public int TotalRows { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public string SqlQuery { get; set; } = string.Empty;
    }

    public class ColumnInfo
    {
        public string Name { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public bool IsNullable { get; set; }
        public int MaxLength { get; set; }
    }

    public class ReportPreview
    {
        public string HtmlContent { get; set; } = string.Empty;
        public byte[]? PdfContent { get; set; }
        public ReportData Data { get; set; } = new ReportData();
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    }

    public class SqlGenerationResult
    {
        public string SqlQuery { get; set; } = string.Empty;
        public List<string> Parameters { get; set; } = new List<string>();
        public List<string> DataSources { get; set; } = new List<string>();
        public List<string> Fields { get; set; } = new List<string>();
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
