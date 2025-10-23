using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CadenceAccounting.Services;
using CadenceAccounting.Models;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using CadenceAccounting.Data;
using Microsoft.EntityFrameworkCore;

namespace CadenceAccounting.Pages.Projects
{
    [Authorize]
    [IgnoreAntiforgeryToken]
    public class IndexModel : PageModel
    {
        private readonly IProjectService _projectService;
        private readonly ILogger<IndexModel> _logger;
        private readonly ApplicationDbContext _context;

        public IndexModel(IProjectService projectService, ILogger<IndexModel> logger, ApplicationDbContext context)
        {
            _projectService = projectService;
            _logger = logger;
            _context = context;
        }

        public IEnumerable<Project> Projects { get; set; } = new List<Project>();
        public IEnumerable<Company> Companies { get; set; } = new List<Company>();
        public bool HasCompanies => Companies.Any();

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
            [Display(Name = "Company")]
            public Guid CompanyId { get; set; }

            [EmailAddress]
            [Display(Name = "Client Email")]
            public string? ClientEmail { get; set; }

            [Display(Name = "Client Phone")]
            public string? ClientPhone { get; set; }

            [Display(Name = "Client Address")]
            public string? ClientAddress { get; set; }

            [Display(Name = "Budget")]
            [Range(0.01, 10000000, ErrorMessage = "Budget must be between $0.01 and $10,000,000")]
            public decimal? Budget { get; set; }

            [Display(Name = "Hourly Rate")]
            [Range(0.01, 10000, ErrorMessage = "Hourly rate must be between $0.01 and $10,000")]
            public decimal? HourlyRate { get; set; }

            [Display(Name = "Start Date")]
            [DataType(DataType.Date)]
            public DateTime? StartDate { get; set; }

            [Display(Name = "End Date")]
            [DataType(DataType.Date)]
            public DateTime? EndDate { get; set; }

            [Display(Name = "Tax Type")]
            public string? TaxType { get; set; }

            [Display(Name = "Contract Document")]
            public IFormFile? ContractFile { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            Projects = await _projectService.GetAllProjectsAsync();
            Companies = await _context.Companies.ToListAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            _logger.LogInformation("Project creation POST received. ModelState.IsValid: {IsValid}", ModelState.IsValid);
            _logger.LogInformation("Request Content-Type: {ContentType}", Request.ContentType);
            _logger.LogInformation("Request Method: {Method}", Request.Method);
            _logger.LogInformation("Form data: Name={Name}, ProjectNumber={ProjectNumber}, CompanyId={CompanyId}, Budget={Budget}, HourlyRate={HourlyRate}, StartDate={StartDate}, TaxType={TaxType}",
                Request.Form["Input.Name"],
                Request.Form["Input.ProjectNumber"],
                Request.Form["Input.CompanyId"],
                Request.Form["Input.Budget"],
                Request.Form["Input.HourlyRate"],
                Request.Form["Input.StartDate"],
                Request.Form["Input.TaxType"]);

            // Check if CompanyId is provided and valid
            var companyIdString = Request.Form["Input.CompanyId"].ToString();
            if (string.IsNullOrEmpty(companyIdString) || !Guid.TryParse(companyIdString, out var companyId))
            {
                TempData["ErrorMessage"] = "Please select a company for this project.";
                Companies = await _context.Companies.ToListAsync();
                Projects = await _projectService.GetAllProjectsAsync();
                return Page();
            }
            
            // Set the CompanyId if it wasn't bound properly
            if (Input.CompanyId == Guid.Empty)
            {
                Input.CompanyId = companyId;
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState is invalid. Errors: {Errors}",
                    string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                Projects = await _projectService.GetAllProjectsAsync();
                Companies = await _context.Companies.ToListAsync();
                return Page();
            }

            try
            {
                var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
                
                // Get the selected company
                var selectedCompany = await _context.Companies.FindAsync(Input.CompanyId);
                if (selectedCompany == null)
                {
                    TempData["ErrorMessage"] = "Please select a valid company.";
                    Companies = await _context.Companies.ToListAsync();
                    Projects = await _projectService.GetAllProjectsAsync();
                    return Page();
                }

                var project = new Project
                {
                    CompanyId = Input.CompanyId,
                    Name = Input.Name,
                    Description = Input.Description,
                    ProjectNumber = Input.ProjectNumber,
                    ClientName = selectedCompany.Name, // Use company name as client name
                    ClientEmail = Input.ClientEmail,
                    ClientPhone = Input.ClientPhone,
                    ClientAddress = Input.ClientAddress,
                    Budget = Input.Budget ?? 0,
                    HourlyRate = Input.HourlyRate ?? 0,
                    StartDate = Input.StartDate ?? DateTime.Today,
                    EndDate = Input.EndDate,
                    Status = "Active",
                    TaxType = Input.TaxType ?? "1099",
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
                Companies = await _context.Companies.ToListAsync();
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
}
