using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CadenceAccounting.Models;
using CadenceAccounting.Services;
using CadenceAccounting.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CadenceAccounting.Pages.Projects
{
    [IgnoreAntiforgeryToken]
    public class DetailsModel : PageModel
    {
        private readonly IProjectService _projectService;
        private readonly ITimeEntryService _timeEntryService;
        private readonly IExpenseService _expenseService;
        private readonly IInvoiceService _invoiceService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DetailsModel> _logger;

        public DetailsModel(
            IProjectService projectService,
            ITimeEntryService timeEntryService,
            IExpenseService expenseService,
            IInvoiceService invoiceService,
            ApplicationDbContext context,
            ILogger<DetailsModel> logger)
        {
            _projectService = projectService;
            _timeEntryService = timeEntryService;
            _expenseService = expenseService;
            _invoiceService = invoiceService;
            _context = context;
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

        public async Task<IActionResult> OnPostAddTimeEntryAsync()
        {
            _logger.LogInformation("AddTimeEntry POST received");
            _logger.LogInformation("Request Content-Type: {ContentType}", Request.ContentType);
            _logger.LogInformation("Request Method: {Method}", Request.Method);
            _logger.LogInformation("Form data: ProjectId={ProjectId}, Date={Date}, Hours={Hours}, Rate={Rate}, Description={Description}, IsBillable={IsBillable}",
                Request.Form["ProjectId"],
                Request.Form["Date"],
                Request.Form["Hours"],
                Request.Form["Rate"],
                Request.Form["Description"],
                Request.Form["IsBillable"]);

            try
            {
                // Get project ID from form
                var projectIdString = Request.Form["ProjectId"].ToString();
                _logger.LogInformation("ProjectId from form: '{ProjectId}'", projectIdString);
                
                Guid projectId;
                if (!string.IsNullOrEmpty(projectIdString) && Guid.TryParse(projectIdString, out projectId))
                {
                    _logger.LogInformation("Using project ID from form: {ProjectId}", projectId);
                }
                else
                {
                    _logger.LogError("No valid project ID found in form. Form: '{FormId}'", projectIdString);
                    TempData["ErrorMessage"] = "Invalid project ID.";
                    return RedirectToPage("/Projects/Index");
                }
                
                _logger.LogInformation("Final project ID: {ProjectId}", projectId);

                // Load the project and related data
                Project = await _projectService.GetProjectByIdAsync(projectId);
                if (Project == null)
                {
                    _logger.LogError("Project not found with ID: {ProjectId}", projectId);
                    TempData["ErrorMessage"] = "Project not found.";
                    return RedirectToPage("/Projects/Index");
                }

                TimeEntries = await _timeEntryService.GetTimeEntriesByProjectAsync(projectId);
                Expenses = await _expenseService.GetExpensesByProjectAsync(projectId);
                Invoices = (await _invoiceService.GetAllInvoicesAsync()).Where(i => i.ProjectId == projectId);

                _logger.LogInformation("Project loaded successfully: {ProjectName}", Project.Name);

                if (!DateTime.TryParse(Request.Form["Date"].ToString(), out var date))
                {
                    TempData["ErrorMessage"] = "Please enter a valid date.";
                    return RedirectToPage(new { id = projectId });
                }

                if (!decimal.TryParse(Request.Form["Hours"].ToString(), out var hours))
                {
                    TempData["ErrorMessage"] = "Please enter a valid number of hours.";
                    return RedirectToPage(new { id = projectId });
                }

                if (!decimal.TryParse(Request.Form["Rate"].ToString(), out var rate))
                {
                    TempData["ErrorMessage"] = "Please enter a valid rate.";
                    return RedirectToPage(new { id = projectId });
                }

                var description = Request.Form["Description"].ToString() ?? string.Empty;
                var isBillable = Request.Form.ContainsKey("IsBillable");

                _logger.LogInformation("Manual binding applied - ProjectId: {ProjectId}, Hours: {Hours}, Rate: {Rate}", projectId, hours, rate);

                // Validation
                if (hours <= 0)
                {
                    TempData["ErrorMessage"] = "Hours must be greater than 0.";
                    return RedirectToPage(new { id = projectId });
                }
                if (rate <= 0)
                {
                    TempData["ErrorMessage"] = "Rate must be greater than 0.";
                    return RedirectToPage(new { id = projectId });
                }

                var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
                var timeEntryId = Guid.NewGuid();
                var createdAt = DateTime.UtcNow;
                
                // Calculate amount
                var amount = hours * rate;

                // Use raw SQL to avoid database trigger conflicts
                await _context.Database.ExecuteSqlInterpolatedAsync($@"
                    INSERT INTO TimeEntries (Id, ProjectId, UserId, Date, Hours, Description, Rate, Amount, IsBillable, CreatedAt, UpdatedAt)
                    VALUES ({timeEntryId}, {projectId}, {userId}, {date}, {hours}, {description}, {rate}, {amount}, {isBillable}, {createdAt}, {createdAt})");

                TempData["SuccessMessage"] = "Time entry added successfully!";
                return RedirectToPage(new { id = projectId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating time entry for project {ProjectId}", Project?.Id);
                TempData["ErrorMessage"] = "An error occurred while adding the time entry. Please try again.";
                return RedirectToPage("/Projects/Index");
            }
        }

        public async Task<IActionResult> OnPostAddExpenseAsync()
        {
            _logger.LogInformation("AddExpense POST received");
            _logger.LogInformation("Request Content-Type: {ContentType}", Request.ContentType);
            _logger.LogInformation("Request Method: {Method}", Request.Method);
            _logger.LogInformation("Form data: ProjectId={ProjectId}, Date={Date}, Description={Description}, Category={Category}, Amount={Amount}, IsBillable={IsBillable}",
                Request.Form["ProjectId"],
                Request.Form["Date"],
                Request.Form["Description"],
                Request.Form["Category"],
                Request.Form["Amount"],
                Request.Form["IsBillable"]);

            try
            {
                // Get project ID from form
                var projectIdString = Request.Form["ProjectId"].ToString();
                _logger.LogInformation("ProjectId from form: '{ProjectId}'", projectIdString);
                
                Guid projectId;
                if (!string.IsNullOrEmpty(projectIdString) && Guid.TryParse(projectIdString, out projectId))
                {
                    _logger.LogInformation("Using project ID from form: {ProjectId}", projectId);
                }
                else
                {
                    _logger.LogError("No valid project ID found in form. Form: '{FormId}'", projectIdString);
                    TempData["ErrorMessage"] = "Invalid project ID.";
                    return RedirectToPage("/Projects/Index");
                }
                
                _logger.LogInformation("Final project ID: {ProjectId}", projectId);

                // Load the project and related data
                Project = await _projectService.GetProjectByIdAsync(projectId);
                if (Project == null)
                {
                    _logger.LogError("Project not found with ID: {ProjectId}", projectId);
                    TempData["ErrorMessage"] = "Project not found.";
                    return RedirectToPage("/Projects/Index");
                }

                TimeEntries = await _timeEntryService.GetTimeEntriesByProjectAsync(projectId);
                Expenses = await _expenseService.GetExpensesByProjectAsync(projectId);
                Invoices = (await _invoiceService.GetAllInvoicesAsync()).Where(i => i.ProjectId == projectId);

                _logger.LogInformation("Project loaded successfully: {ProjectName}", Project.Name);

                // Manual model binding
                if (!DateTime.TryParse(Request.Form["Date"].ToString(), out var date))
                {
                    TempData["ErrorMessage"] = "Please enter a valid date.";
                    return RedirectToPage(new { id = projectId });
                }

                var description = Request.Form["Description"].ToString() ?? string.Empty;
                var category = Request.Form["Category"].ToString() ?? string.Empty;

                if (!decimal.TryParse(Request.Form["Amount"].ToString(), out var amount))
                {
                    TempData["ErrorMessage"] = "Please enter a valid amount.";
                    return RedirectToPage(new { id = projectId });
                }

                var isBillable = Request.Form.ContainsKey("IsBillable");

                _logger.LogInformation("Manual binding applied - ProjectId: {ProjectId}, Amount: {Amount}", projectId, amount);

                // Validation
                if (amount <= 0)
                {
                    TempData["ErrorMessage"] = "Amount must be greater than 0.";
                    return RedirectToPage(new { id = projectId });
                }

                var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
                var expenseId = Guid.NewGuid();
                var createdAt = DateTime.UtcNow;

                // Use raw SQL to avoid database trigger conflicts
                await _context.Database.ExecuteSqlInterpolatedAsync($@"
                    INSERT INTO Expenses (Id, ProjectId, UserId, Date, Description, Category, Amount, IsBillable, CreatedAt, UpdatedAt)
                    VALUES ({expenseId}, {projectId}, {userId}, {date}, {description}, {category}, {amount}, {isBillable}, {createdAt}, {createdAt})");

                TempData["SuccessMessage"] = "Expense added successfully!";
                return RedirectToPage(new { id = projectId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating expense for project {ProjectId}", Project?.Id);
                TempData["ErrorMessage"] = "An error occurred while adding the expense. Please try again.";
                return RedirectToPage("/Projects/Index");
            }
        }
    }
}
