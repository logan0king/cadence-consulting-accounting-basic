using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CadenceAccounting.Models;
using CadenceAccounting.Services;

namespace CadenceAccounting.Pages.Projects
{
    public class DetailsModel : PageModel
    {
        private readonly IProjectService _projectService;
        private readonly ITimeEntryService _timeEntryService;
        private readonly IExpenseService _expenseService;
        private readonly IInvoiceService _invoiceService;
        private readonly ILogger<DetailsModel> _logger;

        public DetailsModel(
            IProjectService projectService,
            ITimeEntryService timeEntryService,
            IExpenseService expenseService,
            IInvoiceService invoiceService,
            ILogger<DetailsModel> logger)
        {
            _projectService = projectService;
            _timeEntryService = timeEntryService;
            _expenseService = expenseService;
            _invoiceService = invoiceService;
            _logger = logger;
        }

        public Project? Project { get; set; }
        public IEnumerable<TimeEntry> TimeEntries { get; set; } = new List<TimeEntry>();
        public IEnumerable<Expense> Expenses { get; set; } = new List<Expense>();
        public IEnumerable<Invoice> Invoices { get; set; } = new List<Invoice>();

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            try
            {
                _logger.LogInformation("Loading project details for ID: {ProjectId}", id);

                Project = await _projectService.GetProjectByIdAsync(id);
                if (Project == null)
                {
                    _logger.LogWarning("Project not found with ID: {ProjectId}", id);
                    TempData["ErrorMessage"] = "Project not found.";
                    return RedirectToPage("Index");
                }

                TimeEntries = await _timeEntryService.GetTimeEntriesByProjectAsync(id);
                Expenses = await _expenseService.GetExpensesByProjectAsync(id);
                Invoices = await _invoiceService.GetAllInvoicesAsync();
                Invoices = Invoices.Where(i => i.ProjectId == id);

                _logger.LogInformation("Successfully loaded project details for: {ProjectName}", Project.Name);
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading project details for ID: {ProjectId}", id);
                TempData["ErrorMessage"] = "An error occurred while loading the project details.";
                return RedirectToPage("Index");
            }
        }
    }
}
