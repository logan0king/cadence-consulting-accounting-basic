using Microsoft.EntityFrameworkCore;
using CadenceAccounting.Data;
using CadenceAccounting.Models;
using MailKit.Net.Smtp;
using MimeKit;
using System.Text;
using System.Text.Json;

namespace CadenceAccounting.Services
{
    public class EmailService : IEmailService
    {
        private readonly ISettingsService _settingsService;
        private readonly ILogger<EmailService> _logger;

        public EmailService(ISettingsService settingsService, ILogger<EmailService> logger)
        {
            _settingsService = settingsService;
            _logger = logger;
        }

        public async Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = true)
        {
            try
            {
                var smtpServer = await _settingsService.GetSettingAsync("SMTP_Server") ?? "smtp.office365.com";
                var smtpPortSetting = await _settingsService.GetSettingAsync("SMTP_Port");
                var smtpPort = int.TryParse(smtpPortSetting, out var port) ? port : 587;
                var username = await _settingsService.GetSettingAsync("SMTP_Username") ?? "";
                var password = await _settingsService.GetSettingAsync("SMTP_Password") ?? "";
                var useSslSetting = await _settingsService.GetSettingAsync("SMTP_UseSSL");
                var useSsl = bool.TryParse(useSslSetting, out var ssl) ? ssl : true;
                var fromName = await _settingsService.GetSettingAsync("Email_FromName") ?? "Cadence Consulting LLC";
                var fromEmail = await _settingsService.GetSettingAsync("CompanyEmail") ?? "";

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(fromName, fromEmail));
                message.To.Add(new MailboxAddress("", to));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder();
                if (isHtml)
                {
                    bodyBuilder.HtmlBody = body;
                }
                else
                {
                    bodyBuilder.TextBody = body;
                }

                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();
                await client.ConnectAsync(smtpServer, smtpPort, useSsl);
                await client.AuthenticateAsync(username, password);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("Email sent successfully to {To}", to);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {To}", to);
                return false;
            }
        }

        public async Task<bool> SendInvoiceEmailAsync(string to, string invoiceNumber, byte[] pdfAttachment)
        {
            try
            {
                var subject = $"Invoice {invoiceNumber} - Cadence Consulting LLC";
                var body = $@"
                    <html>
                    <body>
                        <h2>Invoice {invoiceNumber}</h2>
                        <p>Dear Client,</p>
                        <p>Please find attached your invoice for services rendered.</p>
                        <p>Payment is due within 30 days of the invoice date.</p>
                        <p>If you have any questions, please don't hesitate to contact us.</p>
                        <br>
                        <p>Thank you for your business!</p>
                        <p>Cadence Consulting LLC</p>
                    </body>
                    </html>";

                var message = new MimeMessage();
                var fromName = await _settingsService.GetSettingAsync("Email_FromName") ?? "Cadence Consulting LLC";
                var fromEmail = await _settingsService.GetSettingAsync("CompanyEmail") ?? "";
                
                message.From.Add(new MailboxAddress(fromName, fromEmail));
                message.To.Add(new MailboxAddress("", to));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder();
                bodyBuilder.HtmlBody = body;
                bodyBuilder.Attachments.Add($"Invoice_{invoiceNumber}.pdf", pdfAttachment);
                message.Body = bodyBuilder.ToMessageBody();

                var smtpServer = await _settingsService.GetSettingAsync("SMTP_Server") ?? "smtp.office365.com";
                var smtpPortSetting = await _settingsService.GetSettingAsync("SMTP_Port");
                var smtpPort = int.TryParse(smtpPortSetting, out var port) ? port : 587;
                var username = await _settingsService.GetSettingAsync("SMTP_Username") ?? "";
                var password = await _settingsService.GetSettingAsync("SMTP_Password") ?? "";
                var useSslSetting = await _settingsService.GetSettingAsync("SMTP_UseSSL");
                var useSsl = bool.TryParse(useSslSetting, out var ssl) ? ssl : true;

                using var client = new SmtpClient();
                await client.ConnectAsync(smtpServer, smtpPort, useSsl);
                await client.AuthenticateAsync(username, password);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("Invoice email sent successfully to {To}", to);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send invoice email to {To}", to);
                return false;
            }
        }

        public async Task<bool> SendPaymentReminderEmailAsync(string to, string invoiceNumber, decimal amount)
        {
            try
            {
                var subject = $"Payment Reminder - Invoice {invoiceNumber}";
                var body = $@"
                    <html>
                    <body>
                        <h2>Payment Reminder</h2>
                        <p>Dear Client,</p>
                        <p>This is a friendly reminder that payment for Invoice {invoiceNumber} in the amount of {amount:C} is now due.</p>
                        <p>Please remit payment at your earliest convenience.</p>
                        <p>If you have already sent payment, please disregard this notice.</p>
                        <br>
                        <p>Thank you for your prompt attention to this matter.</p>
                        <p>Cadence Consulting LLC</p>
                    </body>
                    </html>";

                return await SendEmailAsync(to, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send payment reminder email to {To}", to);
                return false;
            }
        }

        public async Task<bool> TestEmailConnectionAsync()
        {
            try
            {
                var smtpServer = await _settingsService.GetSettingAsync("SMTP_Server") ?? "smtp.office365.com";
                var smtpPortSetting = await _settingsService.GetSettingAsync("SMTP_Port");
                var smtpPort = int.TryParse(smtpPortSetting, out var port) ? port : 587;
                var username = await _settingsService.GetSettingAsync("SMTP_Username") ?? "";
                var password = await _settingsService.GetSettingAsync("SMTP_Password") ?? "";
                var useSslSetting = await _settingsService.GetSettingAsync("SMTP_UseSSL");
                var useSsl = bool.TryParse(useSslSetting, out var ssl) ? ssl : true;

                using var client = new SmtpClient();
                await client.ConnectAsync(smtpServer, smtpPort, useSsl);
                await client.AuthenticateAsync(username, password);
                await client.DisconnectAsync(true);

                _logger.LogInformation("Email connection test successful");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email connection test failed");
                return false;
            }
        }
    }

    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuditService> _logger;

        public AuditService(ApplicationDbContext context, ILogger<AuditService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task LogActionAsync(string tableName, Guid recordId, string action, string? oldValues, string? newValues, Guid userId)
        {
            try
            {
                var auditLog = new AuditLog
                {
                    TableName = tableName,
                    RecordId = recordId,
                    Action = action,
                    OldValues = oldValues,
                    NewValues = newValues,
                    ChangedBy = userId,
                    ChangedAt = DateTime.UtcNow
                };

                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();

                _logger.LogDebug("Audit log created for {TableName} {Action} by user {UserId}", tableName, action, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create audit log for {TableName} {Action}", tableName, action);
            }
        }

        public async Task<IEnumerable<AuditLog>> GetAuditLogsAsync(string? tableName = null, Guid? recordId = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.AuditLogs
                .Include(a => a.ChangedByUser)
                .AsQueryable();

            if (!string.IsNullOrEmpty(tableName))
                query = query.Where(a => a.TableName == tableName);

            if (recordId.HasValue)
                query = query.Where(a => a.RecordId == recordId.Value);

            if (startDate.HasValue)
                query = query.Where(a => a.ChangedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(a => a.ChangedAt <= endDate.Value);

            return await query
                .OrderByDescending(a => a.ChangedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<AuditLog>> GetAuditLogsByUserAsync(Guid userId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.AuditLogs
                .Include(a => a.ChangedByUser)
                .Where(a => a.ChangedBy == userId);

            if (startDate.HasValue)
                query = query.Where(a => a.ChangedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(a => a.ChangedAt <= endDate.Value);

            return await query
                .OrderByDescending(a => a.ChangedAt)
                .ToListAsync();
        }

        public async Task<byte[]> ExportAuditLogsAsync(DateTime startDate, DateTime endDate)
        {
            var auditLogs = await GetAuditLogsAsync(startDate: startDate, endDate: endDate);
            
            var csv = new StringBuilder();
            csv.AppendLine("Timestamp,User,Table,Record ID,Action,Old Values,New Values");

            foreach (var log in auditLogs)
            {
                csv.AppendLine($"{log.ChangedAt:yyyy-MM-dd HH:mm:ss},{log.ChangedByUser?.Username},{log.TableName},{log.RecordId},{log.Action},\"{log.OldValues}\",\"{log.NewValues}\"");
            }

            return Encoding.UTF8.GetBytes(csv.ToString());
        }
    }
}
