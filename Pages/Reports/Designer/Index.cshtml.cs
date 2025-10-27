using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CadenceAccounting.Services;
using CadenceAccounting.Models;
using Microsoft.EntityFrameworkCore;
using CadenceAccounting.Data;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CadenceAccounting.Pages.Reports.Designer
{
    public class IndexModel : PageModel
    {
        private readonly IReportDesignerService _reportDesignerService;
        private readonly IReportExportService _reportExportService;
        private readonly IReportParameterService _reportParameterService;
        private readonly ApplicationDbContext _context;

        public IndexModel(IReportDesignerService reportDesignerService, IReportExportService reportExportService, IReportParameterService reportParameterService, ApplicationDbContext context)
        {
            _reportDesignerService = reportDesignerService;
            _reportExportService = reportExportService;
            _reportParameterService = reportParameterService;
            _context = context;
        }

        public List<ReportDefinition> Reports { get; set; } = new List<ReportDefinition>();

        public async Task OnGetAsync(Guid? reportId = null)
        {
            Reports = await _reportDesignerService.GetReportsAsync();
            
            if (reportId.HasValue)
            {
                // Load the specific report for editing
                var report = await _reportDesignerService.GetReportAsync(reportId.Value);
                if (report != null)
                {
                    // Set the current report ID for the frontend
                    ViewData["CurrentReportId"] = report.Id;
                    ViewData["CurrentReportTitle"] = report.Title;
                    ViewData["CurrentReportDescription"] = report.Description;
                }
            }
        }

        public async Task<IActionResult> OnGetDataSourcesAsync()
        {
            var dataSources = await _reportDesignerService.GetDataSourcesAsync();
            return new JsonResult(dataSources);
        }

        public async Task<IActionResult> OnGetGetReportAsync(Guid reportId)
        {
            try
            {
                var report = await _reportDesignerService.GetReportAsync(reportId);
                if (report == null)
                {
                    return new JsonResult(new { success = false, error = "Report not found" });
                }

                Console.WriteLine($"OnGetGetReportAsync: Found {report.Components?.Count ?? 0} components");

                // Convert components to a format the frontend can use
                var componentsList = new List<object>();
                if (report.Components != null)
                {
                    foreach (var c in report.Components)
                    {
                        Console.WriteLine($"Component: Type={c.ComponentType}, X={c.PositionX}, Y={c.PositionY}");
                        componentsList.Add(new
                        {
                            id = c.Id,
                            componentType = c.ComponentType,
                            positionX = c.PositionX,
                            positionY = c.PositionY,
                            width = c.Width,
                            height = c.Height,
                            properties = !string.IsNullOrEmpty(c.Properties) ? 
                                System.Text.Json.JsonSerializer.Deserialize<object>(c.Properties) : null,
                            dataBinding = !string.IsNullOrEmpty(c.DataBinding) ? 
                                System.Text.Json.JsonSerializer.Deserialize<object>(c.DataBinding) : null,
                            zIndex = c.ZIndex,
                            isVisible = c.IsVisible
                        });
                    }
                }

                Console.WriteLine($"Returning {componentsList.Count} components to frontend");

                // Parse canvas data if available
                object? canvasDataObj = null;
                if (!string.IsNullOrEmpty(report.CanvasData))
                {
                    try
                    {
                        canvasDataObj = System.Text.Json.JsonSerializer.Deserialize<object>(report.CanvasData);
                    }
                    catch
                    {
                        // Ignore parsing errors, use null
                    }
                }

                return new JsonResult(new
                {
                    success = true,
                    id = report.Id,
                    title = report.Title,
                    description = report.Description,
                    reportGroup = report.ReportGroup,
                    components = componentsList,
                    metadata = new
                    {
                        canvasData = canvasDataObj
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading report: {ex.Message}");
                return new JsonResult(new { success = false, error = ex.Message });
            }
        }

        public async Task<IActionResult> OnGetFieldsAsync(string sourceName)
        {
            var fields = await _reportDesignerService.GetFieldsAsync(sourceName);
            return new JsonResult(fields);
        }

        public async Task<IActionResult> OnGetExecuteQueryAsync(string sql)
        {
            try
            {
                // Execute SQL query and return results
                using var connection = _context.Database.GetDbConnection();
                await connection.OpenAsync();

                using var command = connection.CreateCommand();
                command.CommandText = sql;

                var results = new List<Dictionary<string, object>>();
                using var reader = await command.ExecuteReaderAsync();
                
                while (await reader.ReadAsync())
                {
                    var row = new Dictionary<string, object>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        row[reader.GetName(i)] = reader.GetValue(i) ?? DBNull.Value;
                    }
                    results.Add(row);
                }

                return new JsonResult(new { success = true, data = results });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing query: {ex.Message}");
                return new JsonResult(new { success = false, error = ex.Message });
            }
        }

        public async Task<IActionResult> OnGetDatabaseSchemaAsync()
        {
            try
            {
                // Get database schema using ADO.NET
                using var connection = _context.Database.GetDbConnection();
                await connection.OpenAsync();

                var tables = new List<Dictionary<string, object>>();
                
                // Get table names from INFORMATION_SCHEMA
                using var tablesCommand = connection.CreateCommand();
                tablesCommand.CommandText = @"
                    SELECT TABLE_NAME 
                    FROM INFORMATION_SCHEMA.TABLES 
                    WHERE TABLE_TYPE = 'BASE TABLE' 
                    ORDER BY TABLE_NAME";

                using var reader = await tablesCommand.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var tableName = reader.GetString(0);
                    
                    // Get columns for this table
                    using var columnsCommand = connection.CreateCommand();
                    columnsCommand.CommandText = @"
                        SELECT COLUMN_NAME, DATA_TYPE 
                        FROM INFORMATION_SCHEMA.COLUMNS 
                        WHERE TABLE_NAME = @tableName 
                        ORDER BY ORDINAL_POSITION";
                    
                    var tableParam = columnsCommand.CreateParameter();
                    tableParam.ParameterName = "@tableName";
                    tableParam.Value = tableName;
                    columnsCommand.Parameters.Add(tableParam);
                    
                    var columns = new List<Dictionary<string, string>>();
                    using var columnReader = await columnsCommand.ExecuteReaderAsync();
                    while (await columnReader.ReadAsync())
                    {
                        columns.Add(new Dictionary<string, string>
                        {
                            { "name", columnReader.GetString(0) },
                            { "type", columnReader.GetString(1) }
                        });
                    }
                    
                    tables.Add(new Dictionary<string, object>
                    {
                        { "name", tableName },
                        { "columns", columns }
                    });
                }
                
                return new JsonResult(tables);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching database schema: {ex.Message}");
                return new JsonResult(new { error = ex.Message });
            }
        }

        public async Task<IActionResult> OnPostCreateReportAsync(string title, string description, string reportGroup)
        {
            try
            {
                Console.WriteLine($"CreateReport called with: title='{title}', description='{description}', reportGroup='{reportGroup}'");
                
                var report = new ReportDefinition
                {
                    Title = title ?? "New Report",
                    Description = description ?? "Created from Report Designer",
                    ReportGroup = reportGroup ?? "General",
                    CreatedBy = GetCurrentUserId(),
                    IsActive = true
                };

                Console.WriteLine($"About to save report: {report.Title}");
                var createdReport = await _reportDesignerService.SaveReportAsync(report);
                Console.WriteLine($"Report saved successfully with ID: {createdReport.Id}");
                return new JsonResult(new { success = true, reportId = createdReport.Id });
            }
            catch (Exception ex)
            {
                // Log the full exception details for debugging
                Console.WriteLine($"Error creating report: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                return new JsonResult(new { success = false, error = ex.Message });
            }
        }


        public async Task<IActionResult> OnPostSaveReportAsync(string reportId, string reportData)
        {
            try
            {
                Console.WriteLine("SaveReport handler called");
                Console.WriteLine($"Report ID parameter: {reportId}");
                Console.WriteLine($"Report data parameter length: {reportData?.Length ?? 0}");
                
                if (string.IsNullOrEmpty(reportId) || !Guid.TryParse(reportId, out var parsedReportId))
                {
                    Console.WriteLine("Invalid report ID");
                    return new JsonResult(new { success = false, error = "Invalid report ID" });
                }
                
                Console.WriteLine($"Parsed Report ID: {parsedReportId}");
                
                if (string.IsNullOrEmpty(reportData))
                {
                    Console.WriteLine("Report data is missing");
                    return new JsonResult(new { success = false, error = "Report data is required" });
                }

                var report = await _reportDesignerService.GetReportAsync(parsedReportId);
                if (report == null)
                {
                    Console.WriteLine($"Report not found: {parsedReportId}");
                    return new JsonResult(new { success = false, error = "Report not found" });
                }

                Console.WriteLine($"Report found: {report.Title}");

                // Parse the report data JSON
                using var reportDataDoc = System.Text.Json.JsonDocument.Parse(reportData);
                var reportDataObj = reportDataDoc.RootElement;
                
                // Update report metadata
                if (reportDataObj.TryGetProperty("title", out var titleElement))
                {
                    report.Title = titleElement.GetString() ?? report.Title;
                }
                if (reportDataObj.TryGetProperty("description", out var descElement))
                {
                    report.Description = descElement.GetString() ?? report.Description;
                }
                
                // Save canvas data if provided
                if (reportDataObj.TryGetProperty("canvasData", out var canvasDataElement))
                {
                    report.CanvasData = canvasDataElement.GetRawText();
                }

                // Save components if provided
                if (reportDataObj.TryGetProperty("components", out var componentsElement))
                {
                    var components = componentsElement.EnumerateArray().ToList();
                    await _reportDesignerService.SaveReportComponentsAsync(parsedReportId, components);
                }

                // Save relationships if provided
                if (reportDataObj.TryGetProperty("relationships", out var relationshipsElement))
                {
                    var relationships = relationshipsElement.EnumerateArray().ToList();
                    await _reportDesignerService.SaveReportRelationshipsAsync(parsedReportId, relationships);
                }

                report.UpdatedAt = DateTime.UtcNow;
                await _reportDesignerService.SaveReportAsync(report);
                
                Console.WriteLine("Report saved successfully");
                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SaveReport handler: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return new JsonResult(new { success = false, error = ex.Message });
            }
        }

        // Export API endpoints
        public async Task<IActionResult> OnGetExportPdfAsync(Guid reportId)
        {
            try
            {
                var report = await _reportDesignerService.GetReportAsync(reportId);
                if (report == null)
                {
                    return new JsonResult(new { success = false, error = "Report not found" });
                }

                // Generate actual report data
                var reportData = await _reportDesignerService.ProcessReportDataAsync(reportId);
                
                var pdfBytes = await _reportExportService.ExportToPdfAsync(reportData, report.Title);
                return File(pdfBytes, "application/pdf", $"{report.Title.Replace(" ", "_")}.pdf");
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, error = ex.Message });
            }
        }

        public async Task<IActionResult> OnGetExportExcelAsync(Guid reportId)
        {
            try
            {
                var report = await _reportDesignerService.GetReportAsync(reportId);
                if (report == null)
                {
                    return new JsonResult(new { success = false, error = "Report not found" });
                }

                // Generate actual report data
                var reportData = await _reportDesignerService.ProcessReportDataAsync(reportId);
                
                var excelBytes = await _reportExportService.ExportToExcelAsync(reportData, report.Title);
                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{report.Title.Replace(" ", "_")}.xlsx");
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, error = ex.Message });
            }
        }

        public async Task<IActionResult> OnGetExportCsvAsync(Guid reportId)
        {
            try
            {
                var report = await _reportDesignerService.GetReportAsync(reportId);
                if (report == null)
                {
                    return new JsonResult(new { success = false, error = "Report not found" });
                }

                // Generate actual report data
                var reportData = await _reportDesignerService.ProcessReportDataAsync(reportId);
                
                var csvBytes = await _reportExportService.ExportToCsvAsync(reportData, report.Title);
                return File(csvBytes, "text/csv", $"{report.Title.Replace(" ", "_")}.csv");
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, error = ex.Message });
            }
        }

        public async Task<IActionResult> OnGetExportHtmlAsync(Guid reportId)
        {
            try
            {
                var report = await _reportDesignerService.GetReportAsync(reportId);
                if (report == null)
                {
                    return new JsonResult(new { success = false, error = "Report not found" });
                }

                // Generate actual report data
                var reportData = await _reportDesignerService.ProcessReportDataAsync(reportId);
                
                var htmlBytes = await _reportExportService.ExportToHtmlAsync(reportData, report.Title);
                return File(htmlBytes, "text/html", $"{report.Title.Replace(" ", "_")}.html");
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, error = ex.Message });
            }
        }

        // Report Generation API endpoints
        public async Task<IActionResult> OnGetGenerateSqlAsync(Guid reportId)
        {
            try
            {
                var sql = await _reportDesignerService.GenerateSqlAsync(reportId);
                return new JsonResult(new { success = true, sql = sql });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, error = ex.Message });
            }
        }

        public async Task<IActionResult> OnGetExecuteReportAsync(Guid reportId)
        {
            try
            {
                var reportData = await _reportDesignerService.ExecuteReportAsync(reportId);
                return new JsonResult(new { success = true, data = reportData });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, error = ex.Message });
            }
        }

        public async Task<IActionResult> OnGetPreviewReportAsync(Guid reportId)
        {
            try
            {
                var reportData = await _reportDesignerService.ProcessReportDataAsync(reportId);
                return new JsonResult(new { success = true, preview = reportData });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, error = ex.Message });
            }
        }

        [BindProperty]
        public string? ComponentId { get; set; }

        [BindProperty(Name = "dataBinding")]
        public string? DataBinding { get; set; }

        public async Task<IActionResult> OnPostSaveDataBindingAsync(string componentId, string? dataBinding = null)
        {
            try
            {
                Console.WriteLine($"SaveDataBinding called - ComponentId: {componentId}, DataBinding: {dataBinding}");
                
                if (string.IsNullOrEmpty(componentId) || !Guid.TryParse(componentId, out Guid componentGuid))
                {
                    return new JsonResult(new { success = false, error = "Invalid component ID" });
                }

                var component = await _context.ReportComponents.FindAsync(componentGuid);
                if (component == null)
                {
                    return new JsonResult(new { success = false, error = "Component not found" });
                }

                if (dataBinding != null && dataBinding != "null" && !string.IsNullOrEmpty(dataBinding))
                {
                    component.DataBinding = dataBinding;
                }
                else
                {
                    component.DataBinding = null;
                }

                await _context.SaveChangesAsync();
                Console.WriteLine("Data binding saved successfully");
                
                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving data binding: {ex.Message}");
                return new JsonResult(new { success = false, error = ex.Message });
            }
        }

        // Parameter handlers
        public async Task<IActionResult> OnGetParametersAsync(Guid reportId)
        {
            try
            {
                var parameters = await _reportParameterService.GetParametersAsync(reportId);
                return new JsonResult(parameters);
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, error = ex.Message });
            }
        }

        public async Task<IActionResult> OnGetParameterAsync(Guid parameterId)
        {
            try
            {
                var parameter = await _reportParameterService.GetParameterAsync(parameterId);
                return new JsonResult(parameter);
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, error = ex.Message });
            }
        }

        public async Task<IActionResult> OnPostSaveParameterAsync([FromBody] ReportParameter parameter)
        {
            try
            {
                var savedParameter = await _reportParameterService.SaveParameterAsync(parameter);
                return new JsonResult(new { success = true, parameter = savedParameter });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, error = ex.Message });
            }
        }

        public async Task<IActionResult> OnPostDeleteParameterAsync(Guid parameterId)
        {
            try
            {
                await _reportParameterService.DeleteParameterAsync(parameterId);
                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, error = ex.Message });
            }
        }

        // Filter handlers
        public async Task<IActionResult> OnGetFiltersAsync(Guid reportId)
        {
            try
            {
                var filters = await _context.ReportFilters
                    .Where(f => f.ReportId == reportId)
                    .OrderBy(f => f.SortOrder)
                    .ToListAsync();
                return new JsonResult(filters);
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, error = ex.Message });
            }
        }

        public async Task<IActionResult> OnGetFilterAsync(Guid filterId)
        {
            try
            {
                var filter = await _context.ReportFilters.FindAsync(filterId);
                return new JsonResult(filter);
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, error = ex.Message });
            }
        }

        public async Task<IActionResult> OnPostSaveFilterAsync([FromBody] ReportFilter filter)
        {
            try
            {
                if (filter.Id == Guid.Empty)
                {
                    filter.Id = Guid.NewGuid();
                    filter.CreatedAt = DateTime.UtcNow;
                    filter.UpdatedAt = DateTime.UtcNow;
                    _context.ReportFilters.Add(filter);
                }
                else
                {
                    filter.UpdatedAt = DateTime.UtcNow;
                    _context.ReportFilters.Update(filter);
                }
                await _context.SaveChangesAsync();
                return new JsonResult(new { success = true, filter = filter });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, error = ex.Message });
            }
        }

        public async Task<IActionResult> OnPostDeleteFilterAsync(Guid filterId)
        {
            try
            {
                var filter = await _context.ReportFilters.FindAsync(filterId);
                if (filter != null)
                {
                    _context.ReportFilters.Remove(filter);
                    await _context.SaveChangesAsync();
                }
                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, error = ex.Message });
            }
        }

        public async Task<IActionResult> OnPostCreateFromWizardAsync([FromBody] WizardData data)
        {
            try
            {
                Console.WriteLine($"OnPostCreateFromWizardAsync called. Data: {System.Text.Json.JsonSerializer.Serialize(data)}");
                
                if (data == null)
                {
                    return new JsonResult(new { success = false, error = "No data provided" });
                }

                var userId = GetCurrentUserId();
                Console.WriteLine($"Current user ID: {userId}");

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
                return new JsonResult(new { success = true, reportId = report.Id });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in OnPostCreateFromWizardAsync: {ex.Message}\n{ex.StackTrace}");
                return new JsonResult(new { success = false, error = ex.Message });
            }
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

        private Guid GetCurrentUserId()
        {
            // Get the admin user ID from the database
            // For now, we'll get the first active user (which should be the admin)
            var adminUser = _context.Users.FirstOrDefault(u => u.Username == "admin" && u.IsActive);
            if (adminUser != null)
            {
                return adminUser.Id;
            }
            
            // Fallback to a default admin user ID if not found
            return Guid.Parse("00000000-0000-0000-0000-000000000001");
        }
    }
}
