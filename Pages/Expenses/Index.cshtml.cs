using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CadenceAccounting.Services;
using CadenceAccounting.Models;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace CadenceAccounting.Pages.Expenses
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly IExpenseService _expenseService;
        private readonly IProjectService _projectService;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(IExpenseService expenseService, IProjectService projectService, ILogger<IndexModel> logger)
        {
            _expenseService = expenseService;
            _projectService = projectService;
            _logger = logger;
        }

        public IEnumerable<Expense> Expenses { get; set; } = new List<Expense>();
        public IEnumerable<Project> Projects { get; set; } = new List<Project>();
        public IEnumerable<string> Categories { get; set; } = new List<string>();

        [BindProperty]
        public ExpenseInputModel Input { get; set; } = new();


        public async Task<IActionResult> OnGetAsync()
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
            Expenses = await _expenseService.GetExpensesByUserAsync(userId);
            Projects = await _projectService.GetAllProjectsAsync();
            Categories = await _expenseService.GetExpenseCategoriesAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
                Expenses = await _expenseService.GetExpensesByUserAsync(userId);
                Projects = await _projectService.GetAllProjectsAsync();
                Categories = await _expenseService.GetExpenseCategoriesAsync();
                return Page();
            }

            try
            {
                var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
                
                var expense = new Expense
                {
                    ProjectId = Input.ProjectId,
                    UserId = userId,
                    Date = Input.Date,
                    Description = Input.Description,
                    Amount = Input.Amount,
                    Category = Input.Category,
                    IsBillable = Input.IsBillable
                };

                // Handle receipt upload
                if (Input.ReceiptFile != null && Input.ReceiptFile.Length > 0)
                {
                    using var memoryStream = new MemoryStream();
                    await Input.ReceiptFile.CopyToAsync(memoryStream);
                    expense.ReceiptImage = memoryStream.ToArray();
                    expense.ReceiptFileName = Input.ReceiptFile.FileName;
                }

                await _expenseService.CreateExpenseAsync(expense);

                TempData["SuccessMessage"] = "Expense added successfully!";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating expense");
                TempData["ErrorMessage"] = "An error occurred while adding the expense. Please try again.";
                
                var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
                Expenses = await _expenseService.GetExpensesByUserAsync(userId);
                Projects = await _projectService.GetAllProjectsAsync();
                Categories = await _expenseService.GetExpenseCategoriesAsync();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(Guid id)
        {
            try
            {
                await _expenseService.DeleteExpenseAsync(id);
                TempData["SuccessMessage"] = "Expense deleted successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting expense {Id}", id);
                TempData["ErrorMessage"] = "An error occurred while deleting the expense.";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnGetReceiptAsync(Guid id)
        {
            var expense = await _expenseService.GetExpenseByIdAsync(id);
            if (expense?.ReceiptImage == null)
                return NotFound();

            return File(expense.ReceiptImage, "application/octet-stream", expense.ReceiptFileName ?? "receipt.jpg");
        }
    }

    public class EditModel : PageModel
    {
        private readonly IExpenseService _expenseService;
        private readonly IProjectService _projectService;
        private readonly ILogger<EditModel> _logger;

        public EditModel(IExpenseService expenseService, IProjectService projectService, ILogger<EditModel> logger)
        {
            _expenseService = expenseService;
            _projectService = projectService;
            _logger = logger;
        }

        [BindProperty]
        public ExpenseInputModel Input { get; set; } = new();

        public Expense? Expense { get; set; }
        public IEnumerable<Project> Projects { get; set; } = new List<Project>();
        public IEnumerable<string> Categories { get; set; } = new List<string>();

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            Expense = await _expenseService.GetExpenseByIdAsync(id);
            if (Expense == null)
            {
                TempData["ErrorMessage"] = "Expense not found.";
                return RedirectToPage("Index");
            }

            Projects = await _projectService.GetAllProjectsAsync();
            Categories = await _expenseService.GetExpenseCategoriesAsync();

            Input.ProjectId = Expense.ProjectId;
            Input.Date = Expense.Date;
            Input.Description = Expense.Description;
            Input.Amount = Expense.Amount;
            Input.Category = Expense.Category;
            Input.IsBillable = Expense.IsBillable;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(Guid id)
        {
            if (!ModelState.IsValid)
            {
                Projects = await _projectService.GetAllProjectsAsync();
                Categories = await _expenseService.GetExpenseCategoriesAsync();
                return Page();
            }

            try
            {
                var expense = await _expenseService.GetExpenseByIdAsync(id);
                if (expense == null)
                {
                    TempData["ErrorMessage"] = "Expense not found.";
                    return RedirectToPage("Index");
                }

                expense.ProjectId = Input.ProjectId;
                expense.Date = Input.Date;
                expense.Description = Input.Description;
                expense.Amount = Input.Amount;
                expense.Category = Input.Category;
                expense.IsBillable = Input.IsBillable;

                // Handle receipt upload
                if (Input.ReceiptFile != null && Input.ReceiptFile.Length > 0)
                {
                    using var memoryStream = new MemoryStream();
                    await Input.ReceiptFile.CopyToAsync(memoryStream);
                    expense.ReceiptImage = memoryStream.ToArray();
                    expense.ReceiptFileName = Input.ReceiptFile.FileName;
                }

                await _expenseService.UpdateExpenseAsync(expense);

                TempData["SuccessMessage"] = "Expense updated successfully!";
                return RedirectToPage("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating expense {Id}", id);
                TempData["ErrorMessage"] = "An error occurred while updating the expense. Please try again.";
                
                Projects = await _projectService.GetAllProjectsAsync();
                Categories = await _expenseService.GetExpenseCategoriesAsync();
                return Page();
            }
        }
    }

    public class ExpenseInputModel
    {
        [Required]
        [Display(Name = "Project")]
        public Guid ProjectId { get; set; }

        [Required]
        [Display(Name = "Date")]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; } = DateTime.Today;

        [Required]
        [Display(Name = "Description")]
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Amount")]
        [Range(0.01, 100000, ErrorMessage = "Amount must be between $0.01 and $100,000")]
        public decimal Amount { get; set; }

        [Required]
        [Display(Name = "Category")]
        [StringLength(100, ErrorMessage = "Category cannot exceed 100 characters")]
        public string Category { get; set; } = string.Empty;

        [Display(Name = "Receipt")]
        public IFormFile? ReceiptFile { get; set; }

        [Display(Name = "Billable")]
        public bool IsBillable { get; set; } = true;
    }
}
