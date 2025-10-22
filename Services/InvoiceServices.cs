using Microsoft.EntityFrameworkCore;
using CadenceAccounting.Data;
using CadenceAccounting.Models;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.Text;
using System.Security.Claims;

namespace CadenceAccounting.Services
{
    public class InvoiceService : IInvoiceService
    {
        private readonly ApplicationDbContext _context;
        private readonly ISettingsService _settingsService;
        private readonly ILogger<InvoiceService> _logger;

        public InvoiceService(ApplicationDbContext context, ISettingsService settingsService, ILogger<InvoiceService> logger)
        {
            _context = context;
            _settingsService = settingsService;
            _logger = logger;
        }

        public async Task<IEnumerable<Invoice>> GetAllInvoicesAsync()
        {
            return await _context.Invoices
                .Include(i => i.Project)
                .Include(i => i.CreatedByUser)
                .Include(i => i.InvoiceItems)
                .Include(i => i.Payments)
                .OrderByDescending(i => i.InvoiceDate)
                .ToListAsync();
        }

        public async Task<Invoice?> GetInvoiceByIdAsync(Guid id)
        {
            return await _context.Invoices
                .Include(i => i.Project)
                .Include(i => i.CreatedByUser)
                .Include(i => i.InvoiceItems)
                .Include(i => i.Payments)
                .FirstOrDefaultAsync(i => i.Id == id);
        }

        public async Task<Invoice?> GetInvoiceByNumberAsync(string invoiceNumber)
        {
            return await _context.Invoices
                .Include(i => i.Project)
                .Include(i => i.CreatedByUser)
                .Include(i => i.InvoiceItems)
                .Include(i => i.Payments)
                .FirstOrDefaultAsync(i => i.InvoiceNumber == invoiceNumber);
        }

        public async Task<Invoice> CreateInvoiceAsync(Invoice invoice)
        {
            invoice.CreatedAt = DateTime.UtcNow;
            invoice.UpdatedAt = DateTime.UtcNow;

            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Invoice {InvoiceNumber} created successfully", invoice.InvoiceNumber);
            return invoice;
        }

        public async Task<Invoice> UpdateInvoiceAsync(Invoice invoice)
        {
            invoice.UpdatedAt = DateTime.UtcNow;
            _context.Invoices.Update(invoice);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Invoice {InvoiceNumber} updated successfully", invoice.InvoiceNumber);
            return invoice;
        }

        public async Task<bool> DeleteInvoiceAsync(Guid id)
        {
            var invoice = await GetInvoiceByIdAsync(id);
            if (invoice == null) return false;

            _context.Invoices.Remove(invoice);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Invoice {InvoiceNumber} deleted successfully", invoice.InvoiceNumber);
            return true;
        }

        public async Task<string> GenerateInvoiceNumberAsync(string projectNumber)
        {
            var format = await _settingsService.GetSettingAsync("InvoiceNumberFormat") ?? "INV-{ProjectNumber}-{Year}-{SequentialNumber}";
            
            var year = DateTime.Now.Year.ToString();
            var sequentialNumber = await GetNextSequentialNumberAsync(projectNumber, year);
            
            return format
                .Replace("{ProjectNumber}", projectNumber)
                .Replace("{Year}", year)
                .Replace("{SequentialNumber}", sequentialNumber.ToString("D3"));
        }

        private async Task<int> GetNextSequentialNumberAsync(string projectNumber, string year)
        {
            var lastInvoice = await _context.Invoices
                .Where(i => i.InvoiceNumber.Contains(projectNumber) && i.InvoiceNumber.Contains(year))
                .OrderByDescending(i => i.InvoiceNumber)
                .FirstOrDefaultAsync();

            if (lastInvoice == null) return 1;

            // Extract sequential number from last invoice
            var parts = lastInvoice.InvoiceNumber.Split('-');
            if (parts.Length >= 3 && int.TryParse(parts[parts.Length - 1], out int lastNumber))
            {
                return lastNumber + 1;
            }

            return 1;
        }

        public async Task<Invoice> CreateInvoiceFromProjectAsync(Guid projectId, DateTime invoiceDate, DateTime dueDate, Guid userId)
        {
            var project = await _context.Projects
                .Include(p => p.TimeEntries.Where(t => t.IsBillable))
                .Include(p => p.Expenses.Where(e => e.IsBillable))
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project == null)
                throw new ArgumentException("Project not found");

            var invoiceNumber = await GenerateInvoiceNumberAsync(project.ProjectNumber);

            var invoice = new Invoice
            {
                ProjectId = projectId,
                InvoiceNumber = invoiceNumber,
                InvoiceDate = invoiceDate,
                DueDate = dueDate,
                Subtotal = project.TotalAmount,
                TaxAmount = 0, // Calculate tax based on project tax type
                TotalAmount = project.TotalAmount,
                Status = "Draft",
                CreatedBy = userId
            };

