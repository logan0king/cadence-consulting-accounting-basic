using Microsoft.AspNetCore.Mvc;
using CadenceAccounting.Data;
using CadenceAccounting.Models;
using System.Text.Json.Serialization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Data.Common;
using System.Text.RegularExpressions;

namespace CadenceAccounting.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportWizardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ReportWizardController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("data/schema")]
        public async Task<IActionResult> GetSchema()
        {
            try
            {
                await using var connection = _context.Database.GetDbConnection();
                if (connection.State != ConnectionState.Open)
                {
                    await connection.OpenAsync();
                }

                string provider = _context.Database.ProviderName ?? string.Empty;
                string sql;
                if (provider.Contains("SqlServer", StringComparison.OrdinalIgnoreCase))
                {
                    sql = @"
                        SELECT c.TABLE_SCHEMA as SchemaName, c.TABLE_NAME as TableName, c.COLUMN_NAME as ColumnName, c.DATA_TYPE as DataType
                        FROM INFORMATION_SCHEMA.COLUMNS c
                        JOIN INFORMATION_SCHEMA.TABLES t ON t.TABLE_SCHEMA = c.TABLE_SCHEMA AND t.TABLE_NAME = c.TABLE_NAME
                        WHERE t.TABLE_TYPE = 'BASE TABLE'
                        ORDER BY c.TABLE_SCHEMA, c.TABLE_NAME, c.ORDINAL_POSITION";
                }
                else if (provider.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) || provider.Contains("Postgre", StringComparison.OrdinalIgnoreCase))
                {
                    sql = @"
                        SELECT c.table_schema as SchemaName, c.table_name as TableName, c.column_name as ColumnName, c.data_type as DataType
                        FROM information_schema.columns c
                        JOIN information_schema.tables t ON t.table_schema = c.table_schema AND t.table_name = c.table_name
                        WHERE t.table_type = 'BASE TABLE' AND c.table_schema NOT IN ('pg_catalog', 'information_schema')
                        ORDER BY c.table_schema, c.table_name, c.ordinal_position";
                }
                else if (provider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
                {
                    // SQLite: build from PRAGMA
                    var tables = new List<(string Schema, string Table)>();
                    await using (var cmdTables = connection.CreateCommand())
                    {
                        cmdTables.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name";
                        await using var rdr = await cmdTables.ExecuteReaderAsync();
                        while (await rdr.ReadAsync())
                        {
                            tables.Add(("main", rdr.GetString(0)));
                        }
                    }

                    var rows = new List<object>();
                    foreach (var t in tables)
                    {
                        await using var cmd = connection.CreateCommand();
                        cmd.CommandText = $"PRAGMA table_info([{t.Table}])";
                        await using var rr = await cmd.ExecuteReaderAsync();
                        while (await rr.ReadAsync())
                        {
                            rows.Add(new { SchemaName = t.Schema, TableName = t.Table, ColumnName = rr.GetString(1), DataType = rr.GetString(2) });
                        }
                    }

                    // Group into shape
                    var resultSqlite = rows
                        .GroupBy(r => (r.GetType().GetProperty("SchemaName")!.GetValue(r)!.ToString(), r.GetType().GetProperty("TableName")!.GetValue(r)!.ToString()))
                        .Select(g => new {
                            schema = g.Key.Item1,
                            table = g.Key.Item2,
                            columns = g.Select(r => new {
                                name = r.GetType().GetProperty("ColumnName")!.GetValue(r)!.ToString(),
                                dataType = r.GetType().GetProperty("DataType")!.GetValue(r)!.ToString()
                            }).ToList()
                        });

                    return Ok(new { success = true, items = resultSqlite });
                }
                else
                {
                    return Ok(new { success = true, items = Array.Empty<object>() });
                }

                await using var cmdInfo = connection.CreateCommand();
                cmdInfo.CommandText = sql;
                await using var reader = await cmdInfo.ExecuteReaderAsync();

                var records = new List<(string Schema, string Table, string Column, string Type)>();
                while (await reader.ReadAsync())
                {
                    records.Add((
                        reader.GetString(0),
                        reader.GetString(1),
                        reader.GetString(2),
                        reader.GetString(3)
                    ));
                }

                var items = records
                    .GroupBy(r => (r.Schema, r.Table))
                    .Select(g => new {
                        schema = g.Key.Schema,
                        table = g.Key.Table,
                        columns = g.Select(r => new { name = r.Column, dataType = r.Type }).ToList()
                    })
                    .OrderBy(x => x.schema).ThenBy(x => x.table)
                    .ToList();

                return Ok(new { success = true, items });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateReport([FromBody] WizardData data)
        {
            try
            {
                Console.WriteLine($"CreateReport called. Data: {JsonSerializer.Serialize(data)}");
                
                if (data == null)
                {
                    return BadRequest(new { success = false, error = "No data provided" });
                }

                // Get the first admin user
                var userId = _context.Users
                    .Where(u => u.Username == "admin")
                    .Select(u => u.Id)
                    .FirstOrDefault();
                
                Console.WriteLine($"Current user ID: {userId}");

                if (userId == Guid.Empty)
                {
                    return BadRequest(new { success = false, error = "Admin user not found" });
                }

                var report = new ReportDefinition
                {
                    Id = Guid.NewGuid(),
                    Title = data.Title,
                    Description = data.Description ?? string.Empty,
                    ReportGroup = data.Group ?? "General",
                    CreatedBy = userId,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _context.ReportDefinitions.AddAsync(report);
                await _context.SaveChangesAsync();

                Console.WriteLine($"Report created successfully. ID: {report.Id}");

                // Create components based on wizard data
                var components = new List<ReportComponent>();
                
                // Add title component
                var titleComponent = new ReportComponent
                {
                    Id = Guid.NewGuid(),
                    ReportId = report.Id,
                    ComponentType = "label",
                    PositionX = 50,
                    PositionY = 50,
                    Width = 400,
                    Height = 30,
                    Properties = JsonSerializer.Serialize(new
                    {
                        Text = data.Title,
                        FontSize = 18,
                        FontWeight = "bold",
                        TextAlign = "left"
                    }),
                    ZIndex = 1,
                    IsVisible = true,
                    CreatedAt = DateTime.UtcNow
                };
                components.Add(titleComponent);

                // Add table component for the selected data source
                if (!string.IsNullOrEmpty(data.DataSource) && data.Fields != null && data.Fields.Count > 0)
                {
                    var tableComponent = new ReportComponent
                    {
                        Id = Guid.NewGuid(),
                        ReportId = report.Id,
                        ComponentType = "table",
                        PositionX = 50,
                        PositionY = 100,
                        Width = 700,
                        Height = 200,
                        Properties = JsonSerializer.Serialize(new
                        {
                            DataSource = data.DataSource,
                            Fields = data.Fields,
                            ShowHeader = true,
                            AlternatingRowColor = "#f5f5f5"
                        }),
                        DataBinding = JsonSerializer.Serialize(new
                        {
                            DataSource = data.DataSource,
                            Fields = data.Fields
                        }),
                        ZIndex = 0,
                        IsVisible = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    components.Add(tableComponent);
                }

                // Save components
                if (components.Any())
                {
                    await _context.ReportComponents.AddRangeAsync(components);
                    await _context.SaveChangesAsync();
                    Console.WriteLine($"Created {components.Count} components");
                }

                return Ok(new { success = true, reportId = report.Id });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CreateReport: {ex.Message}\n{ex.StackTrace}");
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        [HttpPost("data/preview-sql")]
        public async Task<IActionResult> PreviewSql([FromBody] SqlRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Sql))
            {
                return BadRequest(new { success = false, error = "SQL is required" });
            }

            // Guardrails: only allow SELECT
            var sqlTrim = request.Sql.TrimStart();
            if (!sqlTrim.StartsWith("select", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { success = false, error = "Only SELECT statements are allowed." });
            }

            var maxRows = Math.Min(request.MaxRows > 0 ? request.MaxRows : 100, 1000);

            try
            {
                await using var connection = _context.Database.GetDbConnection();
                if (connection.State != ConnectionState.Open)
                {
                    await connection.OpenAsync();
                }

                // Apply a row cap if caller didn't include one. We won't rewrite complex queries;
                // instead we wrap as a subquery with provider-specific limit syntax.
                string provider = _context.Database.ProviderName ?? string.Empty;
                string wrappedSql;
                if (provider.Contains("SqlServer", StringComparison.OrdinalIgnoreCase))
                {
                    // SQL Server: TOP N
                    wrappedSql = $"select top ({maxRows}) * from ( {request.Sql} ) as sub";
                }
                else if (provider.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) || provider.Contains("Postgre", StringComparison.OrdinalIgnoreCase))
                {
                    // PostgreSQL: LIMIT N
                    wrappedSql = $"select * from ( {request.Sql} ) as sub limit {maxRows}";
                }
                else if (provider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
                {
                    // SQLite: LIMIT N
                    wrappedSql = $"select * from ( {request.Sql} ) as sub limit {maxRows}";
                }
                else
                {
                    // Fallback: do not wrap; execute directly (caller must self-limit)
                    wrappedSql = request.Sql;
                }

                await using var cmd = connection.CreateCommand();
                cmd.CommandText = wrappedSql;
                cmd.CommandType = CommandType.Text;

                if (request.Parameters != null)
                {
                    foreach (var p in request.Parameters)
                    {
                        var dbParam = cmd.CreateParameter();
                        dbParam.ParameterName = p.Name;
                        dbParam.Value = p.Value ?? DBNull.Value;
                        cmd.Parameters.Add(dbParam);
                    }
                }

                await using var reader = await cmd.ExecuteReaderAsync();

                var columns = new List<string>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    columns.Add(reader.GetName(i));
                }

                var rows = new List<List<object?>>();
                while (await reader.ReadAsync())
                {
                    var row = new List<object?>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        row.Add(reader.IsDBNull(i) ? null : reader.GetValue(i));
                    }
                    rows.Add(row);
                }

                return Ok(new { success = true, columns, rows, rowCount = rows.Count });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        public class SqlParameterDto
        {
            public string Name { get; set; } = string.Empty;
            public object? Value { get; set; }
        }

        public class SqlRequest
        {
            public string Sql { get; set; } = string.Empty;
            public int MaxRows { get; set; } = 100;
            public List<SqlParameterDto>? Parameters { get; set; }
        }

        public class WizardData
        {
            [JsonPropertyName("template")]
            public string Template { get; set; } = string.Empty;
            
            [JsonPropertyName("dataSource")]
            public string DataSource { get; set; } = string.Empty;
            
            [JsonPropertyName("fields")]
            public List<string> Fields { get; set; } = new();
            
            [JsonPropertyName("title")]
            public string Title { get; set; } = string.Empty;
            
            [JsonPropertyName("description")]
            public string Description { get; set; } = string.Empty;
            
            [JsonPropertyName("group")]
            public string Group { get; set; } = string.Empty;
        }
    }
}

