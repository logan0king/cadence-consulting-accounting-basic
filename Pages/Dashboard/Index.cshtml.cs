using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CadenceAccounting.Services;
using CadenceAccounting.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace CadenceAccounting.Pages.Dashboard
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

        public DashboardViewModel Dashboard { get; set; } = new();

        public class DashboardViewModel
        {
            public int TotalProjects { get; set; }
            public int ActiveProjects { get; set; }
            public int OverBudgetProjectsCount { get; set; }
            public decimal TotalBudget { get; set; }
            public decimal TotalSpent { get; set; }
            public decimal TotalInvoiced { get; set; }
            public decimal TotalPaid { get; set; }
            public decimal OutstandingInvoices { get; set; }
            public decimal ThisMonthHours { get; set; }
            public decimal ThisMonthExpenses { get; set; }
            public List<Project> RecentProjects { get; set; } = new();
            public List<Project> OverBudgetProjects { get; set; } = new();
            public List<Invoice> RecentInvoices { get; set; } = new();
            public List<TimeEntry> RecentTimeEntries { get; set; } = new();
            public List<Expense> RecentExpenses { get; set; } = new();
            public List<BudgetAlert> BudgetAlerts { get; set; } = new();
        }

        public class BudgetAlert
        {
            public Guid ProjectId { get; set; }
            public string ProjectName { get; set; } = string.Empty;
            public decimal BudgetUtilization { get; set; }
            public string AlertLevel { get; set; } = string.Empty; // "Warning", "Critical"
            public string Message { get; set; } = string.Empty;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                _logger.LogInformation("Dashboard OnGetAsync called. User authenticated: {IsAuthenticated}", User.Identity?.IsAuthenticated);
                _logger.LogInformation("User claims count: {ClaimsCount}", User.Claims.Count());
                _logger.LogInformation("User name: {UserName}", User.Identity?.Name);
                
                await LoadDashboardData();
                await CheckBudgetAlerts();
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard data");
                TempData["ErrorMessage"] = "An error occurred while loading the dashboard.";
                return Page();
            }
        }

        private async Task LoadDashboardData()
        {
            var projects = await _projectService.GetAllProjectsAsync();
            var invoices = await _invoiceService.GetAllInvoicesAsync();
            
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("User ID claim value: {UserIdClaim}", userIdClaim);
            
            var userId = Guid.Parse(userIdClaim ?? Guid.Empty.ToString());
            _logger.LogInformation("Parsed user ID: {UserId}", userId);

            // Calculate date ranges
            var startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

            // Project statistics
            Dashboard.TotalProjects = projects.Count();
            Dashboard.ActiveProjects = projects.Count(p => p.Status == "Active");
            Dashboard.TotalBudget = projects.Sum(p => p.Budget);
            Dashboard.TotalSpent = projects.Sum(p => p.TotalAmount);

            // Invoice statistics
            Dashboard.TotalInvoiced = invoices.Sum(i => i.TotalAmount);
            Dashboard.TotalPaid = invoices.Sum(i => i.TotalPaid);
            Dashboard.OutstandingInvoices = Dashboard.TotalInvoiced - Dashboard.TotalPaid;

            // This month statistics
            Dashboard.ThisMonthHours = await _timeEntryService.GetTimeEntriesByDateRangeAsync(startOfMonth, endOfMonth)
                .ContinueWith(t => t.Result.Sum(te => te.Hours));
            
            Dashboard.ThisMonthExpenses = await _expenseService.GetExpensesByDateRangeAsync(startOfMonth, endOfMonth)
                .ContinueWith(t => t.Result.Sum(e => e.Amount));

            // Recent data
            Dashboard.RecentProjects = projects.OrderByDescending(p => p.CreatedAt).Take(5).ToList();
            Dashboard.RecentInvoices = invoices.OrderByDescending(i => i.InvoiceDate).Take(5).ToList();
            Dashboard.RecentTimeEntries = (await _timeEntryService.GetTimeEntriesByUserAsync(userId))
                .OrderByDescending(t => t.Date).Take(5).ToList();
            Dashboard.RecentExpenses = (await _expenseService.GetExpensesByUserAsync(userId))
                .OrderByDescending(e => e.Date).Take(5).ToList();
        }

        private async Task CheckBudgetAlerts()
        {
            var projects = await _projectService.GetAllProjectsAsync();
            var alerts = new List<BudgetAlert>();

            foreach (var project in projects.Where(p => p.Status == "Active"))
            {
                var utilization = project.BudgetUtilization;
                
                if (utilization >= 100)
                {
                    alerts.Add(new BudgetAlert
                    {
                        ProjectId = project.Id,
                        ProjectName = project.Name,
                        BudgetUtilization = utilization,
                        AlertLevel = "Critical",
                        Message = $"Project '{project.Name}' is over budget by {Math.Abs(project.RemainingBudget):C}"
                    });
                }
                else if (utilization >= 90)
                {
                    alerts.Add(new BudgetAlert
                    {
                        ProjectId = project.Id,
                        ProjectName = project.Name,
                        BudgetUtilization = utilization,
                        AlertLevel = "Warning",
                        Message = $"Project '{project.Name}' is at {utilization:F1}% of budget"
                    });
                }
            }

            Dashboard.BudgetAlerts = alerts.OrderByDescending(a => a.BudgetUtilization).ToList();
        }
    }
}
