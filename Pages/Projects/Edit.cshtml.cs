using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CadenceAccounting.Data;
using CadenceAccounting.Models;
using CadenceAccounting.Services;
using System.ComponentModel.DataAnnotations;

namespace CadenceAccounting.Pages.Projects
{
    [IgnoreAntiforgeryToken]
    public class EditModel : PageModel
    {
        private readonly IProjectService _projectService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EditModel> _logger;

        public EditModel(IProjectService projectService, ApplicationDbContext context, ILogger<EditModel> logger)
        {
            _projectService = projectService;
            _context = context;
            _logger = logger;
        }

        public Project? Project { get; set; }
        public IEnumerable<Company> Companies { get; set; } = new List<Company>();

        [BindProperty]
        public ProjectEditInputModel Input { get; set; } = new();

        public class ProjectEditInputModel
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

            [Display(Name = "Client Email")]
            [EmailAddress]
            [StringLength(255, ErrorMessage = "Client email cannot exceed 255 characters")]
            public string? ClientEmail { get; set; }

            [Display(Name = "Client Phone")]
            [StringLength(20, ErrorMessage = "Client phone cannot exceed 20 characters")]
            public string? ClientPhone { get; set; }

            [Display(Name = "Client Address")]
            [StringLength(500, ErrorMessage = "Client address cannot exceed 500 characters")]
            public string? ClientAddress { get; set; }

            [Display(Name = "Budget")]
            [Range(0.01, 10000000, ErrorMessage = "Budget must be between $0.01 and $10,000,000")]
            public decimal Budget { get; set; }

            [Display(Name = "Hourly Rate")]
            [Range(0.01, 10000, ErrorMessage = "Hourly rate must be between $0.01 and $10,000")]
            public decimal HourlyRate { get; set; }

            [Display(Name = "Start Date")]
            [DataType(DataType.Date)]
            public DateTime StartDate { get; set; }

            [Display(Name = "End Date")]
            [DataType(DataType.Date)]
            public DateTime? EndDate { get; set; }

            [Display(Name = "Status")]
            public string Status { get; set; } = "Active";

            [Display(Name = "Tax Type")]
            public string TaxType { get; set; } = "1099";
        }

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            _logger.LogInformation("Loading project for edit - ID: {ProjectId}", id);

            Project = await _projectService.GetProjectByIdAsync(id);
            if (Project == null)
            {
                TempData["ErrorMessage"] = "Project not found.";
                return RedirectToPage("/Projects/Index");
            }

            Companies = await _context.Companies.OrderBy(c => c.Name).ToListAsync();

