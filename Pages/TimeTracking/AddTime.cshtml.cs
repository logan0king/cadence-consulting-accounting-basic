using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using CadenceAccounting.Services;
using CadenceAccounting.Models;
using CadenceAccounting.Data;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace CadenceAccounting.Pages.TimeTracking
{
    [Authorize]
    [IgnoreAntiforgeryToken]
    public class AddTimeModel : PageModel
    {
        private readonly ITimeEntryService _timeEntryService;
        private readonly IProjectService _projectService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AddTimeModel> _logger;

        public AddTimeModel(ITimeEntryService timeEntryService, IProjectService projectService, ApplicationDbContext context, ILogger<AddTimeModel> logger)
        {
            _timeEntryService = timeEntryService;
            _projectService = projectService;
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public TimeEntryInputModel Input { get; set; } = new();

        public Project? Project { get; set; }
        public Guid ProjectId { get; set; }
        public IEnumerable<TimeEntry> RecentTimeEntries { get; set; } = new List<TimeEntry>();

        public async Task<IActionResult> OnGetAsync(Guid projectId)
        {
            _logger.LogInformation("Loading add time page for project ID: {ProjectId}", projectId);
            
            Project = await _projectService.GetProjectByIdAsync(projectId);
            if (Project == null)
            {
                TempData["ErrorMessage"] = "Project not found.";
                return RedirectToPage("/Projects/Index");
            }

            ProjectId = projectId;
            
            // Prefill the form with project information
            Input.ProjectId = projectId;
            Input.Rate = Project.HourlyRate;
            Input.Date = DateTime.Today;
            Input.IsBillable = true;

            // Load recent time entries for this project
            RecentTimeEntries = await _timeEntryService.GetTimeEntriesByProjectAsync(projectId);
            
            _logger.LogInformation("Successfully loaded add time page for project: {ProjectName}", Project.Name);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(Guid projectId)
        {
            _logger.LogInformation("Time entry POST received for project ID: {ProjectId}. ModelState.IsValid: {IsValid}", projectId, ModelState.IsValid);
            _logger.LogInformation("Request Content-Type: {ContentType}", Request.ContentType);
            _logger.LogInformation("Request Method: {Method}", Request.Method);
            _logger.LogInformation("Form data: ProjectId={ProjectId}, Date={Date}, Hours={Hours}, Rate={Rate}, Description={Description}, IsBillable={IsBillable}",
                Request.Form["Input.ProjectId"],
                Request.Form["Input.Date"],
                Request.Form["Input.Hours"],
                Request.Form["Input.Rate"],
                Request.Form["Input.Description"],
                Request.Form["Input.IsBillable"]);

            // Manual model binding with proper validation
            Input.ProjectId = projectId; // Use the projectId from the route
            
            if (!DateTime.TryParse(Request.Form["Input.Date"].ToString(), out var date))
            {
                ModelState.AddModelError("Input.Date", "Please enter a valid date.");
            }
            Input.Date = date;
            
            if (!decimal.TryParse(Request.Form["Input.Hours"].ToString(), out var hours))
            {
                ModelState.AddModelError("Input.Hours", "Please enter a valid number of hours.");
            }
            Input.Hours = hours;
            
            if (!decimal.TryParse(Request.Form["Input.Rate"].ToString(), out var rate))
            {
                ModelState.AddModelError("Input.Rate", "Please enter a valid rate.");
            }
            Input.Rate = rate;
            
            Input.Description = Request.Form["Input.Description"].ToString() ?? string.Empty;
            Input.IsBillable = Request.Form.ContainsKey("Input.IsBillable");

            _logger.LogInformation("Manual binding applied - ProjectId: {ProjectId}, Hours: {Hours}, Rate: {Rate}", Input.ProjectId, Input.Hours, Input.Rate);

            // Load project and recent entries for display
            Project = await _projectService.GetProjectByIdAsync(projectId);
            if (Project == null)
            {
                TempData["ErrorMessage"] = "Project not found.";
                return RedirectToPage("/Projects/Index");
            }

            ProjectId = projectId;
            RecentTimeEntries = await _timeEntryService.GetTimeEntriesByProjectAsync(projectId);

            // Clear ModelState and validate manually since we're doing manual binding
            ModelState.Clear();
            
            // Manual validation
            if (Input.Hours <= 0)
            {
                ModelState.AddModelError("Input.Hours", "Hours must be greater than 0.");
            }
            if (Input.Rate <= 0)
            {
                ModelState.AddModelError("Input.Rate", "Rate must be greater than 0.");
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Manual validation failed. Errors: {Errors}",
                    string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                return Page();
            }

            try
            {
                var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
                var timeEntryId = Guid.NewGuid();
                var createdAt = DateTime.UtcNow;
                
                // Calculate amount
                var amount = Input.Hours * Input.Rate;

                // Use raw SQL to avoid database trigger conflicts
                await _context.Database.ExecuteSqlInterpolatedAsync($@"
                    INSERT INTO TimeEntries (Id, ProjectId, UserId, Date, Hours, Description, Rate, Amount, IsBillable, CreatedAt, UpdatedAt)
                    VALUES ({timeEntryId}, {Input.ProjectId}, {userId}, {Input.Date}, {Input.Hours}, {Input.Description}, {Input.Rate}, {amount}, {Input.IsBillable}, {createdAt}, {createdAt})");

                TempData["SuccessMessage"] = "Time entry added successfully!";
                
                // Reload recent entries to show the new one
                RecentTimeEntries = await _timeEntryService.GetTimeEntriesByProjectAsync(projectId);
                
                // Reset form for next entry
                Input = new TimeEntryInputModel
                {
                    ProjectId = projectId,
                    Rate = Project.HourlyRate,
                    Date = DateTime.Today,
                    IsBillable = true
                };
                
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating time entry for project {ProjectId}", projectId);
                TempData["ErrorMessage"] = "An error occurred while adding the time entry. Please try again.";
                return Page();
            }
        }

        public class TimeEntryInputModel
        {
            public Guid ProjectId { get; set; }

            [Required]
            [Display(Name = "Date")]
            [DataType(DataType.Date)]
            public DateTime Date { get; set; } = DateTime.Today;

            [Required]
            [Display(Name = "Hours")]
            [Range(0, 24, ErrorMessage = "Hours must be between 0 and 24")]
            public decimal Hours { get; set; }

            [Display(Name = "Description")]
            [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
            public string? Description { get; set; }

            [Required]
            [Display(Name = "Rate")]
            [Range(0.01, 10000, ErrorMessage = "Rate must be between $0.01 and $10,000")]
            public decimal Rate { get; set; }

            [Display(Name = "Billable")]
            public bool IsBillable { get; set; } = true;
        }
    }
}
