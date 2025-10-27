using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CadenceAccounting.Services;
using CadenceAccounting.Models;
using CadenceAccounting.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Data.SqlClient;

namespace CadenceAccounting.Pages.Reports.Viewer
{
    public class IndexModel : PageModel
    {
        private readonly IReportDesignerService _reportDesignerService;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        
        public IndexModel(
            IReportDesignerService reportDesignerService,
            ApplicationDbContext context,
            IConfiguration configuration)
        {
            _reportDesignerService = reportDesignerService;
            _context = context;
            _configuration = configuration;
        }
        
        public ReportDefinition? Report { get; set; }
        public List<Dictionary<string, object>> ReportData { get; set; } = new();
        
        public async Task OnGetAsync(Guid reportId)
        {
            try
            {
                // Get report
                Report = await _reportDesignerService.GetReportAsync(reportId);
                
                if (Report == null)
                {
                    TempData["ErrorMessage"] = "Report not found";
                    return;
                }
                
                // Get components with data bindings
                var components = await _context.ReportComponents
                    .Where(c => c.ReportId == reportId)
                    .ToListAsync();
                
                // Execute SQL for the report's data source
                var sqlQuery = GenerateSqlQuery(components);
                
                Console.WriteLine($"Generated SQL: {sqlQuery}");
                
                // Execute query
                ReportData = await ExecuteQueryAsync(sqlQuery);
                
                Console.WriteLine($"Query returned {ReportData.Count} rows");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading report: {ex.Message}");
                TempData["ErrorMessage"] = $"Error loading report: {ex.Message}";
            }
        }
        
        private string GenerateSqlQuery(List<ReportComponent> components)
        {
            try
            {
                // Extract data sources from components
                var dataSources = new HashSet<string>();
                var bindings = new List<DataBindingInfo>();
                
                foreach (var component in components)
                {
                    if (!string.IsNullOrEmpty(component.DataBinding))
                    {
                        try
                        {
                            var binding = JsonSerializer.Deserialize<DataBindingInfo>(
                                component.DataBinding,
                                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                            );
                            
                            if (binding != null && !string.IsNullOrEmpty(binding.TableName))
                            {
                                dataSources.Add(binding.TableName);
                                bindings.Add(binding);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error deserializing data binding for component {component.Id}: {ex.Message}");
                        }
                    }
                }
                
                if (dataSources.Count == 0)
                {
                    return "SELECT 'No data bindings configured' as Message";
                }
                
                // Get the primary data source
                var primarySource = dataSources.First();
                
                // Build SELECT clause from bindings
                var fields = new List<string>();
                foreach (var binding in bindings)
                {
                    if (binding.TableName == primarySource)
                    {
                        fields.Add($"{binding.TableName}.{binding.FieldName} as {binding.FieldName}");
                    }
                }
                
                if (fields.Count == 0)
                {
                    return $"SELECT * FROM {primarySource}";
                }
                
                return $"SELECT {string.Join(", ", fields)} FROM {primarySource}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating SQL query: {ex.Message}");
                return "SELECT 'Error generating query' as Message";
            }
        }
        
        private async Task<List<Dictionary<string, object>>> ExecuteQueryAsync(string sqlQuery)
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                
                if (string.IsNullOrEmpty(connectionString))
                {
                    Console.WriteLine("Connection string is null or empty");
                    return new List<Dictionary<string, object>>();
                }
                
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();
                
                using var command = new SqlCommand(sqlQuery, connection);
                using var reader = await command.ExecuteReaderAsync();
                
                var results = new List<Dictionary<string, object>>();
                while (await reader.ReadAsync())
                {
                    var row = new Dictionary<string, object>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    }
                    results.Add(row);
                }
                
                return results;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing query: {ex.Message}");
                return new List<Dictionary<string, object>>();
            }
        }
    }
    
    public class DataBindingInfo
    {
        public string TableName { get; set; } = string.Empty;
        public string FieldName { get; set; } = string.Empty;
        public string DataType { get; set; } = "string";
    }
}