            // Add time entries as invoice items
            foreach (var timeEntry in project.TimeEntries.Where(t => t.IsBillable))
            {
                invoice.InvoiceItems.Add(new InvoiceItem
                {
                    Description = $"Time: {timeEntry.Description}",
                    Quantity = timeEntry.Hours,
                    Rate = timeEntry.Rate,
                    Amount = timeEntry.Amount,
                    ItemType = "Time"
                });
            }

            // Add expenses as invoice items
            foreach (var expense in project.Expenses.Where(e => e.IsBillable))
            {
                invoice.InvoiceItems.Add(new InvoiceItem
                {
                    Description = $"Expense: {expense.Description}",
                    Quantity = 1,
                    Rate = expense.Amount,
                    Amount = expense.Amount,
                    ItemType = "Expense"
                });
            }

            return await CreateInvoiceAsync(invoice);
        }

        public async Task<byte[]> GenerateInvoicePdfAsync(Guid invoiceId)
        {
            var invoice = await GetInvoiceByIdAsync(invoiceId);
            if (invoice == null)
                throw new ArgumentException("Invoice not found");

            using var memoryStream = new MemoryStream();
            var document = new Document(PageSize.A4, 50, 50, 50, 50);
            var writer = PdfWriter.GetInstance(document, memoryStream);

            document.Open();

            // Add company header
            var companyName = await _settingsService.GetSettingAsync("CompanyName") ?? "Cadence Consulting LLC";
            var companyEmail = await _settingsService.GetSettingAsync("CompanyEmail") ?? "";
            var companyPhone = await _settingsService.GetSettingAsync("CompanyPhone") ?? "";

            var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18);
            var normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 12);
            var smallFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);

            var headerTable = new PdfPTable(2);
            headerTable.WidthPercentage = 100;
            headerTable.SetWidths(new float[] { 2f, 1f });

            // Company info
            var companyCell = new PdfPCell(new Phrase(companyName, headerFont));
            companyCell.Border = Rectangle.NO_BORDER;
            companyCell.VerticalAlignment = Element.ALIGN_TOP;
            headerTable.AddCell(companyCell);

            // Invoice info
            var invoiceInfo = $"Invoice: {invoice.InvoiceNumber}\nDate: {invoice.InvoiceDate:MM/dd/yyyy}\nDue: {invoice.DueDate:MM/dd/yyyy}";
            var invoiceCell = new PdfPCell(new Phrase(invoiceInfo, normalFont));
            invoiceCell.Border = Rectangle.NO_BORDER;
            invoiceCell.HorizontalAlignment = Element.ALIGN_RIGHT;
            invoiceCell.VerticalAlignment = Element.ALIGN_TOP;
            headerTable.AddCell(invoiceCell);

            document.Add(headerTable);
            document.Add(new Paragraph("\n"));

            // Client info
            var clientInfo = $"Bill To:\n{invoice.Project.ClientName}\n{invoice.Project.ClientAddress}\n{invoice.Project.ClientEmail}";
            document.Add(new Phrase(clientInfo, normalFont));
            document.Add(new Paragraph("\n"));

            // Invoice items table
            var itemsTable = new PdfPTable(4);
            itemsTable.WidthPercentage = 100;
            itemsTable.SetWidths(new float[] { 3f, 1f, 1f, 1f });

            // Header row
            var headers = new[] { "Description", "Qty", "Rate", "Amount" };
            foreach (var header in headers)
            {
                var cell = new PdfPCell(new Phrase(header, FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12)));
                cell.BackgroundColor = BaseColor.LIGHT_GRAY;
                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                itemsTable.AddCell(cell);
            }

            // Data rows
            foreach (var item in invoice.InvoiceItems)
            {
                itemsTable.AddCell(new Phrase(item.Description, normalFont));
                itemsTable.AddCell(new Phrase(item.Quantity.ToString("F2"), normalFont));
                itemsTable.AddCell(new Phrase(item.Rate.ToString("C"), normalFont));
                itemsTable.AddCell(new Phrase(item.Amount.ToString("C"), normalFont));
            }

            document.Add(itemsTable);
            document.Add(new Paragraph("\n"));

            // Totals
            var totalsTable = new PdfPTable(2);
            totalsTable.WidthPercentage = 30;
            totalsTable.SetWidths(new float[] { 1f, 1f });
            totalsTable.HorizontalAlignment = Element.ALIGN_RIGHT;

            totalsTable.AddCell(new Phrase("Subtotal:", normalFont));
            totalsTable.AddCell(new Phrase(invoice.Subtotal.ToString("C"), normalFont));
            
            if (invoice.TaxAmount > 0)
            {
                totalsTable.AddCell(new Phrase("Tax:", normalFont));
                totalsTable.AddCell(new Phrase(invoice.TaxAmount.ToString("C"), normalFont));
            }

            var totalFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14);
            totalsTable.AddCell(new Phrase("Total:", totalFont));
            totalsTable.AddCell(new Phrase(invoice.TotalAmount.ToString("C"), totalFont));

            document.Add(totalsTable);

            // Footer
            var footerText = await _settingsService.GetSettingAsync("InvoiceFooterText") ?? "Thank you for your business!";
            document.Add(new Paragraph("\n\n" + footerText, smallFont));

            document.Close();
            writer.Close();

            return memoryStream.ToArray();
        }

        public Task<bool> SendInvoiceEmailAsync(Guid invoiceId, string toEmail)
        {
            // This will be implemented with the email service
            return Task.FromResult(true);
        }
    }

    public class PaymentService : IPaymentService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(ApplicationDbContext context, ILogger<PaymentService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<Payment>> GetPaymentsByInvoiceAsync(Guid invoiceId)
        {
            return await _context.Payments
                .Where(p => p.InvoiceId == invoiceId)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();
        }

        public async Task<Payment?> GetPaymentByIdAsync(Guid id)
        {
            return await _context.Payments
                .Include(p => p.Invoice)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Payment> CreatePaymentAsync(Payment payment)
        {
            payment.CreatedAt = DateTime.UtcNow;

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Payment of {Amount} created for invoice {InvoiceId}", payment.Amount, payment.InvoiceId);
            return payment;
        }

        public async Task<Payment> UpdatePaymentAsync(Payment payment)
        {
            _context.Payments.Update(payment);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Payment {Id} updated successfully", payment.Id);
            return payment;
        }

        public async Task<bool> DeletePaymentAsync(Guid id)
        {
            var payment = await GetPaymentByIdAsync(id);
            if (payment == null) return false;

            _context.Payments.Remove(payment);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Payment {Id} deleted successfully", id);
            return true;
        }

        public async Task<decimal> GetTotalPaidByInvoiceAsync(Guid invoiceId)
        {
            return await _context.Payments
                .Where(p => p.InvoiceId == invoiceId)
                .SumAsync(p => p.Amount);
        }

        public async Task<bool> IsInvoicePaidAsync(Guid invoiceId)
        {
            var invoice = await _context.Invoices.FindAsync(invoiceId);
            if (invoice == null) return false;

            var totalPaid = await GetTotalPaidByInvoiceAsync(invoiceId);
            return totalPaid >= invoice.TotalAmount;
        }

        public async Task<IEnumerable<Payment>> GetUnmatchedPaymentsAsync()
        {
            return await _context.Payments
                .Where(p => string.IsNullOrEmpty(p.BankTransactionId))
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();
        }

        public async Task<bool> MatchPaymentToInvoiceAsync(Guid paymentId, Guid invoiceId)
        {
            var payment = await GetPaymentByIdAsync(paymentId);
            var invoice = await _context.Invoices.FindAsync(invoiceId);

            if (payment == null || invoice == null) return false;

            payment.InvoiceId = invoiceId;
            _context.Payments.Update(payment);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Payment {PaymentId} matched to invoice {InvoiceId}", paymentId, invoiceId);
            return true;
        }
    }

    public class SettingsService : ISettingsService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SettingsService> _logger;

        public SettingsService(ApplicationDbContext context, ILogger<SettingsService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<string?> GetSettingAsync(string key)
        {
            var setting = await _context.SystemSettings
                .FirstOrDefaultAsync(s => s.Key == key);
            return setting?.Value;
        }

        public async Task<T?> GetSettingAsync<T>(string key)
        {
            var value = await GetSettingAsync(key);
            if (string.IsNullOrEmpty(value)) return default(T);

            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return default(T);
            }
        }

        public async Task SetSettingAsync(string key, string value)
        {
            var setting = await _context.SystemSettings
                .FirstOrDefaultAsync(s => s.Key == key);

            if (setting == null)
            {
                setting = new SystemSetting
                {
                    Key = key,
                    Value = value,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.SystemSettings.Add(setting);
            }
            else
            {
                setting.Value = value;
                setting.UpdatedAt = DateTime.UtcNow;
                _context.SystemSettings.Update(setting);
            }

            await _context.SaveChangesAsync();
        }

        public async Task SetSettingAsync<T>(string key, T value)
        {
            await SetSettingAsync(key, value?.ToString() ?? string.Empty);
        }

        public async Task<IEnumerable<SystemSetting>> GetSettingsByCategoryAsync(string category)
        {
            return await _context.SystemSettings
                .Where(s => s.Category == category)
                .OrderBy(s => s.Key)
                .ToListAsync();
        }

        public async Task<Dictionary<string, string>> GetAllSettingsAsync()
        {
            var settings = await _context.SystemSettings.ToListAsync();
            return settings.ToDictionary(s => s.Key, s => s.Value ?? string.Empty);
        }

        public async Task<bool> DeleteSettingAsync(string key)
        {
            var setting = await _context.SystemSettings
                .FirstOrDefaultAsync(s => s.Key == key);

            if (setting == null) return false;

            _context.SystemSettings.Remove(setting);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Setting {Key} deleted successfully", key);
            return true;
        }
    }
}
