using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using CadenceAccounting.Services;
using CadenceAccounting.Models;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace CadenceAccounting.Pages.TimeTracking
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ITimeEntryService _timeEntryService;
        private readonly IProjectService _projectService;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ITimeEntryService timeEntryService, IProjectService projectService, ILogger<IndexModel> logger)
        {
            _timeEntryService = timeEntryService;
            _projectService = projectService;
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
            if (!ModelState.IsValid)
            {
                var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
                TimeEntries = await _timeEntryService.GetTimeEntriesByUserAsync(userId);
                Projects = await _projectService.GetAllProjectsAsync();
                return Page();
            }

            try
            {
                var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
                
                var timeEntry = new TimeEntry
                {
                    ProjectId = Input.ProjectId,
                    UserId = userId,
                    Date = Input.Date,
                    Hours = Input.Hours,
                    Description = Input.Description,
                    Rate = Input.Rate,
                    IsBillable = Input.IsBillable
                };

                await _timeEntryService.CreateTimeEntryAsync(timeEntry);

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

        public async Task<IActionResult> OnPostDeleteAsync(Guid id)
        {
            try
            {
                await _timeEntryService.DeleteTimeEntryAsync(id);
                TempData["SuccessMessage"] = "Time entry deleted successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting time entry {Id}", id);
                TempData["ErrorMessage"] = "An error occurred while deleting the time entry.";
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
        [Range(0.1, 24, ErrorMessage = "Hours must be between 0.1 and 24")]
        public decimal Hours { get; set; }

        [Required]
        [Display(Name = "Description")]
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Rate")]
        [Range(0.01, 10000, ErrorMessage = "Rate must be between $0.01 and $10,000")]
        public decimal Rate { get; set; }

        [Display(Name = "Billable")]
        public bool IsBillable { get; set; } = true;
    }
}
