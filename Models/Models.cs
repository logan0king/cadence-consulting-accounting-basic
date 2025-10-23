using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CadenceAccounting.Models
{
    public class User
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;
        
        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        public string PasswordHash { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;
        
        [StringLength(50)]
        public string Role { get; set; } = "Admin";
        
        public bool IsActive { get; set; } = true;
        
        public bool EmailVerified { get; set; } = false;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? LastLogin { get; set; }
        
        // Navigation properties
        public virtual ICollection<Project> Projects { get; set; } = new List<Project>();
        public virtual ICollection<TimeEntry> TimeEntries { get; set; } = new List<TimeEntry>();
        public virtual ICollection<Expense> Expenses { get; set; } = new List<Expense>();
        public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
        public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    }
    
    public class Company
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        [StringLength(255)]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(50)]
        public string? TaxId { get; set; }
        
        [StringLength(500)]
        public string? Address { get; set; }
        
        [StringLength(100)]
        public string? City { get; set; }
        
        [StringLength(50)]
        public string? State { get; set; }
        
        [StringLength(20)]
        public string? ZipCode { get; set; }
        
        [StringLength(20)]
        public string? Phone { get; set; }
        
        [EmailAddress]
        [StringLength(255)]
        public string? Email { get; set; }
        
        [StringLength(255)]
        public string? Website { get; set; }
        
        [StringLength(500)]
        public string? LogoPath { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual ICollection<Project> Projects { get; set; } = new List<Project>();
    }
    
    public class Project
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        
        public Guid CompanyId { get; set; }
        
        [Required]
        [StringLength(255)]
        public string Name { get; set; } = string.Empty;
        
        public string? Description { get; set; }
        
        [Required]
        [StringLength(50)]
        public string ProjectNumber { get; set; } = string.Empty;
        
        [StringLength(255)]
        public string? ClientName { get; set; }
        
        [EmailAddress]
        [StringLength(255)]
        public string? ClientEmail { get; set; }
        
        [StringLength(20)]
        public string? ClientPhone { get; set; }
        
        [StringLength(500)]
        public string? ClientAddress { get; set; }
        
        [Required]
        [Column(TypeName = "decimal(15,2)")]
        public decimal Budget { get; set; }
        
        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal HourlyRate { get; set; }
        
        [Required]
        public DateTime StartDate { get; set; }
        
        public DateTime? EndDate { get; set; }
        
        [StringLength(50)]
        public string Status { get; set; } = "Active";
        
        [StringLength(50)]
        public string TaxType { get; set; } = "1099";
        
        public byte[]? ContractDocument { get; set; }
        
        public string? ContractText { get; set; }
        
        [Required]
        public Guid CreatedBy { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual Company Company { get; set; } = null!;
        public virtual User CreatedByUser { get; set; } = null!;
        public virtual ICollection<TimeEntry> TimeEntries { get; set; } = new List<TimeEntry>();
        public virtual ICollection<Expense> Expenses { get; set; } = new List<Expense>();
        public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
        
        // Calculated properties
        [NotMapped]
        public decimal TotalTimeAmount => TimeEntries.Where(t => t.IsBillable).Sum(t => t.Amount);
        
        [NotMapped]
        public decimal TotalExpenseAmount => Expenses.Where(e => e.IsBillable).Sum(e => e.Amount);
        
        [NotMapped]
        public decimal TotalAmount => TotalTimeAmount + TotalExpenseAmount;
        
        [NotMapped]
        public decimal RemainingBudget => Budget - TotalAmount;
        
        [NotMapped]
        public decimal BudgetUtilization => Budget > 0 ? (TotalAmount / Budget) * 100 : 0;
    }
    
    public class TimeEntry
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        public Guid ProjectId { get; set; }
        
        [Required]
        public Guid UserId { get; set; }
        
        [Required]
        public DateTime Date { get; set; }
        
        [Required]
        [Column(TypeName = "decimal(5,2)")]
        public decimal Hours { get; set; }
        
        [Required]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;
        
        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Rate { get; set; }
        
        [Required]
        [Column(TypeName = "decimal(15,2)")]
        public decimal Amount { get; set; }
        
        public bool IsBillable { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual Project Project { get; set; } = null!;
        public virtual User User { get; set; } = null!;
    }
    
    public class Expense
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        public Guid ProjectId { get; set; }
        
        [Required]
        public Guid UserId { get; set; }
        
        [Required]
        public DateTime Date { get; set; }
        
        [Required]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;
        
        [Required]
        [Column(TypeName = "decimal(15,2)")]
        public decimal Amount { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Category { get; set; } = string.Empty;
        
        public byte[]? ReceiptImage { get; set; }
        
        [StringLength(255)]
        public string? ReceiptFileName { get; set; }
        
        public bool IsBillable { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual Project Project { get; set; } = null!;
        public virtual User User { get; set; } = null!;
    }
    
    public class Invoice
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        public Guid ProjectId { get; set; }
        
        [Required]
        [StringLength(100)]
        public string InvoiceNumber { get; set; } = string.Empty;
        
        [Required]
        public DateTime InvoiceDate { get; set; }
        
        [Required]
        public DateTime DueDate { get; set; }
        
        [Required]
        [Column(TypeName = "decimal(15,2)")]
        public decimal Subtotal { get; set; }
        
        [Column(TypeName = "decimal(15,2)")]
        public decimal TaxAmount { get; set; } = 0;
        
        [Required]
        [Column(TypeName = "decimal(15,2)")]
        public decimal TotalAmount { get; set; }
        
        [StringLength(50)]
        public string Status { get; set; } = "Draft";
        
        public string? Notes { get; set; }
        
        [Required]
        public Guid CreatedBy { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual Project Project { get; set; } = null!;
        public virtual User CreatedByUser { get; set; } = null!;
        public virtual ICollection<InvoiceItem> InvoiceItems { get; set; } = new List<InvoiceItem>();
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
        
        // Calculated properties
        [NotMapped]
        public decimal TotalPaid => Payments.Sum(p => p.Amount);
        
        [NotMapped]
        public decimal Balance => TotalAmount - TotalPaid;
        
        [NotMapped]
        public bool IsPaid => Balance <= 0;
    }
    
    public class InvoiceItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        public Guid InvoiceId { get; set; }
        
        [Required]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;
        
        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Quantity { get; set; }
        
        [Required]
        [Column(TypeName = "decimal(15,2)")]
        public decimal Rate { get; set; }
        
        [Required]
        [Column(TypeName = "decimal(15,2)")]
        public decimal Amount { get; set; }
        
        [Required]
        [StringLength(50)]
        public string ItemType { get; set; } = string.Empty; // "Time" or "Expense"
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual Invoice Invoice { get; set; } = null!;
    }
    
    public class Payment
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        public Guid InvoiceId { get; set; }
        
        [Required]
        [Column(TypeName = "decimal(15,2)")]
        public decimal Amount { get; set; }
        
        [Required]
        public DateTime PaymentDate { get; set; }
        
        [Required]
        [StringLength(50)]
        public string PaymentMethod { get; set; } = string.Empty;
        
        [StringLength(100)]
        public string? Reference { get; set; }
        
        [StringLength(100)]
        public string? BankTransactionId { get; set; }
        
        [StringLength(500)]
        public string? Notes { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual Invoice Invoice { get; set; } = null!;
    }
    
    public class SystemSetting
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        [StringLength(100)]
        public string Key { get; set; } = string.Empty;
        
        public string? Value { get; set; }
        
        [StringLength(500)]
        public string? Description { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Category { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
    
    public class AuditLog
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        [StringLength(100)]
        public string TableName { get; set; } = string.Empty;
        
        [Required]
        public Guid RecordId { get; set; }
        
        [Required]
        [StringLength(20)]
        public string Action { get; set; } = string.Empty; // "INSERT", "UPDATE", "DELETE"
        
        public string? OldValues { get; set; }
        
        public string? NewValues { get; set; }
        
        [Required]
        public Guid ChangedBy { get; set; }
        
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual User ChangedByUser { get; set; } = null!;
    }
    
    public class ProjectTemplate
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        [StringLength(255)]
        public string Name { get; set; } = string.Empty;
        
        public string? Description { get; set; }
        
        [StringLength(50)]
        public string DefaultTaxType { get; set; } = "1099";
        
        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal DefaultHourlyRate { get; set; }
        
        [Column(TypeName = "decimal(15,2)")]
        public decimal? DefaultBudget { get; set; }
        
        public string? CommonExpenses { get; set; } // JSON array of expense categories
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
