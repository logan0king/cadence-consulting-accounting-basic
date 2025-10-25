using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CadenceAccounting.Services;
using CadenceAccounting.Models;

namespace CadenceAccounting.Pages.Reports.Designer
{
    public class IndexModel : PageModel
    {
        private readonly IReportDesignerService _reportDesignerService;
        private readonly IReportExportService _reportExportService;

        public IndexModel(IReportDesignerService reportDesignerService, IReportExportService reportExportService)
        {
            _reportDesignerService = reportDesignerService;
            _reportExportService = reportExportService;
        }

        public List<ReportDefinition> Reports { get; set; } = new List<ReportDefinition>();

        public async Task OnGetAsync()
        {
            Reports = await _reportDesignerService.GetReportsAsync();
        }

        public async Task<IActionResult> OnGetDataSourcesAsync()
        {
            var dataSources = await _reportDesignerService.GetDataSourcesAsync();
            return new JsonResult(dataSources);
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


        public async Task<IActionResult> OnPostSaveReportAsync(Guid reportId, string reportData)
        {
            try
            {
                var report = await _reportDesignerService.GetReportAsync(reportId);
                if (report == null)
                {
                    return new JsonResult(new { success = false, error = "Report not found" });
                }

                // Parse the report data JSON
                using var document = System.Text.Json.JsonDocument.Parse(reportData);
                var reportDataObj = document.RootElement;
                
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
                    await _reportDesignerService.SaveReportComponentsAsync(reportId, components);
                }

                // Save relationships if provided
                if (reportDataObj.TryGetProperty("relationships", out var relationshipsElement))
                {
                    var relationships = relationshipsElement.EnumerateArray().ToList();
                    await _reportDesignerService.SaveReportRelationshipsAsync(reportId, relationships);
                }

                report.UpdatedAt = DateTime.UtcNow;
                await _reportDesignerService.SaveReportAsync(report);
                
                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
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
            // Get current user ID from authentication
            // This is a placeholder - implement based on your auth system
            return Guid.Parse("00000000-0000-0000-0000-000000000001"); // Admin user ID
        }
    }
}
