using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CadenceAccounting.Data;
using CadenceAccounting.Models;
using System.ComponentModel.DataAnnotations;

namespace CadenceAccounting.Pages.Companies
{
    [IgnoreAntiforgeryToken]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ApplicationDbContext context, ILogger<IndexModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IEnumerable<Company> Companies { get; set; } = new List<Company>();

        [BindProperty]
        public CompanyInputModel Input { get; set; } = new();

        [BindProperty]
        public CompanyInputModel EditInput { get; set; } = new();

        public class CompanyInputModel
        {
            public Guid Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string? Email { get; set; }
            public string? Phone { get; set; }
            public string? Address { get; set; }
            public string? City { get; set; }
            public string? State { get; set; }
            public string? ZipCode { get; set; }
            public string? TaxId { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            Companies = await _context.Companies.OrderBy(c => c.Name).ToListAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            _logger.LogInformation("Company creation POST received. ModelState.IsValid: {IsValid}", ModelState.IsValid);
            _logger.LogInformation("Request Content-Type: {ContentType}", Request.ContentType);
            _logger.LogInformation("Request Method: {Method}", Request.Method);
            _logger.LogInformation("Form data: Name={Name}, Email={Email}, Phone={Phone}, Address={Address}, City={City}, State={State}, ZipCode={ZipCode}, TaxId={TaxId}",
                Request.Form["Input.Name"],
                Request.Form["Input.Email"],
                Request.Form["Input.Phone"],
                Request.Form["Input.Address"],
                Request.Form["Input.City"],
                Request.Form["Input.State"],
                Request.Form["Input.ZipCode"],
                Request.Form["Input.TaxId"]);
            
            _logger.LogInformation("Model binding - Input.Name: {Name}, Input.Email: {Email}", Input.Name, Input.Email);

            // Manual model binding - always do this since automatic binding isn't working
            Input.Name = Request.Form["Input.Name"].ToString();
            Input.Email = Request.Form["Input.Email"].ToString();
            Input.Phone = Request.Form["Input.Phone"].ToString();
            Input.Address = Request.Form["Input.Address"].ToString();
            Input.City = Request.Form["Input.City"].ToString();
            Input.State = Request.Form["Input.State"].ToString();
            Input.ZipCode = Request.Form["Input.ZipCode"].ToString();
            Input.TaxId = Request.Form["Input.TaxId"].ToString();
            
            _logger.LogInformation("Manual binding applied - Input.Name: {Name}", Input.Name);

            // Simple validation - only require company name
            if (string.IsNullOrWhiteSpace(Input.Name))
            {
                TempData["ErrorMessage"] = "Company name is required.";
                Companies = await _context.Companies.OrderBy(c => c.Name).ToListAsync();
                return Page();
            }

            try
            {
                var companyId = Guid.NewGuid();
                var createdAt = DateTime.UtcNow;

                // Use raw SQL to avoid database trigger conflicts
                await _context.Database.ExecuteSqlInterpolatedAsync($@"
                    INSERT INTO Companies (Id, Name, Email, Phone, Address, City, State, ZipCode, TaxId, CreatedAt, UpdatedAt)
                    VALUES ({companyId}, {Input.Name}, {Input.Email}, {Input.Phone}, {Input.Address}, {Input.City}, {Input.State}, {Input.ZipCode}, {Input.TaxId}, {createdAt}, {createdAt})");

                TempData["SuccessMessage"] = "Company created successfully!";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating company");
                TempData["ErrorMessage"] = "An error occurred while creating the company. Please try again.";
                
                Companies = await _context.Companies.OrderBy(c => c.Name).ToListAsync();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostEditAsync()
        {
            _logger.LogInformation("Company edit POST received. ModelState.IsValid: {IsValid}", ModelState.IsValid);
            _logger.LogInformation("Request Content-Type: {ContentType}", Request.ContentType);
            _logger.LogInformation("Request Method: {Method}", Request.Method);
            _logger.LogInformation("Edit form data: Id={Id}, Name={Name}, Email={Email}, Phone={Phone}, Address={Address}, City={City}, State={State}, ZipCode={ZipCode}, TaxId={TaxId}",
                Request.Form["EditInput.Id"],
                Request.Form["EditInput.Name"],
                Request.Form["EditInput.Email"],
                Request.Form["EditInput.Phone"],
                Request.Form["EditInput.Address"],
                Request.Form["EditInput.City"],
                Request.Form["EditInput.State"],
                Request.Form["EditInput.ZipCode"],
                Request.Form["EditInput.TaxId"]);

            _logger.LogInformation("Edit model binding - EditInput.Name: {Name}, EditInput.Email: {Email}", EditInput.Name, EditInput.Email);

            // Manual model binding - always do this since automatic binding isn't working
            EditInput.Id = Guid.Parse(Request.Form["EditInput.Id"].ToString());
            EditInput.Name = Request.Form["EditInput.Name"].ToString();
            EditInput.Email = Request.Form["EditInput.Email"].ToString();
            EditInput.Phone = Request.Form["EditInput.Phone"].ToString();
            EditInput.Address = Request.Form["EditInput.Address"].ToString();
            EditInput.City = Request.Form["EditInput.City"].ToString();
            EditInput.State = Request.Form["EditInput.State"].ToString();
            EditInput.ZipCode = Request.Form["EditInput.ZipCode"].ToString();
            EditInput.TaxId = Request.Form["EditInput.TaxId"].ToString();
            
            _logger.LogInformation("Manual edit binding applied - EditInput.Name: {Name}", EditInput.Name);

            // Simple validation - only require company name
            if (string.IsNullOrWhiteSpace(EditInput.Name))
            {
                TempData["ErrorMessage"] = "Company name is required.";
                Companies = await _context.Companies.OrderBy(c => c.Name).ToListAsync();
                return Page();
            }

            try
            {
                var company = await _context.Companies.FindAsync(EditInput.Id);
                if (company == null)
                {
                    TempData["ErrorMessage"] = "Company not found.";
                    Companies = await _context.Companies.OrderBy(c => c.Name).ToListAsync();
                    return Page();
                }

                var updatedAt = DateTime.UtcNow;

                // Use raw SQL to avoid database trigger conflicts
                var rowsAffected = await _context.Database.ExecuteSqlInterpolatedAsync($@"
                    UPDATE Companies 
                    SET Name = {EditInput.Name}, 
                        Email = {EditInput.Email}, 
                        Phone = {EditInput.Phone}, 
                        Address = {EditInput.Address}, 
                        City = {EditInput.City}, 
                        State = {EditInput.State}, 
                        ZipCode = {EditInput.ZipCode}, 
                        TaxId = {EditInput.TaxId}, 
                        UpdatedAt = {updatedAt}
                    WHERE Id = {EditInput.Id}");

                if (rowsAffected == 0)
                {
                    TempData["ErrorMessage"] = "Company not found.";
                    Companies = await _context.Companies.OrderBy(c => c.Name).ToListAsync();
                    return Page();
                }

                TempData["SuccessMessage"] = "Company updated successfully!";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating company");
                TempData["ErrorMessage"] = "An error occurred while updating the company. Please try again.";
                
                Companies = await _context.Companies.OrderBy(c => c.Name).ToListAsync();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(Guid id)
        {
            try
            {
                var company = await _context.Companies.FindAsync(id);
                if (company == null)
                {
                    TempData["ErrorMessage"] = "Company not found.";
                    return RedirectToPage();
                }

                // Check if company is being used by any projects
                var projectsUsingCompany = await _context.Projects.AnyAsync(p => p.CompanyId == id);
                if (projectsUsingCompany)
                {
                    TempData["ErrorMessage"] = "Cannot delete company because it is being used by one or more projects.";
                    return RedirectToPage();
                }

                // Use raw SQL to avoid database trigger conflicts
                var rowsAffected = await _context.Database.ExecuteSqlInterpolatedAsync($@"
                    DELETE FROM Companies WHERE Id = {id}");

                if (rowsAffected == 0)
                {
                    TempData["ErrorMessage"] = "Company not found.";
                    return RedirectToPage();
                }

                TempData["SuccessMessage"] = "Company deleted successfully!";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting company");
                TempData["ErrorMessage"] = "An error occurred while deleting the company. Please try again.";
                return RedirectToPage();
            }
        }
    }
}
