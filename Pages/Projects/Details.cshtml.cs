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

        public async Task<IActionResult> OnPostCreateInvoiceAsync()
        {
            _logger.LogInformation("CreateInvoice POST received");
            _logger.LogInformation("Request Content-Type: {ContentType}", Request.ContentType);
            _logger.LogInformation("Request Method: {Method}", Request.Method);

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

                // Parse form data
                if (!DateTime.TryParse(Request.Form["InvoiceDate"].ToString(), out var invoiceDate))
                {
                    TempData["ErrorMessage"] = "Please enter a valid invoice date.";
                    return RedirectToPage(new { id = projectId });
                }

                if (!DateTime.TryParse(Request.Form["DueDate"].ToString(), out var dueDate))
                {
                    TempData["ErrorMessage"] = "Please enter a valid due date.";
                    return RedirectToPage(new { id = projectId });
                }

                var notes = Request.Form["Notes"].ToString() ?? string.Empty;

                // Get selected time entries and expenses
                var selectedTimeEntryIds = Request.Form["SelectedTimeEntries"].ToString().Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(Guid.Parse).ToList();
                var selectedExpenseIds = Request.Form["SelectedExpenses"].ToString().Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(Guid.Parse).ToList();

                _logger.LogInformation("Selected time entries: {TimeEntryCount}, Selected expenses: {ExpenseCount}", 
                    selectedTimeEntryIds.Count, selectedExpenseIds.Count);

                // Validation
                if (dueDate <= invoiceDate)
                {
                    TempData["ErrorMessage"] = "Due date must be after invoice date.";
                    return RedirectToPage(new { id = projectId });
                }

                if (!selectedTimeEntryIds.Any() && !selectedExpenseIds.Any())
                {
                    TempData["ErrorMessage"] = "Please select at least one time entry or expense to include in the invoice.";
                    return RedirectToPage(new { id = projectId });
                }

                var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());

                // Create invoice with selected items
                var invoice = await CreateInvoiceWithSelectedItems(projectId, invoiceDate, dueDate, notes, selectedTimeEntryIds, selectedExpenseIds, userId);

                TempData["SuccessMessage"] = $"Invoice {invoice.InvoiceNumber} created successfully!";
                return RedirectToPage(new { id = projectId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating invoice");
                TempData["ErrorMessage"] = "An error occurred while creating the invoice. Please try again.";
                return RedirectToPage();
            }
        }

        private async Task<Invoice> CreateInvoiceWithSelectedItems(Guid projectId, DateTime invoiceDate, DateTime dueDate, 
            string notes, List<Guid> selectedTimeEntryIds, List<Guid> selectedExpenseIds, Guid userId)
        {
            // Load project
            var project = await _projectService.GetProjectByIdAsync(projectId);
            if (project == null)
            {
                throw new InvalidOperationException("Project not found");
            }

            // Load selected time entries and expenses
            var selectedTimeEntries = new List<TimeEntry>();
            var selectedExpenses = new List<Expense>();

            foreach (var timeEntryId in selectedTimeEntryIds)
            {
                var timeEntry = await _timeEntryService.GetTimeEntryByIdAsync(timeEntryId);
                if (timeEntry != null && timeEntry.ProjectId == projectId && timeEntry.IsBillable && !timeEntry.IsInvoiced)
                {
                    selectedTimeEntries.Add(timeEntry);
                }
            }

            foreach (var expenseId in selectedExpenseIds)
            {
                var expense = await _expenseService.GetExpenseByIdAsync(expenseId);
                if (expense != null && expense.ProjectId == projectId && expense.IsBillable && !expense.IsInvoiced)
                {
                    selectedExpenses.Add(expense);
                }
            }

            // Calculate totals
            var timeTotal = selectedTimeEntries.Sum(t => t.Amount);
            var expenseTotal = selectedExpenses.Sum(e => e.Amount);
            var subtotal = timeTotal + expenseTotal;
            var taxAmount = 0m; // For now, no tax calculation
            var totalAmount = subtotal + taxAmount;

            // Generate invoice number
            var invoiceNumber = await _invoiceService.GenerateInvoiceNumberAsync(project.ProjectNumber);

            // Create invoice
            var invoice = new Invoice
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                InvoiceNumber = invoiceNumber,
                InvoiceDate = invoiceDate,
                DueDate = dueDate,
                Subtotal = subtotal,
                TaxAmount = taxAmount,
                TotalAmount = totalAmount,
                Status = "Draft",
                Notes = notes,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Save invoice
            await _context.Invoices.AddAsync(invoice);
            await _context.SaveChangesAsync();

            // Create invoice items for time entries
            foreach (var timeEntry in selectedTimeEntries)
            {
                var invoiceItem = new InvoiceItem
                {
                    Id = Guid.NewGuid(),
                    InvoiceId = invoice.Id,
                    Description = $"{timeEntry.Date:MM/dd/yyyy} - {timeEntry.Description}",
                    Quantity = timeEntry.Hours,
                    Rate = timeEntry.Rate,
                    Amount = timeEntry.Amount,
                    ItemType = "Time",
                    CreatedAt = DateTime.UtcNow
                };
                await _context.InvoiceItems.AddAsync(invoiceItem);
            }

            // Create invoice items for expenses
            foreach (var expense in selectedExpenses)
            {
                var invoiceItem = new InvoiceItem
                {
                    Id = Guid.NewGuid(),
                    InvoiceId = invoice.Id,
                    Description = $"{expense.Date:MM/dd/yyyy} - {expense.Description} ({expense.Category})",
                    Quantity = 1,
                    Rate = expense.Amount,
                    Amount = expense.Amount,
                    ItemType = "Expense",
                    CreatedAt = DateTime.UtcNow
                };
                await _context.InvoiceItems.AddAsync(invoiceItem);
            }

            // Mark selected items as invoiced using raw SQL to avoid trigger conflicts
            if (selectedTimeEntryIds.Any())
            {
                var timeEntryIdsParam = string.Join(",", selectedTimeEntryIds.Select(id => $"'{id}'"));
                var currentTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
                await _context.Database.ExecuteSqlRawAsync(
                    $"UPDATE TimeEntries SET IsInvoiced = 1, UpdatedAt = '{currentTime}' WHERE Id IN ({timeEntryIdsParam})");
            }

            if (selectedExpenseIds.Any())
            {
                var expenseIdsParam = string.Join(",", selectedExpenseIds.Select(id => $"'{id}'"));
                var currentTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
                await _context.Database.ExecuteSqlRawAsync(
                    $"UPDATE Expenses SET IsInvoiced = 1, UpdatedAt = '{currentTime}' WHERE Id IN ({expenseIdsParam})");
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Invoice {InvoiceNumber} created with {TimeEntryCount} time entries and {ExpenseCount} expenses", 
                invoice.InvoiceNumber, selectedTimeEntries.Count, selectedExpenses.Count);

            return invoice;
        }

        public async Task<IActionResult> OnPostUpdateTimeEntryAsync()
        {
            try
            {
                _logger.LogInformation("UpdateTimeEntry POST received");
                _logger.LogInformation("Request Content-Type: {ContentType}", Request.ContentType);
                _logger.LogInformation("Request Method: {Method}", Request.Method);

                // Get form data
                var formData = Request.Form;
                _logger.LogInformation("Form data: {FormData}", string.Join(", ", formData.Select(kv => $"{kv.Key}=[{string.Join(",", kv.Value.ToArray())}]")));

                // Extract and validate required fields
                if (!Guid.TryParse(formData["TimeEntryId"].FirstOrDefault(), out var timeEntryId))
                {
                    _logger.LogWarning("Invalid TimeEntryId in form data");
                    TempData["ErrorMessage"] = "Invalid time entry ID.";
                    return RedirectToPage();
                }

                if (!Guid.TryParse(formData["ProjectId"].FirstOrDefault(), out var projectId))
                {
                    _logger.LogWarning("Invalid ProjectId in form data");
                    TempData["ErrorMessage"] = "Invalid project ID.";
                    return RedirectToPage();
                }

                // Get the existing time entry
                var existingEntry = await _timeEntryService.GetTimeEntryByIdAsync(timeEntryId);
                if (existingEntry == null)
                {
                    _logger.LogWarning("Time entry not found: {TimeEntryId}", timeEntryId);
                    TempData["ErrorMessage"] = "Time entry not found.";
                    return RedirectToPage(new { id = projectId });
                }

                // Parse and validate form data
                if (!DateTime.TryParse(formData["Date"].FirstOrDefault(), out var date))
                {
                    TempData["ErrorMessage"] = "Invalid date format.";
                    return RedirectToPage(new { id = projectId });
                }

                if (!decimal.TryParse(formData["Hours"].FirstOrDefault(), out var hours) || hours < 0)
                {
                    TempData["ErrorMessage"] = "Invalid hours value.";
                    return RedirectToPage(new { id = projectId });
                }

                if (!decimal.TryParse(formData["Rate"].FirstOrDefault(), out var rate) || rate <= 0)
                {
                    TempData["ErrorMessage"] = "Invalid rate value.";
                    return RedirectToPage(new { id = projectId });
                }

                var description = formData["Description"].FirstOrDefault() ?? string.Empty;
                var isBillable = formData["IsBillable"].FirstOrDefault() == "on";

                // Update the time entry using raw SQL to avoid trigger conflicts
                var amount = hours * rate;
                var updatedAt = DateTime.UtcNow;
                
                await _context.Database.ExecuteSqlRawAsync(
                    "UPDATE TimeEntries SET Date = {0}, Hours = {1}, Rate = {2}, Description = {3}, IsBillable = {4}, Amount = {5}, UpdatedAt = {6} WHERE Id = {7}",
                    date, hours, rate, description, isBillable, amount, updatedAt, timeEntryId);

                _logger.LogInformation("Time entry {TimeEntryId} updated successfully", timeEntryId);
                TempData["SuccessMessage"] = "Time entry updated successfully!";
                return RedirectToPage(new { id = projectId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating time entry");
                TempData["ErrorMessage"] = "An error occurred while updating the time entry. Please try again.";
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnPostDeleteTimeEntryAsync()
        {
            try
            {
                _logger.LogInformation("DeleteTimeEntry POST received");

                // Get form data
                var formData = Request.Form;
                _logger.LogInformation("Form data: {FormData}", string.Join(", ", formData.Select(kv => $"{kv.Key}=[{string.Join(",", kv.Value.ToArray())}]")));

                // Extract and validate required fields
                if (!Guid.TryParse(formData["TimeEntryId"].FirstOrDefault(), out var timeEntryId))
                {
                    _logger.LogWarning("Invalid TimeEntryId in form data");
                    TempData["ErrorMessage"] = "Invalid time entry ID.";
                    return RedirectToPage();
                }

                // Get the existing time entry to get the project ID
                var existingEntry = await _timeEntryService.GetTimeEntryByIdAsync(timeEntryId);
                if (existingEntry == null)
                {
                    _logger.LogWarning("Time entry not found: {TimeEntryId}", timeEntryId);
                    TempData["ErrorMessage"] = "Time entry not found.";
                    return RedirectToPage();
                }

                var projectId = existingEntry.ProjectId;

                // Delete the time entry using raw SQL to avoid trigger conflicts
                var rowsAffected = await _context.Database.ExecuteSqlRawAsync(
                    "DELETE FROM TimeEntries WHERE Id = {0}",
                    timeEntryId);
                    
                if (rowsAffected == 0)
                {
                    TempData["ErrorMessage"] = "Failed to delete time entry.";
                    return RedirectToPage(new { id = projectId });
                }

                _logger.LogInformation("Time entry {TimeEntryId} deleted successfully", timeEntryId);
                TempData["SuccessMessage"] = "Time entry deleted successfully!";
                return RedirectToPage(new { id = projectId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting time entry");
                TempData["ErrorMessage"] = "An error occurred while deleting the time entry. Please try again.";
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnPostUpdateExpenseAsync()
        {
            try
            {
                _logger.LogInformation("UpdateExpense POST received");
                _logger.LogInformation("Request Content-Type: {ContentType}", Request.ContentType);
                _logger.LogInformation("Request Method: {Method}", Request.Method);

                var formData = await Request.ReadFormAsync();
                _logger.LogInformation("Form data: {FormData}", string.Join(", ", formData.Select(kv => $"{kv.Key}=[{string.Join(", ", kv.Value)}]")));

                if (!Guid.TryParse(formData["ExpenseId"].FirstOrDefault(), out var expenseId))
                {
                    TempData["ErrorMessage"] = "Invalid expense ID.";
                    return RedirectToPage();
                }

                if (!Guid.TryParse(formData["ProjectId"].FirstOrDefault(), out var projectId))
                {
                    TempData["ErrorMessage"] = "Invalid project ID.";
                    return RedirectToPage();
                }

                // Get existing expense
                var existingExpense = await _expenseService.GetExpenseByIdAsync(expenseId);
                if (existingExpense == null)
                {
                    TempData["ErrorMessage"] = "Expense not found.";
                    return RedirectToPage();
                }

                // Parse form data
                if (!DateTime.TryParse(formData["Date"].FirstOrDefault(), out var date))
                {
                    TempData["ErrorMessage"] = "Invalid date format.";
                    return RedirectToPage();
                }

                if (!decimal.TryParse(formData["Amount"].FirstOrDefault(), out var amount))
                {
                    TempData["ErrorMessage"] = "Invalid amount.";
                    return RedirectToPage();
                }

                var description = formData["Description"].FirstOrDefault() ?? string.Empty;
                var category = formData["Category"].FirstOrDefault() ?? string.Empty;
                var isBillable = formData["IsBillable"].FirstOrDefault() == "on";

                // Update the expense using raw SQL to avoid trigger conflicts
                var updatedAt = DateTime.UtcNow;
                
                await _context.Database.ExecuteSqlRawAsync(
                    "UPDATE Expenses SET Date = {0}, Description = {1}, Category = {2}, Amount = {3}, IsBillable = {4}, UpdatedAt = {5} WHERE Id = {6}",
                    date, description, category, amount, isBillable, updatedAt, expenseId);

                _logger.LogInformation("Expense {ExpenseId} updated successfully", expenseId);
                TempData["SuccessMessage"] = "Expense updated successfully!";
                return RedirectToPage(new { id = projectId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating expense");
                TempData["ErrorMessage"] = "An error occurred while updating the expense. Please try again.";
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnPostDeleteExpenseAsync()
        {
            try
            {
                _logger.LogInformation("DeleteExpense POST received");

                if (!Guid.TryParse(Request.Form["ExpenseId"], out var expenseId))
                {
                    TempData["ErrorMessage"] = "Invalid expense ID.";
                    return RedirectToPage();
                }

                // Get existing expense to get project ID
                var existingExpense = await _expenseService.GetExpenseByIdAsync(expenseId);
                if (existingExpense == null)
                {
                    TempData["ErrorMessage"] = "Expense not found.";
                    return RedirectToPage();
                }

                var projectId = existingExpense.ProjectId;

                // Delete the expense using raw SQL to avoid trigger conflicts
                var rowsAffected = await _context.Database.ExecuteSqlRawAsync(
                    "DELETE FROM Expenses WHERE Id = {0}",
                    expenseId);
                    
                if (rowsAffected == 0)
                {
                    TempData["ErrorMessage"] = "Failed to delete expense.";
                    return RedirectToPage(new { id = projectId });
                }

                _logger.LogInformation("Expense {ExpenseId} deleted successfully", expenseId);
                TempData["SuccessMessage"] = "Expense deleted successfully!";
                return RedirectToPage(new { id = projectId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting expense");
                TempData["ErrorMessage"] = "An error occurred while deleting the expense. Please try again.";
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnPostUpdateInvoiceAsync()
        {
            try
            {
                _logger.LogInformation("UpdateInvoice POST received");
                _logger.LogInformation("Request Content-Type: {ContentType}", Request.ContentType);
                _logger.LogInformation("Request Method: {Method}", Request.Method);

                var formData = await Request.ReadFormAsync();
                _logger.LogInformation("Form data: {FormData}", string.Join(", ", formData.Select(kv => $"{kv.Key}=[{string.Join(", ", kv.Value)}]")));

                if (!Guid.TryParse(formData["InvoiceId"].FirstOrDefault(), out var invoiceId))
                {
                    TempData["ErrorMessage"] = "Invalid invoice ID.";
                    return RedirectToPage();
                }

                if (!Guid.TryParse(formData["ProjectId"].FirstOrDefault(), out var projectId))
                {
                    TempData["ErrorMessage"] = "Invalid project ID.";
                    return RedirectToPage();
                }

                // Parse form data
                if (!DateTime.TryParse(formData["InvoiceDate"].FirstOrDefault(), out var invoiceDate))
                {
                    TempData["ErrorMessage"] = "Invalid invoice date format.";
                    return RedirectToPage();
                }

                if (!DateTime.TryParse(formData["DueDate"].FirstOrDefault(), out var dueDate))
                {
                    TempData["ErrorMessage"] = "Invalid due date format.";
                    return RedirectToPage();
                }

                var notes = formData["Notes"].FirstOrDefault() ?? string.Empty;
                var status = formData["Status"].FirstOrDefault() ?? "Draft";

                // Update the invoice using raw SQL to avoid trigger conflicts
                var updatedAt = DateTime.UtcNow;
                
                await _context.Database.ExecuteSqlRawAsync(
                    "UPDATE Invoices SET InvoiceDate = {0}, DueDate = {1}, Notes = {2}, Status = {3}, UpdatedAt = {4} WHERE Id = {5}",
                    invoiceDate, dueDate, notes, status, updatedAt, invoiceId);

                _logger.LogInformation("Invoice {InvoiceId} updated successfully", invoiceId);
                TempData["SuccessMessage"] = "Invoice updated successfully!";
                return RedirectToPage(new { id = projectId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating invoice");
                TempData["ErrorMessage"] = "An error occurred while updating the invoice. Please try again.";
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnPostDeleteInvoiceAsync()
        {
            try
            {
                _logger.LogInformation("DeleteInvoice POST received");

                if (!Guid.TryParse(Request.Form["InvoiceId"], out var invoiceId))
                {
                    TempData["ErrorMessage"] = "Invalid invoice ID.";
                    return RedirectToPage();
                }

                // Get existing invoice to get project ID
                var existingInvoice = await _invoiceService.GetInvoiceByIdAsync(invoiceId);
                if (existingInvoice == null)
                {
                    TempData["ErrorMessage"] = "Invoice not found.";
                    return RedirectToPage();
                }

                var projectId = existingInvoice.ProjectId;

                // Get all invoice items to find which time entries and expenses were invoiced
                var invoiceItems = await _context.InvoiceItems
                    .Where(ii => ii.InvoiceId == invoiceId)
                    .ToListAsync();

                // Extract time entry and expense IDs from invoice items
                var timeEntryIds = new List<Guid>();
                var expenseIds = new List<Guid>();

                foreach (var item in invoiceItems)
                {
                    if (item.ItemType == "Time")
                    {
                        // For time entries, we need to find the original time entry
                        // The description format is: "MM/dd/yyyy - Description"
                        var timeEntries = await _context.TimeEntries
                            .Where(te => te.ProjectId == projectId && te.IsInvoiced)
                            .ToListAsync();
                        
                        // Find matching time entry by amount and description pattern
                        var matchingTimeEntry = timeEntries.FirstOrDefault(te => 
                            te.Amount == item.Amount && 
                            item.Description.Contains(te.Description));
                        
                        if (matchingTimeEntry != null)
                        {
                            timeEntryIds.Add(matchingTimeEntry.Id);
                        }
                    }
                    else if (item.ItemType == "Expense")
                    {
                        // For expenses, we need to find the original expense
                        var expenses = await _context.Expenses
                            .Where(e => e.ProjectId == projectId && e.IsInvoiced)
                            .ToListAsync();
                        
                        // Find matching expense by amount and description pattern
                        var matchingExpense = expenses.FirstOrDefault(e => 
                            e.Amount == item.Amount && 
                            item.Description.Contains(e.Description));
                        
                        if (matchingExpense != null)
                        {
                            expenseIds.Add(matchingExpense.Id);
                        }
                    }
                }

                // Mark time entries as not invoiced
                if (timeEntryIds.Any())
                {
                    var timeEntryIdsParam = string.Join(",", timeEntryIds.Select(id => $"'{id}'"));
                    var currentTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
                    await _context.Database.ExecuteSqlRawAsync(
                        $"UPDATE TimeEntries SET IsInvoiced = 0, UpdatedAt = '{currentTime}' WHERE Id IN ({timeEntryIdsParam})");
                }

                // Mark expenses as not invoiced
                if (expenseIds.Any())
                {
                    var expenseIdsParam = string.Join(",", expenseIds.Select(id => $"'{id}'"));
                    var currentTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
                    await _context.Database.ExecuteSqlRawAsync(
                        $"UPDATE Expenses SET IsInvoiced = 0, UpdatedAt = '{currentTime}' WHERE Id IN ({expenseIdsParam})");
                }

                // Delete invoice items first (to avoid foreign key constraint)
                await _context.Database.ExecuteSqlRawAsync(
                    "DELETE FROM InvoiceItems WHERE InvoiceId = {0}",
                    invoiceId);

                // Delete the invoice
                var rowsAffected = await _context.Database.ExecuteSqlRawAsync(
                    "DELETE FROM Invoices WHERE Id = {0}",
                    invoiceId);
                    
                if (rowsAffected == 0)
                {
                    TempData["ErrorMessage"] = "Failed to delete invoice.";
                    return RedirectToPage(new { id = projectId });
                }

                _logger.LogInformation("Invoice {InvoiceId} deleted successfully, marked {TimeEntryCount} time entries and {ExpenseCount} expenses as not invoiced", 
                    invoiceId, timeEntryIds.Count, expenseIds.Count);
                TempData["SuccessMessage"] = "Invoice deleted successfully! All associated time entries and expenses have been returned as unbilled.";
                return RedirectToPage(new { id = projectId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting invoice");
                TempData["ErrorMessage"] = "An error occurred while deleting the invoice. Please try again.";
                return RedirectToPage();
            }
        }
    }
}
