using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CadenceAccounting.Services;
using CadenceAccounting.Models;
using Microsoft.EntityFrameworkCore;
using CadenceAccounting.Data;

namespace CadenceAccounting.Pages.Reports.Designer
{
    public class IndexModel : PageModel
    {
        private readonly IReportDesignerService _reportDesignerService;
        private readonly IReportExportService _reportExportService;
        private readonly ApplicationDbContext _context;

        public IndexModel(IReportDesignerService reportDesignerService, IReportExportService reportExportService, ApplicationDbContext context)
        {
            _reportDesignerService = reportDesignerService;
            _reportExportService = reportExportService;
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

                // Convert components to a format the frontend can use
                var componentsList = new List<object>();
                if (report.Components != null)
                {
                    foreach (var c in report.Components)
                    {
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

                return new JsonResult(new
                {
                    success = true,
                    id = report.Id,
                    title = report.Title,
                    description = report.Description,
                    reportGroup = report.ReportGroup,
                    components = componentsList
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
