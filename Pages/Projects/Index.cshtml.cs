using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CadenceAccounting.Services;
using CadenceAccounting.Models;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace CadenceAccounting.Pages.Projects
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly IProjectService _projectService;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(IProjectService projectService, ILogger<IndexModel> logger)
        {
            _projectService = projectService;
            _logger = logger;
        }

        public IEnumerable<Project> Projects { get; set; } = new List<Project>();

        [BindProperty]
        public ProjectInputModel Input { get; set; } = new();

        public class ProjectInputModel
        {
            [Required]
            [Display(Name = "Project Name")]
            [StringLength(255, ErrorMessage = "Project name cannot exceed 255 characters")]
            public string Name { get; set; } = string.Empty;

            [Display(Name = "Description")]
            public string? Description { get; set; }

            [Required]
            [Display(Name = "Project Number")]
            [StringLength(50, ErrorMessage = "Project number cannot exceed 50 characters")]
            public string ProjectNumber { get; set; } = string.Empty;

            [Required]
            [Display(Name = "Client Name")]
            [StringLength(255, ErrorMessage = "Client name cannot exceed 255 characters")]
            public string ClientName { get; set; } = string.Empty;

            [EmailAddress]
            [Display(Name = "Client Email")]
            public string? ClientEmail { get; set; }

            [Display(Name = "Client Phone")]
            public string? ClientPhone { get; set; }

            [Display(Name = "Client Address")]
            public string? ClientAddress { get; set; }

            [Required]
            [Display(Name = "Budget")]
            [Range(0.01, 10000000, ErrorMessage = "Budget must be between $0.01 and $10,000,000")]
            public decimal Budget { get; set; }

            [Required]
            [Display(Name = "Hourly Rate")]
            [Range(0.01, 10000, ErrorMessage = "Hourly rate must be between $0.01 and $10,000")]
            public decimal HourlyRate { get; set; }

            [Required]
            [Display(Name = "Start Date")]
            [DataType(DataType.Date)]
            public DateTime StartDate { get; set; } = DateTime.Today;

            [Display(Name = "End Date")]
            [DataType(DataType.Date)]
            public DateTime? EndDate { get; set; }

            [Required]
            [Display(Name = "Tax Type")]
            public string TaxType { get; set; } = "1099";

            [Display(Name = "Contract Document")]
            public IFormFile? ContractFile { get; set; }
        }

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
                var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
                
                var project = new Project
                {
                    CompanyId = Guid.Empty, // Will be set to default company
                    Name = Input.Name,
                    Description = Input.Description,
                    ProjectNumber = Input.ProjectNumber,
                    ClientName = Input.ClientName,
                    ClientEmail = Input.ClientEmail,
                    ClientPhone = Input.ClientPhone,
                    ClientAddress = Input.ClientAddress,
                    Budget = Input.Budget,
                    HourlyRate = Input.HourlyRate,
                    StartDate = Input.StartDate,
                    EndDate = Input.EndDate,
                    Status = "Active",
                    TaxType = Input.TaxType,
                    CreatedBy = userId
                };

                // Handle contract document upload
                if (Input.ContractFile != null && Input.ContractFile.Length > 0)
                {
                    using var memoryStream = new MemoryStream();
                    await Input.ContractFile.CopyToAsync(memoryStream);
                    project.ContractDocument = memoryStream.ToArray();
                }

                await _projectService.CreateProjectAsync(project);

                TempData["SuccessMessage"] = "Project created successfully!";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating project");
                TempData["ErrorMessage"] = "An error occurred while creating the project. Please try again.";
                
                Projects = await _projectService.GetAllProjectsAsync();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(Guid id)
        {
            try
            {
                await _projectService.DeleteProjectAsync(id);
                TempData["SuccessMessage"] = "Project deleted successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting project {Id}", id);
                TempData["ErrorMessage"] = "An error occurred while deleting the project.";
            }

            return RedirectToPage();
        }
    }

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
            Project = await _projectService.GetProjectByIdAsync(id);
            if (Project == null)
            {
                TempData["ErrorMessage"] = "Project not found.";
                return RedirectToPage("Index");
            }

            TimeEntries = await _timeEntryService.GetTimeEntriesByProjectAsync(id);
            Expenses = await _expenseService.GetExpensesByProjectAsync(id);
            Invoices = await _invoiceService.GetAllInvoicesAsync();
            Invoices = Invoices.Where(i => i.ProjectId == id);

            return Page();
        }
    }
}
