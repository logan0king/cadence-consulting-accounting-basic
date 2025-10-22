using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CadenceAccounting.Services;
using CadenceAccounting.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;

namespace CadenceAccounting.Pages.Reports
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly IProjectService _projectService;
        private readonly ITimeEntryService _timeEntryService;
        private readonly IExpenseService _expenseService;
        private readonly IInvoiceService _invoiceService;
        private readonly IPaymentService _paymentService;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(
            IProjectService projectService,
            ITimeEntryService timeEntryService,
            IExpenseService expenseService,
            IInvoiceService invoiceService,
            IPaymentService paymentService,
            ILogger<IndexModel> logger)
        {
            _projectService = projectService;
            _timeEntryService = timeEntryService;
            _expenseService = expenseService;
            _invoiceService = invoiceService;
            _paymentService = paymentService;
            _logger = logger;
        }

        [BindProperty]
        public ReportRequestModel ReportRequest { get; set; } = new();

        public ReportViewModel? Report { get; set; }

        public class ReportRequestModel
        {
            [Required]
            [Display(Name = "Report Type")]
            public string ReportType { get; set; } = "ProjectSummary";

            [Display(Name = "Project")]
            public Guid? ProjectId { get; set; }

            [Required]
            [Display(Name = "Start Date")]
            [DataType(DataType.Date)]
            public DateTime StartDate { get; set; } = DateTime.Today.AddMonths(-1);

            [Required]
            [Display(Name = "End Date")]
            [DataType(DataType.Date)]
            public DateTime EndDate { get; set; } = DateTime.Today;

            [Display(Name = "Include Time Entries")]
            public bool IncludeTimeEntries { get; set; } = true;

            [Display(Name = "Include Expenses")]
            public bool IncludeExpenses { get; set; } = true;

            [Display(Name = "Include Invoices")]
            public bool IncludeInvoices { get; set; } = true;
        }

        public class ReportViewModel
        {
            public string ReportType { get; set; } = string.Empty;
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public Project? Project { get; set; }
            public decimal TotalHours { get; set; }
            public decimal TotalTimeAmount { get; set; }
            public decimal TotalExpenses { get; set; }
            public decimal TotalInvoiced { get; set; }
            public decimal TotalPaid { get; set; }
            public decimal NetProfit { get; set; }
            public List<TimeEntry> TimeEntries { get; set; } = new();
            public List<Expense> Expenses { get; set; } = new();
            public List<Invoice> Invoices { get; set; } = new();
            public List<Payment> Payments { get; set; } = new();
        }

        public IEnumerable<Project> Projects { get; set; } = new List<Project>();

        public async Task<IActionResult> OnGetAsync()
        {
            Projects = await _projectService.GetAllProjectsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                Projects = await _projectService.GetAllProjectsAsync();
                return Page();
            }

            try
            {
                Report = await GenerateReportAsync();
                Projects = await _projectService.GetAllProjectsAsync();
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating report");
                TempData["ErrorMessage"] = "An error occurred while generating the report. Please try again.";
                
                Projects = await _projectService.GetAllProjectsAsync();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostExportAsync()
        {
            if (!ModelState.IsValid)
            {
                Projects = await _projectService.GetAllProjectsAsync();
                return Page();
            }

            try
            {
                var report = await GenerateReportAsync();
                var csv = GenerateCsvReport(report);
                
                var fileName = $"{ReportRequest.ReportType}_{ReportRequest.StartDate:yyyyMMdd}_{ReportRequest.EndDate:yyyyMMdd}.csv";
                return File(csv, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting report");
                TempData["ErrorMessage"] = "An error occurred while exporting the report.";
                return RedirectToPage();
            }
        }

        private async Task<ReportViewModel> GenerateReportAsync()
        {
            var report = new ReportViewModel
            {
                ReportType = ReportRequest.ReportType,
                StartDate = ReportRequest.StartDate,
                EndDate = ReportRequest.EndDate
            };

            if (ReportRequest.ProjectId.HasValue)
            {
                report.Project = await _projectService.GetProjectByIdAsync(ReportRequest.ProjectId.Value);
            }

            // Get time entries
            if (ReportRequest.IncludeTimeEntries)
            {
                if (ReportRequest.ProjectId.HasValue)
                {
                    report.TimeEntries = (await _timeEntryService.GetTimeEntriesByProjectAsync(ReportRequest.ProjectId.Value))
                        .Where(t => t.Date >= ReportRequest.StartDate && t.Date <= ReportRequest.EndDate)
                        .ToList();
                }
                else
                {
                    report.TimeEntries = (await _timeEntryService.GetTimeEntriesByDateRangeAsync(ReportRequest.StartDate, ReportRequest.EndDate))
                        .ToList();
                }

                report.TotalHours = report.TimeEntries.Sum(t => t.Hours);
                report.TotalTimeAmount = report.TimeEntries.Sum(t => t.Amount);
            }

            // Get expenses
            if (ReportRequest.IncludeExpenses)
            {
                if (ReportRequest.ProjectId.HasValue)
                {
                    report.Expenses = (await _expenseService.GetExpensesByProjectAsync(ReportRequest.ProjectId.Value))
                        .Where(e => e.Date >= ReportRequest.StartDate && e.Date <= ReportRequest.EndDate)
                        .ToList();
                }
                else
                {
                    report.Expenses = (await _expenseService.GetExpensesByDateRangeAsync(ReportRequest.StartDate, ReportRequest.EndDate))
                        .ToList();
                }

                report.TotalExpenses = report.Expenses.Sum(e => e.Amount);
            }

            // Get invoices
            if (ReportRequest.IncludeInvoices)
            {
                var allInvoices = await _invoiceService.GetAllInvoicesAsync();
                report.Invoices = allInvoices
                    .Where(i => i.InvoiceDate >= ReportRequest.StartDate && i.InvoiceDate <= ReportRequest.EndDate)
                    .Where(i => !ReportRequest.ProjectId.HasValue || i.ProjectId == ReportRequest.ProjectId.Value)
                    .ToList();

                report.TotalInvoiced = report.Invoices.Sum(i => i.TotalAmount);
                report.TotalPaid = report.Invoices.Sum(i => i.TotalPaid);
            }

            // Calculate net profit
            report.NetProfit = report.TotalTimeAmount + report.TotalExpenses - report.TotalPaid;

            return report;
        }

        private byte[] GenerateCsvReport(ReportViewModel report)
        {
            var csv = new System.Text.StringBuilder();
            
            // Header
            csv.AppendLine($"Report: {report.ReportType}");
            csv.AppendLine($"Period: {report.StartDate:yyyy-MM-dd} to {report.EndDate:yyyy-MM-dd}");
            if (report.Project != null)
            {
                csv.AppendLine($"Project: {report.Project.Name}");
            }
            csv.AppendLine();

            // Summary
            csv.AppendLine("SUMMARY");
            csv.AppendLine("Metric,Value");
            csv.AppendLine($"Total Hours,{report.TotalHours:F2}");
            csv.AppendLine($"Total Time Amount,{report.TotalTimeAmount:C}");
            csv.AppendLine($"Total Expenses,{report.TotalExpenses:C}");
            csv.AppendLine($"Total Invoiced,{report.TotalInvoiced:C}");
            csv.AppendLine($"Total Paid,{report.TotalPaid:C}");
            csv.AppendLine($"Net Profit,{report.NetProfit:C}");
            csv.AppendLine();

            // Time entries
            if (report.TimeEntries.Any())
            {
                csv.AppendLine("TIME ENTRIES");
                csv.AppendLine("Date,Project,Description,Hours,Rate,Amount,Billable");
                foreach (var entry in report.TimeEntries)
                {
                    csv.AppendLine($"{entry.Date:yyyy-MM-dd},{entry.Project.Name},\"{entry.Description}\",{entry.Hours:F2},{entry.Rate:C},{entry.Amount:C},{entry.IsBillable}");
                }
                csv.AppendLine();
            }

            // Expenses
            if (report.Expenses.Any())
            {
                csv.AppendLine("EXPENSES");
                csv.AppendLine("Date,Project,Description,Category,Amount,Billable");
                foreach (var expense in report.Expenses)
                {
                    csv.AppendLine($"{expense.Date:yyyy-MM-dd},{expense.Project.Name},\"{expense.Description}\",{expense.Category},{expense.Amount:C},{expense.IsBillable}");
                }
                csv.AppendLine();
            }

            // Invoices
            if (report.Invoices.Any())
            {
                csv.AppendLine("INVOICES");
                csv.AppendLine("Invoice Number,Date,Due Date,Amount,Paid,Balance,Status");
                foreach (var invoice in report.Invoices)
                {
                    csv.AppendLine($"{invoice.InvoiceNumber},{invoice.InvoiceDate:yyyy-MM-dd},{invoice.DueDate:yyyy-MM-dd},{invoice.TotalAmount:C},{invoice.TotalPaid:C},{invoice.Balance:C},{invoice.Status}");
                }
            }

            return System.Text.Encoding.UTF8.GetBytes(csv.ToString());
        }
    }
}
