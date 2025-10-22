using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CadenceAccounting.Services;
using CadenceAccounting.Models;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace CadenceAccounting.Pages.Invoices
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly IInvoiceService _invoiceService;
        private readonly IProjectService _projectService;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(IInvoiceService invoiceService, IProjectService projectService, ILogger<IndexModel> logger)
        {
            _invoiceService = invoiceService;
            _projectService = projectService;
            _logger = logger;
        }

        public IEnumerable<Invoice> Invoices { get; set; } = new List<Invoice>();
        public IEnumerable<Project> Projects { get; set; } = new List<Project>();

        [BindProperty]
        public CreateInvoiceModel Input { get; set; } = new();

        public class CreateInvoiceModel
        {
            [Required]
            [Display(Name = "Project")]
            public Guid ProjectId { get; set; }

            [Required]
            [Display(Name = "Invoice Date")]
            [DataType(DataType.Date)]
            public DateTime InvoiceDate { get; set; } = DateTime.Today;

            [Required]
            [Display(Name = "Due Date")]
            [DataType(DataType.Date)]
            public DateTime DueDate { get; set; } = DateTime.Today.AddDays(30);

            [Display(Name = "Notes")]
            public string? Notes { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            Invoices = await _invoiceService.GetAllInvoicesAsync();
            Projects = await _projectService.GetAllProjectsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                Invoices = await _invoiceService.GetAllInvoicesAsync();
                Projects = await _projectService.GetAllProjectsAsync();
                return Page();
            }

            try
            {
                var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
                var invoice = await _invoiceService.CreateInvoiceFromProjectAsync(
                    Input.ProjectId, 
                    Input.InvoiceDate, 
                    Input.DueDate,
                    userId);

                if (!string.IsNullOrEmpty(Input.Notes))
                {
                    invoice.Notes = Input.Notes;
                    await _invoiceService.UpdateInvoiceAsync(invoice);
                }

                TempData["SuccessMessage"] = $"Invoice {invoice.InvoiceNumber} created successfully!";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating invoice");
                TempData["ErrorMessage"] = "An error occurred while creating the invoice. Please try again.";
                
                Invoices = await _invoiceService.GetAllInvoicesAsync();
                Projects = await _projectService.GetAllProjectsAsync();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(Guid id)
        {
            try
            {
                await _invoiceService.DeleteInvoiceAsync(id);
                TempData["SuccessMessage"] = "Invoice deleted successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting invoice {Id}", id);
                TempData["ErrorMessage"] = "An error occurred while deleting the invoice.";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnGetPdfAsync(Guid id)
        {
            try
            {
                var pdfBytes = await _invoiceService.GenerateInvoicePdfAsync(id);
                return File(pdfBytes, "application/pdf", $"Invoice_{id}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating PDF for invoice {Id}", id);
                TempData["ErrorMessage"] = "An error occurred while generating the PDF.";
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnPostSendEmailAsync(Guid id)
        {
            try
            {
                var invoice = await _invoiceService.GetInvoiceByIdAsync(id);
                if (invoice == null)
                {
                    TempData["ErrorMessage"] = "Invoice not found.";
                    return RedirectToPage();
                }

                var success = await _invoiceService.SendInvoiceEmailAsync(id, invoice.Project.ClientEmail ?? "");
                if (success)
                {
                    TempData["SuccessMessage"] = $"Invoice {invoice.InvoiceNumber} sent successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to send invoice email. Please check your email settings.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending invoice email {Id}", id);
                TempData["ErrorMessage"] = "An error occurred while sending the invoice email.";
            }

            return RedirectToPage();
        }
    }

    public class DetailsModel : PageModel
    {
        private readonly IInvoiceService _invoiceService;
        private readonly IPaymentService _paymentService;
        private readonly ILogger<DetailsModel> _logger;

        public DetailsModel(IInvoiceService invoiceService, IPaymentService paymentService, ILogger<DetailsModel> logger)
        {
            _invoiceService = invoiceService;
            _paymentService = paymentService;
            _logger = logger;
        }

        public Invoice? Invoice { get; set; }
        public IEnumerable<Payment> Payments { get; set; } = new List<Payment>();

        [BindProperty]
        public PaymentInputModel PaymentInput { get; set; } = new();

        public class PaymentInputModel
        {
            [Required]
            [Display(Name = "Amount")]
            [Range(0.01, 100000, ErrorMessage = "Amount must be between $0.01 and $100,000")]
            public decimal Amount { get; set; }

            [Required]
            [Display(Name = "Payment Date")]
            [DataType(DataType.Date)]
            public DateTime PaymentDate { get; set; } = DateTime.Today;

            [Required]
            [Display(Name = "Payment Method")]
            public string PaymentMethod { get; set; } = string.Empty;

            [Display(Name = "Reference")]
            public string? Reference { get; set; }

            [Display(Name = "Notes")]
            public string? Notes { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            Invoice = await _invoiceService.GetInvoiceByIdAsync(id);
            if (Invoice == null)
            {
                TempData["ErrorMessage"] = "Invoice not found.";
                return RedirectToPage("Index");
            }

            Payments = await _paymentService.GetPaymentsByInvoiceAsync(id);
            return Page();
        }

        public async Task<IActionResult> OnPostAddPaymentAsync(Guid id)
        {
            if (!ModelState.IsValid)
            {
                Invoice = await _invoiceService.GetInvoiceByIdAsync(id);
                Payments = await _paymentService.GetPaymentsByInvoiceAsync(id);
                return Page();
            }

            try
            {
                var payment = new Payment
                {
                    InvoiceId = id,
                    Amount = PaymentInput.Amount,
                    PaymentDate = PaymentInput.PaymentDate,
                    PaymentMethod = PaymentInput.PaymentMethod,
                    Reference = PaymentInput.Reference,
                    Notes = PaymentInput.Notes
                };

                await _paymentService.CreatePaymentAsync(payment);

                TempData["SuccessMessage"] = "Payment added successfully!";
                return RedirectToPage(new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding payment for invoice {Id}", id);
                TempData["ErrorMessage"] = "An error occurred while adding the payment. Please try again.";
                
                Invoice = await _invoiceService.GetInvoiceByIdAsync(id);
                Payments = await _paymentService.GetPaymentsByInvoiceAsync(id);
                return Page();
            }
        }
    }
}
