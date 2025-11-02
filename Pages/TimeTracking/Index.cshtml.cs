using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using CadenceAccounting.Services;
using CadenceAccounting.Models;
using CadenceAccounting.Data;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace CadenceAccounting.Pages.TimeTracking
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ITimeEntryService _timeEntryService;
        private readonly IProjectService _projectService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ITimeEntryService timeEntryService, IProjectService projectService, ApplicationDbContext context, ILogger<IndexModel> logger)
        {
            _timeEntryService = timeEntryService;
            _projectService = projectService;
            _context = context;
            _logger = logger;
        }

        public IEnumerable<TimeEntry> TimeEntries { get; set; } = new List<TimeEntry>();
        public IEnumerable<Project> Projects { get; set; } = new List<Project>();

        [BindProperty]
        public TimeEntryInputModel Input { get; set; } = new();


        public async Task<IActionResult> OnGetAsync()
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
            TimeEntries = await _timeEntryService.GetTimeEntriesByUserAsync(userId);
            Projects = await _projectService.GetAllProjectsAsync();
            
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            _logger.LogInformation("Time entry POST received. ModelState.IsValid: {IsValid}", ModelState.IsValid);
            _logger.LogInformation("Request Content-Type: {ContentType}", Request.ContentType);
            _logger.LogInformation("Request Method: {Method}", Request.Method);
            _logger.LogInformation("Form data: ProjectId={ProjectId}, Date={Date}, Hours={Hours}, Rate={Rate}, Description={Description}, IsBillable={IsBillable}",
                Request.Form["Input.ProjectId"],
                Request.Form["Input.Date"],
                Request.Form["Input.Hours"],
                Request.Form["Input.Rate"],
                Request.Form["Input.Description"],
                Request.Form["Input.IsBillable"]);

            // Manual model binding as fallback
            var projectIdString = Request.Form["Input.ProjectId"].ToString();
            if (string.IsNullOrEmpty(projectIdString) || !Guid.TryParse(projectIdString, out var projectId))
            {
                ModelState.AddModelError("Input.ProjectId", "Please select a project.");
                var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
                TimeEntries = await _timeEntryService.GetTimeEntriesByUserAsync(userId);
                Projects = await _projectService.GetAllProjectsAsync();
                return Page();
            }
            Input.ProjectId = projectId;
            
            // Parse other fields with error handling
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

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState is invalid. Errors: {Errors}",
                    string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                
                var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
                TimeEntries = await _timeEntryService.GetTimeEntriesByUserAsync(userId);
                Projects = await _projectService.GetAllProjectsAsync();
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
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating time entry");
                TempData["ErrorMessage"] = "An error occurred while adding the time entry. Please try again.";
                
                var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
                TimeEntries = await _timeEntryService.GetTimeEntriesByUserAsync(userId);
                Projects = await _projectService.GetAllProjectsAsync();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync()
        {
            try
            {
                _logger.LogInformation("DeleteTimeEntry POST received");
                _logger.LogInformation("Request Content-Type: {ContentType}", Request.ContentType);
                _logger.LogInformation("Request Method: {Method}", Request.Method);

                // Get form data
                var formData = Request.Form;
                _logger.LogInformation("Form data: {FormData}", string.Join(", ", formData.Select(kv => $"{kv.Key}=[{string.Join(",", kv.Value.ToArray())}]")));

                // Extract and validate required fields
                if (!Guid.TryParse(formData["id"].FirstOrDefault(), out var timeEntryId))
                {
                    _logger.LogWarning("Invalid id in form data");
                    TempData["ErrorMessage"] = "Invalid time entry ID.";
                    return RedirectToPage();
                }

                _logger.LogInformation("Attempting to delete time entry {TimeEntryId}", timeEntryId);

                // Delete the time entry using raw SQL to avoid trigger conflicts
                var rowsAffected = await _context.Database.ExecuteSqlRawAsync(
                    "DELETE FROM TimeEntries WHERE Id = {0}",
                    timeEntryId);
                    
                if (rowsAffected == 0)
                {
                    _logger.LogWarning("Time entry {TimeEntryId} not found or already deleted", timeEntryId);
                    TempData["ErrorMessage"] = "Time entry not found or already deleted.";
                    return RedirectToPage();
                }

                _logger.LogInformation("Time entry {TimeEntryId} deleted successfully", timeEntryId);
                TempData["SuccessMessage"] = "Time entry deleted successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting time entry");
                TempData["ErrorMessage"] = "An error occurred while deleting the time entry. Please try again.";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnGetProjectRateAsync(Guid projectId)
        {
            var project = await _projectService.GetProjectByIdAsync(projectId);
            if (project == null)
                return new JsonResult(new { rate = 0 });

            return new JsonResult(new { rate = project.HourlyRate });
        }
    }

    public class EditModel : PageModel
    {
        private readonly ITimeEntryService _timeEntryService;
        private readonly IProjectService _projectService;
        private readonly ILogger<EditModel> _logger;

        public EditModel(ITimeEntryService timeEntryService, IProjectService projectService, ILogger<EditModel> logger)
        {
            _timeEntryService = timeEntryService;
            _projectService = projectService;
            _logger = logger;
        }

        [BindProperty]
        public TimeEntryInputModel Input { get; set; } = new();

        public TimeEntry? TimeEntry { get; set; }
        public IEnumerable<Project> Projects { get; set; } = new List<Project>();

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            TimeEntry = await _timeEntryService.GetTimeEntryByIdAsync(id);
            if (TimeEntry == null)
            {
                TempData["ErrorMessage"] = "Time entry not found.";
                return RedirectToPage("Index");
            }

            Projects = await _projectService.GetAllProjectsAsync();

            Input.ProjectId = TimeEntry.ProjectId;
            Input.Date = TimeEntry.Date;
            Input.Hours = TimeEntry.Hours;
            Input.Description = TimeEntry.Description;
            Input.Rate = TimeEntry.Rate;
            Input.IsBillable = TimeEntry.IsBillable;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(Guid id)
        {
            if (!ModelState.IsValid)
            {
                Projects = await _projectService.GetAllProjectsAsync();
                return Page();
            }

            try
            {
                var timeEntry = await _timeEntryService.GetTimeEntryByIdAsync(id);
                if (timeEntry == null)
                {
                    TempData["ErrorMessage"] = "Time entry not found.";
                    return RedirectToPage("Index");
                }

                timeEntry.ProjectId = Input.ProjectId;
                timeEntry.Date = Input.Date;
                timeEntry.Hours = Input.Hours;
                timeEntry.Description = Input.Description;
                timeEntry.Rate = Input.Rate;
                timeEntry.IsBillable = Input.IsBillable;

                await _timeEntryService.UpdateTimeEntryAsync(timeEntry);

                TempData["SuccessMessage"] = "Time entry updated successfully!";
                return RedirectToPage("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating time entry {Id}", id);
                TempData["ErrorMessage"] = "An error occurred while updating the time entry. Please try again.";
                
                Projects = await _projectService.GetAllProjectsAsync();
                return Page();
            }
        }
    }

    public class TimeEntryInputModel
    {
        [Required]
        [Display(Name = "Project")]
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
