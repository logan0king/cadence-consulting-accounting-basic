using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CadenceAccounting.Services;
using CadenceAccounting.Data;
using CadenceAccounting.Models;
using Microsoft.AspNetCore.Authorization;

namespace CadenceAccounting.Pages.Reports
{
    [Authorize]
    public class ManageModel : PageModel
    {
        private readonly IReportDesignerService _reportDesignerService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ManageModel> _logger;

        public ManageModel(
            IReportDesignerService reportDesignerService,
            ApplicationDbContext context,
            ILogger<ManageModel> logger)
        {
            _reportDesignerService = reportDesignerService;
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public List<ReportDefinition>? Reports { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            _logger.LogInformation("OnGetAsync called");
            await LoadReportsAsync();
            _logger.LogInformation("OnGetAsync completed - Reports count: {Count}", this.Reports?.Count ?? 0);
            
            // Use ViewData to ensure data persists through rendering
            ViewData["Reports"] = this.Reports;
            
            return Page();
        }

        public async Task<IActionResult> OnPostDeleteAsync(Guid reportId)
        {
            _logger.LogInformation("=== DELETE HANDLER CALLED ===");
            _logger.LogInformation("OnPostDeleteAsync called with reportId: {ReportId}", reportId);
            _logger.LogInformation("Request method: {Method}", Request.Method);
            _logger.LogInformation("Request path: {Path}", Request.Path);
            _logger.LogInformation("Request query string: {QueryString}", Request.QueryString);
            try
            {
                _logger.LogInformation("Attempting to delete report {ReportId}", reportId);
                
                // Check if report exists before deletion
                var reportExists = await _context.ReportDefinitions.FindAsync(reportId);
                if (reportExists == null)
                {
                    _logger.LogWarning("Report {ReportId} not found in database", reportId);
                    TempData["ErrorMessage"] = "Report not found.";
                    await LoadReportsAsync();
                    return Page();
                }
                
                _logger.LogInformation("Report {ReportId} found: {Title}", reportId, reportExists.Title);
                
                var success = await _reportDesignerService.DeleteReportAsync(reportId);
                if (success)
                {
                    _logger.LogInformation("Report {ReportId} deleted successfully", reportId);
                    TempData["SuccessMessage"] = "Report deleted successfully.";
                }
                else
                {
                    _logger.LogWarning("Report {ReportId} deletion failed", reportId);
                    TempData["ErrorMessage"] = "Failed to delete report.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting report {ReportId}", reportId);
                TempData["ErrorMessage"] = $"Error deleting report: {ex.Message}";
            }

                    // Reload reports after deletion
                    await LoadReportsAsync();
                    ViewData["Reports"] = this.Reports;
                    return Page();
        }

        private async Task LoadReportsAsync()
        {
            try
            {
                _logger.LogInformation("Loading reports for management page");
                _logger.LogInformation("Reports property before loading: {Count}", this.Reports?.Count ?? 0);
                
                var reports = await _context.ReportDefinitions
                    .Include(r => r.CreatedByUser)
                    .OrderBy(r => r.ReportGroup)
                    .ThenBy(r => r.Title)
                    .ToListAsync();
                
                _logger.LogInformation("Database query returned {Count} reports", reports.Count);
                
                Reports = reports;
                
                _logger.LogInformation("Reports property after assignment: {Count}", this.Reports?.Count ?? 0);
                _logger.LogInformation("Reports property reference check: {IsNull}", this.Reports == null ? "NULL" : "NOT NULL");
                _logger.LogInformation("Reports property actual content: {HasContent}", this.Reports?.Any() == true ? "HAS CONTENT" : "EMPTY");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading reports for management page");
                TempData["ErrorMessage"] = "Error loading reports. Please try again.";
            }
        }
    }
}