            // Populate the input model with current project data
            Input.Name = Project.Name;
            Input.Description = Project.Description;
            Input.ProjectNumber = Project.ProjectNumber;
            Input.CompanyId = Project.CompanyId;
            Input.ClientEmail = Project.ClientEmail;
            Input.ClientPhone = Project.ClientPhone;
            Input.ClientAddress = Project.ClientAddress;
            Input.Budget = Project.Budget;
            Input.HourlyRate = Project.HourlyRate;
            Input.StartDate = Project.StartDate;
            Input.EndDate = Project.EndDate;
            Input.Status = Project.Status;
            Input.TaxType = Project.TaxType;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(Guid id)
        {
            _logger.LogInformation("Project edit POST received for ID: {ProjectId}. ModelState.IsValid: {IsValid}", id, ModelState.IsValid);
            _logger.LogInformation("Request Content-Type: {ContentType}", Request.ContentType);
            _logger.LogInformation("Request Method: {Method}", Request.Method);

            Project = await _projectService.GetProjectByIdAsync(id);
            if (Project == null)
            {
                TempData["ErrorMessage"] = "Project not found.";
                return RedirectToPage("/Projects/Index");
            }

            Companies = await _context.Companies.OrderBy(c => c.Name).ToListAsync();

            // Manual model binding as fallback
            Input.Name = Request.Form["Input.Name"].ToString();
            Input.Description = Request.Form["Input.Description"].ToString();
            Input.ProjectNumber = Request.Form["Input.ProjectNumber"].ToString();
            Input.CompanyId = Guid.Parse(Request.Form["Input.CompanyId"].ToString());
            Input.ClientEmail = Request.Form["Input.ClientEmail"].ToString();
            Input.ClientPhone = Request.Form["Input.ClientPhone"].ToString();
            Input.ClientAddress = Request.Form["Input.ClientAddress"].ToString();
            
            if (decimal.TryParse(Request.Form["Input.Budget"].ToString(), out var budget))
                Input.Budget = budget;
            if (decimal.TryParse(Request.Form["Input.HourlyRate"].ToString(), out var hourlyRate))
                Input.HourlyRate = hourlyRate;
            if (DateTime.TryParse(Request.Form["Input.StartDate"].ToString(), out var startDate))
                Input.StartDate = startDate;
            if (DateTime.TryParse(Request.Form["Input.EndDate"].ToString(), out var endDate))
                Input.EndDate = endDate;
            else
                Input.EndDate = null;

            Input.Status = Request.Form["Input.Status"].ToString();
            Input.TaxType = Request.Form["Input.TaxType"].ToString();

            _logger.LogInformation("Manual binding applied - Input.Name: {Name}, Input.CompanyId: {CompanyId}", Input.Name, Input.CompanyId);

            if (string.IsNullOrWhiteSpace(Input.Name))
            {
                TempData["ErrorMessage"] = "Project name is required.";
                return Page();
            }

            if (string.IsNullOrWhiteSpace(Input.ProjectNumber))
            {
                TempData["ErrorMessage"] = "Project number is required.";
                return Page();
            }

            try
            {
                // Get the selected company
                var selectedCompany = await _context.Companies.FindAsync(Input.CompanyId);
                if (selectedCompany == null)
                {
                    TempData["ErrorMessage"] = "Please select a valid company.";
                    return Page();
                }

                // Update the project
                Project.Name = Input.Name;
                Project.Description = Input.Description;
                Project.ProjectNumber = Input.ProjectNumber;
                Project.CompanyId = Input.CompanyId;
                Project.ClientName = selectedCompany.Name; // Use company name as client name
                Project.ClientEmail = Input.ClientEmail;
                Project.ClientPhone = Input.ClientPhone;
                Project.ClientAddress = Input.ClientAddress;
                Project.Budget = Input.Budget;
                Project.HourlyRate = Input.HourlyRate;
                Project.StartDate = Input.StartDate;
                Project.EndDate = Input.EndDate;
                Project.Status = Input.Status;
                Project.TaxType = Input.TaxType;
                Project.UpdatedAt = DateTime.UtcNow;

                // Use raw SQL to avoid database trigger conflicts
                var updatedAt = DateTime.UtcNow;
                var rowsAffected = await _context.Database.ExecuteSqlInterpolatedAsync($@"
                    UPDATE Projects 
                    SET Name = {Project.Name}, 
                        Description = {Project.Description}, 
                        ProjectNumber = {Project.ProjectNumber}, 
                        CompanyId = {Project.CompanyId}, 
                        ClientName = {Project.ClientName}, 
                        ClientEmail = {Project.ClientEmail}, 
                        ClientPhone = {Project.ClientPhone}, 
                        ClientAddress = {Project.ClientAddress}, 
                        Budget = {Project.Budget}, 
                        HourlyRate = {Project.HourlyRate}, 
                        StartDate = {Project.StartDate}, 
                        EndDate = {Project.EndDate}, 
                        Status = {Project.Status}, 
                        TaxType = {Project.TaxType}, 
                        UpdatedAt = {updatedAt}
                    WHERE Id = {Project.Id}");

                if (rowsAffected == 0)
                {
                    TempData["ErrorMessage"] = "Project not found.";
                    return Page();
                }
                TempData["SuccessMessage"] = "Project updated successfully!";
                return RedirectToPage("/Projects/Details", new { id = Project.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating project with ID {ProjectId}", id);
                TempData["ErrorMessage"] = "An error occurred while updating the project. Please try again.";
                return Page();
            }
        }
    }
}
