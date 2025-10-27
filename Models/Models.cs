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
        
        public bool IsInvoiced { get; set; } = false;
        
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
        
        public bool IsInvoiced { get; set; } = false;
        
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
    
    // Report Designer Models
    
    public class ReportDefinition
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string? Description { get; set; }
        
        [StringLength(100)]
        public string ReportGroup { get; set; } = "General";
        
        [StringLength(100)]
        public string? PrintDateUDF { get; set; }
        
        public string? AllowedUserGroups { get; set; } // JSON array
        
        public bool IsActive { get; set; } = true;
        
        public bool IsTemplate { get; set; } = false;
        
        public string? CanvasData { get; set; } // JSON: { width, height, zoom }
        
        [Required]
        public Guid CreatedBy { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual User CreatedByUser { get; set; } = null!;
        public virtual ICollection<ReportDataSource> DataSources { get; set; } = new List<ReportDataSource>();
        public virtual ICollection<ReportRelationship> Relationships { get; set; } = new List<ReportRelationship>();
        public virtual ICollection<ReportComponent> Components { get; set; } = new List<ReportComponent>();
        public virtual ICollection<ReportFilter> Filters { get; set; } = new List<ReportFilter>();
        public virtual ICollection<ReportExecution> Executions { get; set; } = new List<ReportExecution>();
    }
    
    public class ReportDataSource
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        public Guid ReportId { get; set; }
        
        [Required]
        [StringLength(100)]
        public string SourceName { get; set; } = string.Empty; // Table/view name
        
        [Required]
        [StringLength(20)]
        public string SourceType { get; set; } = "table"; // 'table' or 'view'
        
        [StringLength(50)]
        public string? Alias { get; set; }
        
        public int PositionX { get; set; } = 100;
        
        public int PositionY { get; set; } = 100;
        
        public bool IsSelected { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual ReportDefinition Report { get; set; } = null!;
        public virtual ICollection<ReportField> Fields { get; set; } = new List<ReportField>();
    }
    
    public class ReportField
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        public Guid ReportDataSourceId { get; set; }
        
        [Required]
        [StringLength(100)]
        public string FieldName { get; set; } = string.Empty;
        
        [StringLength(100)]
        public string? DisplayName { get; set; }
        
        public bool IsSelected { get; set; } = false;
        
        public int SortOrder { get; set; } = 0;
        
        [StringLength(50)]
        public string? DataType { get; set; }
        
        [StringLength(100)]
        public string? FormatString { get; set; }
        
        public bool IsGroupBy { get; set; } = false;
        
        public bool IsSortBy { get; set; } = false;
        
        [StringLength(10)]
        public string SortDirection { get; set; } = "ASC";
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual ReportDataSource DataSource { get; set; } = null!;
    }
    
    public class ReportRelationship
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        public Guid ReportId { get; set; }
        
        [Required]
        public Guid FromDataSourceId { get; set; }
        
        [Required]
        public Guid ToDataSourceId { get; set; }
        
        [Required]
        [StringLength(100)]
        public string FromField { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string ToField { get; set; } = string.Empty;
        
        [StringLength(20)]
        public string JoinType { get; set; } = "INNER";
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual ReportDefinition Report { get; set; } = null!;
        public virtual ReportDataSource FromDataSource { get; set; } = null!;
        public virtual ReportDataSource ToDataSource { get; set; } = null!;
    }
    
    public class ReportComponent
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        public Guid ReportId { get; set; }
        
        [Required]
        [StringLength(50)]
        public string ComponentType { get; set; } = string.Empty; // 'textbox', 'table', 'image', etc.
        
        public int PositionX { get; set; } = 0;
        
        public int PositionY { get; set; } = 0;
        
        public int Width { get; set; } = 100;
        
        public int Height { get; set; } = 50;
        
        public string? Properties { get; set; } // JSON properties
        
        public string? DataBinding { get; set; } // JSON data binding info
        
        public int ZIndex { get; set; } = 0;
        
        public bool IsVisible { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual ReportDefinition Report { get; set; } = null!;
    }
    
    public class ReportFilter
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        public Guid ReportId { get; set; }
        
        [Required]
        [StringLength(100)]
        public string TableName { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string ColumnName { get; set; } = string.Empty;
        
        public FilterOperator Operator { get; set; }
        
        [StringLength(500)]
        public string? FilterValue { get; set; }
        
        public LogicOperator? LogicOperator { get; set; }
        
        public int SortOrder { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual ReportDefinition Report { get; set; } = null!;
    }
    
    public enum FilterOperator
    {
        Equals,
        NotEquals,
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual,
        Contains,
        StartsWith,
        EndsWith,
        Between,
        In,
        NotIn,
        IsNull,
        IsNotNull
    }
    
    public enum LogicOperator
    {
        None,
        And,
        Or
    }
    
    public class ReportExecution
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        public Guid ReportId { get; set; }
        
        [Required]
        public Guid ExecutedBy { get; set; }
        
        public DateTime ExecutionDate { get; set; } = DateTime.UtcNow;
        
        public string? Parameters { get; set; } // JSON of parameters used
        
        public int? ExecutionTimeMs { get; set; }
        
        public int? RecordCount { get; set; }
        
        [StringLength(20)]
        public string Status { get; set; } = "Success";
        
        public string? ErrorMessage { get; set; }
        
        // Navigation properties
        public virtual ReportDefinition Report { get; set; } = null!;
        public virtual User ExecutedByUser { get; set; } = null!;
    }
    
    // Report Parameter Models
    public class ReportParameter
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        public Guid ReportId { get; set; }
        
        [Required]
        [StringLength(100)]
        public string ParameterName { get; set; } = string.Empty;
        
        [Required]
        public ParameterType ParameterType { get; set; }
        
        [StringLength(500)]
        public string? Prompt { get; set; }
        
        public string? DefaultValue { get; set; }
        
        public bool Required { get; set; }
        
        // For lookup parameters
        public string? LookupSource { get; set; }
        
        [StringLength(100)]
        public string? DisplayField { get; set; }
        
        [StringLength(100)]
        public string? ValueField { get; set; }
        
        public int SortOrder { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual ReportDefinition Report { get; set; } = null!;
        public virtual ICollection<ReportParameterOption> Options { get; set; } = new List<ReportParameterOption>();
    }
    
    public enum ParameterType
    {
        Text,
        Integer,
        Decimal,
        DateTime,
        Boolean,
        DateRange,
        LookupSingle,
        LookupMulti
    }
    
    public class ReportParameterOption
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        public Guid ParameterId { get; set; }
        
        [StringLength(200)]
        public string? DisplayValue { get; set; }
        
        [StringLength(200)]
        public string? ActualValue { get; set; }
        
        public int SortOrder { get; set; }
        
        // Navigation properties
        public virtual ReportParameter Parameter { get; set; } = null!;
    }
}
