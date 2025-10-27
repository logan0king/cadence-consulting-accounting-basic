using CadenceAccounting.Models;

namespace CadenceAccounting.Services
{
    public interface IUserService
    {
        Task<IEnumerable<User>> GetAllUsersAsync();
        Task<User?> GetUserByIdAsync(Guid id);
        Task<User?> GetUserByUsernameAsync(string username);
        Task<User?> GetUserByEmailAsync(string email);
        Task<bool> ValidateUserAsync(string username, string password);
        Task<User> CreateUserAsync(User user);
        Task<User> UpdateUserAsync(User user);
        Task<bool> DeleteUserAsync(Guid id);
        Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword);
    }

    public interface IProjectService
    {
        Task<IEnumerable<Project>> GetAllProjectsAsync();
        Task<Project?> GetProjectByIdAsync(Guid id);
        Task<Project> CreateProjectAsync(Project project);
        Task<Project> UpdateProjectAsync(Project project);
        Task<bool> DeleteProjectAsync(Guid id);
        Task<IEnumerable<Project>> GetProjectsByStatusAsync(string status);
        Task<decimal> GetProjectBudgetUtilizationAsync(Guid projectId);
        Task<bool> IsProjectOverBudgetAsync(Guid projectId);
    }

    public interface ITimeEntryService
    {
        Task<IEnumerable<TimeEntry>> GetTimeEntriesByProjectAsync(Guid projectId);
        Task<IEnumerable<TimeEntry>> GetTimeEntriesByUserAsync(Guid userId);
        Task<IEnumerable<TimeEntry>> GetTimeEntriesByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<TimeEntry?> GetTimeEntryByIdAsync(Guid id);
        Task<TimeEntry> CreateTimeEntryAsync(TimeEntry timeEntry);
        Task<TimeEntry> UpdateTimeEntryAsync(TimeEntry timeEntry);
        Task<bool> DeleteTimeEntryAsync(Guid id);
        Task<decimal> GetTotalHoursByProjectAsync(Guid projectId);
        Task<decimal> GetTotalAmountByProjectAsync(Guid projectId);
    }

    public interface IExpenseService
    {
        Task<IEnumerable<Expense>> GetExpensesByProjectAsync(Guid projectId);
        Task<IEnumerable<Expense>> GetExpensesByUserAsync(Guid userId);
        Task<IEnumerable<Expense>> GetExpensesByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<Expense?> GetExpenseByIdAsync(Guid id);
        Task<Expense> CreateExpenseAsync(Expense expense);
        Task<Expense> UpdateExpenseAsync(Expense expense);
        Task<bool> DeleteExpenseAsync(Guid id);
        Task<decimal> GetTotalAmountByProjectAsync(Guid projectId);
        Task<IEnumerable<string>> GetExpenseCategoriesAsync();
    }

    public interface IInvoiceService
    {
        Task<IEnumerable<Invoice>> GetAllInvoicesAsync();
        Task<Invoice?> GetInvoiceByIdAsync(Guid id);
        Task<Invoice?> GetInvoiceByNumberAsync(string invoiceNumber);
        Task<Invoice> CreateInvoiceAsync(Invoice invoice);
        Task<Invoice> UpdateInvoiceAsync(Invoice invoice);
        Task<bool> DeleteInvoiceAsync(Guid id);
        Task<string> GenerateInvoiceNumberAsync(string projectNumber);
        Task<Invoice> CreateInvoiceFromProjectAsync(Guid projectId, DateTime invoiceDate, DateTime dueDate, Guid userId);
        Task<byte[]> GenerateInvoicePdfAsync(Guid invoiceId);
        Task<bool> SendInvoiceEmailAsync(Guid invoiceId, string toEmail);
    }

    public interface IPaymentService
    {
        Task<IEnumerable<Payment>> GetPaymentsByInvoiceAsync(Guid invoiceId);
        Task<Payment?> GetPaymentByIdAsync(Guid id);
        Task<Payment> CreatePaymentAsync(Payment payment);
        Task<Payment> UpdatePaymentAsync(Payment payment);
        Task<bool> DeletePaymentAsync(Guid id);
        Task<decimal> GetTotalPaidByInvoiceAsync(Guid invoiceId);
        Task<bool> IsInvoicePaidAsync(Guid invoiceId);
        Task<IEnumerable<Payment>> GetUnmatchedPaymentsAsync();
        Task<bool> MatchPaymentToInvoiceAsync(Guid paymentId, Guid invoiceId);
    }

    public interface ISettingsService
    {
        Task<string?> GetSettingAsync(string key);
        Task<T?> GetSettingAsync<T>(string key);
        Task SetSettingAsync(string key, string value);
        Task SetSettingAsync<T>(string key, T value);
        Task<IEnumerable<SystemSetting>> GetSettingsByCategoryAsync(string category);
        Task<Dictionary<string, string>> GetAllSettingsAsync();
        Task<bool> DeleteSettingAsync(string key);
    }

    public interface IEmailService
    {
        Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = true);
        Task<bool> SendInvoiceEmailAsync(string to, string invoiceNumber, byte[] pdfAttachment);
        Task<bool> SendPaymentReminderEmailAsync(string to, string invoiceNumber, decimal amount);
        Task<bool> TestEmailConnectionAsync();
    }

    public interface IAuditService
    {
        Task LogActionAsync(string tableName, Guid recordId, string action, string? oldValues, string? newValues, Guid userId);
        Task<IEnumerable<AuditLog>> GetAuditLogsAsync(string? tableName = null, Guid? recordId = null, DateTime? startDate = null, DateTime? endDate = null);
        Task<IEnumerable<AuditLog>> GetAuditLogsByUserAsync(Guid userId, DateTime? startDate = null, DateTime? endDate = null);
        Task<byte[]> ExportAuditLogsAsync(DateTime startDate, DateTime endDate);
    }

    public interface IReportParameterService
    {
        Task<List<ReportParameter>> GetParametersAsync(Guid reportId);
        Task<ReportParameter?> GetParameterAsync(Guid parameterId);
        Task<ReportParameter> SaveParameterAsync(ReportParameter parameter);
        Task DeleteParameterAsync(Guid parameterId);
        Task<Dictionary<string, object>> CollectParametersAsync(Guid reportId, HttpRequest request);
        Task<string> ApplyParametersToSqlAsync(Guid reportId, string sql, Dictionary<string, object> parameters);
    }
}
