using Microsoft.EntityFrameworkCore;
using CadenceAccounting.Models;

namespace CadenceAccounting.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<TimeEntry> TimeEntries { get; set; }
        public DbSet<Expense> Expenses { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<InvoiceItem> InvoiceItems { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<SystemSetting> SystemSettings { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<ProjectTemplate> ProjectTemplates { get; set; }
        
        // Report Designer entities
        public DbSet<ReportDefinition> ReportDefinitions { get; set; }
        public DbSet<ReportDataSource> ReportDataSources { get; set; }
        public DbSet<ReportField> ReportFields { get; set; }
        public DbSet<ReportRelationship> ReportRelationships { get; set; }
        public DbSet<ReportComponent> ReportComponents { get; set; }
        public DbSet<ReportFilter> ReportFilters { get; set; }
        public DbSet<ReportExecution> ReportExecutions { get; set; }
        public DbSet<ReportParameter> ReportParameters { get; set; }
        public DbSet<ReportParameterOption> ReportParameterOptions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure User entity
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Username).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
            });

            // Configure Company entity
            modelBuilder.Entity<Company>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
            });

            // Configure Project entity
            modelBuilder.Entity<Project>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.ProjectNumber);
                entity.Property(e => e.Budget).HasColumnType("decimal(15,2)");
                entity.Property(e => e.HourlyRate).HasColumnType("decimal(10,2)");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
                
                entity.HasOne(d => d.Company)
                    .WithMany(p => p.Projects)
                    .HasForeignKey(d => d.CompanyId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                entity.HasOne(d => d.CreatedByUser)
                    .WithMany(p => p.Projects)
                    .HasForeignKey(d => d.CreatedBy)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure TimeEntry entity
            modelBuilder.Entity<TimeEntry>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.ProjectId);
                entity.HasIndex(e => e.Date);
                entity.Property(e => e.Hours).HasColumnType("decimal(5,2)");
                entity.Property(e => e.Rate).HasColumnType("decimal(10,2)");
                entity.Property(e => e.Amount).HasColumnType("decimal(15,2)");
                entity.Property(e => e.IsInvoiced).HasDefaultValue(false);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
                
                entity.HasOne(d => d.Project)
                    .WithMany(p => p.TimeEntries)
                    .HasForeignKey(d => d.ProjectId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                entity.HasOne(d => d.User)
                    .WithMany(p => p.TimeEntries)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure Expense entity
            modelBuilder.Entity<Expense>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.ProjectId);
                entity.HasIndex(e => e.Date);
                entity.Property(e => e.Amount).HasColumnType("decimal(15,2)");
                entity.Property(e => e.IsInvoiced).HasDefaultValue(false);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
                
                entity.HasOne(d => d.Project)
                    .WithMany(p => p.Expenses)
                    .HasForeignKey(d => d.ProjectId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                entity.HasOne(d => d.User)
                    .WithMany(p => p.Expenses)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure Invoice entity
            modelBuilder.Entity<Invoice>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.InvoiceNumber).IsUnique();
                entity.HasIndex(e => e.ProjectId);
                entity.HasIndex(e => e.Status);
                entity.Property(e => e.Subtotal).HasColumnType("decimal(15,2)");
                entity.Property(e => e.TaxAmount).HasColumnType("decimal(15,2)");
                entity.Property(e => e.TotalAmount).HasColumnType("decimal(15,2)");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
                
                entity.HasOne(d => d.Project)
                    .WithMany(p => p.Invoices)
                    .HasForeignKey(d => d.ProjectId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                entity.HasOne(d => d.CreatedByUser)
                    .WithMany(p => p.Invoices)
                    .HasForeignKey(d => d.CreatedBy)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure InvoiceItem entity
            modelBuilder.Entity<InvoiceItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.InvoiceId);
                entity.Property(e => e.Quantity).HasColumnType("decimal(10,2)");
                entity.Property(e => e.Rate).HasColumnType("decimal(15,2)");
                entity.Property(e => e.Amount).HasColumnType("decimal(15,2)");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                
                entity.HasOne(d => d.Invoice)
                    .WithMany(p => p.InvoiceItems)
                    .HasForeignKey(d => d.InvoiceId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure Payment entity
            modelBuilder.Entity<Payment>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.InvoiceId);
                entity.Property(e => e.Amount).HasColumnType("decimal(15,2)");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                
                entity.HasOne(d => d.Invoice)
                    .WithMany(p => p.Payments)
                    .HasForeignKey(d => d.InvoiceId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure SystemSetting entity
            modelBuilder.Entity<SystemSetting>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Key).IsUnique();
                entity.HasIndex(e => e.Category);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
            });

            // Configure AuditLog entity
            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.TableName, e.RecordId });
                entity.HasIndex(e => e.ChangedAt);
                entity.Property(e => e.ChangedAt).HasDefaultValueSql("GETUTCDATE()");
                
                entity.HasOne(d => d.ChangedByUser)
                    .WithMany(p => p.AuditLogs)
                    .HasForeignKey(d => d.ChangedBy)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure ProjectTemplate entity
            modelBuilder.Entity<ProjectTemplate>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.DefaultHourlyRate).HasColumnType("decimal(10,2)");
                entity.Property(e => e.DefaultBudget).HasColumnType("decimal(15,2)");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
            });

            // Seed data
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            // Seed default admin user (Password: Admin123!)
            var adminUserId = Guid.Parse("00000000-0000-0000-0000-000000000001"); // Fixed admin user ID
            modelBuilder.Entity<User>().HasData(new User
            {
                Id = adminUserId,
                Username = "admin",
                Email = "admin@cadence-consulting.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                FirstName = "Admin",
                LastName = "User",
                Role = "Admin",
                IsActive = true,
                EmailVerified = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

            // Seed default company
            var companyId = Guid.NewGuid();
            modelBuilder.Entity<Company>().HasData(new Company
            {
                Id = companyId,
                Name = "Cadence Consulting LLC",
                TaxId = "12-3456789",
                Email = "info@cadence-consulting.com",
                Phone = "(312) 555-0123",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

            // Seed default system settings
            modelBuilder.Entity<SystemSetting>().HasData(
                new SystemSetting { Id = Guid.NewGuid(), Key = "InvoiceNumberFormat", Value = "INV-{ProjectNumber}-{Year}-{SequentialNumber}", Description = "Invoice number format template", Category = "Invoice" },
                new SystemSetting { Id = Guid.NewGuid(), Key = "DefaultTaxType", Value = "1099", Description = "Default tax type for new projects", Category = "Tax" },
                new SystemSetting { Id = Guid.NewGuid(), Key = "FederalWithholdingRate", Value = "15.0", Description = "Federal income tax withholding rate", Category = "Tax" },
                new SystemSetting { Id = Guid.NewGuid(), Key = "IllinoisStateTaxRate", Value = "4.95", Description = "Illinois state tax rate", Category = "Tax" },
                new SystemSetting { Id = Guid.NewGuid(), Key = "SocialSecurityRate", Value = "6.2", Description = "Social Security tax rate", Category = "Tax" },
                new SystemSetting { Id = Guid.NewGuid(), Key = "MedicareRate", Value = "1.45", Description = "Medicare tax rate", Category = "Tax" },
                new SystemSetting { Id = Guid.NewGuid(), Key = "DefaultHourlyRate", Value = "75.00", Description = "Default hourly rate for projects", Category = "Project" },
                new SystemSetting { Id = Guid.NewGuid(), Key = "CompanyName", Value = "Cadence Consulting LLC", Description = "Company name for invoices", Category = "Company" },
                new SystemSetting { Id = Guid.NewGuid(), Key = "CompanyEmail", Value = "info@cadence-consulting.com", Description = "Company email for invoices", Category = "Company" },
                new SystemSetting { Id = Guid.NewGuid(), Key = "SMTP_Server", Value = "smtp.office365.com", Description = "SMTP server for email", Category = "Email" },
                new SystemSetting { Id = Guid.NewGuid(), Key = "SMTP_Port", Value = "587", Description = "SMTP port for email", Category = "Email" },
                new SystemSetting { Id = Guid.NewGuid(), Key = "Backup_Frequency", Value = "Daily", Description = "Backup frequency", Category = "Backup" },
                new SystemSetting { Id = Guid.NewGuid(), Key = "Backup_Location", Value = "C:\\Backups\\CadenceAccounting", Description = "Backup location", Category = "Backup" },
                new SystemSetting { Id = Guid.NewGuid(), Key = "Theme", Value = "Journal", Description = "Default theme", Category = "UI" },
                new SystemSetting { Id = Guid.NewGuid(), Key = "SessionTimeout", Value = "30", Description = "Session timeout in minutes", Category = "Security" }
            );

            // Seed default project template
            modelBuilder.Entity<ProjectTemplate>().HasData(new ProjectTemplate
            {
                Id = Guid.NewGuid(),
                Name = "State Contract",
                Description = "Template for state government contracts",
                DefaultTaxType = "1099",
                DefaultHourlyRate = 75.00m,
                DefaultBudget = 50000.00m,
                CommonExpenses = "[\"Travel\", \"Meals\", \"Supplies\", \"Equipment\", \"Software\"]",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

            // Configure Report Designer entities
            ConfigureReportDesignerEntities(modelBuilder);
        }

        private void ConfigureReportDesignerEntities(ModelBuilder modelBuilder)
        {
            // Configure ReportDefinition entity
            modelBuilder.Entity<ReportDefinition>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Title);
                entity.HasIndex(e => e.ReportGroup);
                entity.HasIndex(e => e.IsActive);
                entity.HasIndex(e => e.CreatedBy);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
                
                entity.HasOne(d => d.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(d => d.CreatedBy)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure ReportDataSource entity
            modelBuilder.Entity<ReportDataSource>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.ReportId);
                entity.HasIndex(e => e.SourceName);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                
                entity.HasOne(d => d.Report)
                    .WithMany(p => p.DataSources)
                    .HasForeignKey(d => d.ReportId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure ReportField entity
            modelBuilder.Entity<ReportField>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.ReportDataSourceId);
                entity.HasIndex(e => e.FieldName);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                
                entity.HasOne(d => d.DataSource)
                    .WithMany(p => p.Fields)
                    .HasForeignKey(d => d.ReportDataSourceId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure ReportRelationship entity
            modelBuilder.Entity<ReportRelationship>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.ReportId);
                entity.HasIndex(e => e.FromDataSourceId);
                entity.HasIndex(e => e.ToDataSourceId);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                
                entity.HasOne(d => d.Report)
                    .WithMany(p => p.Relationships)
                    .HasForeignKey(d => d.ReportId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                entity.HasOne(d => d.FromDataSource)
                    .WithMany()
                    .HasForeignKey(d => d.FromDataSourceId)
                    .OnDelete(DeleteBehavior.NoAction);
                    
                entity.HasOne(d => d.ToDataSource)
                    .WithMany()
                    .HasForeignKey(d => d.ToDataSourceId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // Configure ReportComponent entity
            modelBuilder.Entity<ReportComponent>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.ReportId);
                entity.HasIndex(e => e.ComponentType);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                
                entity.HasOne(d => d.Report)
                    .WithMany(p => p.Components)
                    .HasForeignKey(d => d.ReportId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure ReportFilter entity
            modelBuilder.Entity<ReportFilter>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.ReportId);
                entity.HasIndex(e => e.TableName);
                entity.HasIndex(e => e.ColumnName);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
                
                entity.HasOne(d => d.Report)
                    .WithMany(p => p.Filters)
                    .HasForeignKey(d => d.ReportId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure ReportExecution entity
            modelBuilder.Entity<ReportExecution>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.ReportId);
                entity.HasIndex(e => e.ExecutedBy);
                entity.HasIndex(e => e.ExecutionDate);
                entity.Property(e => e.ExecutionDate).HasDefaultValueSql("GETUTCDATE()");
                
                entity.HasOne(d => d.Report)
                    .WithMany(p => p.Executions)
                    .HasForeignKey(d => d.ReportId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                entity.HasOne(d => d.ExecutedByUser)
                    .WithMany()
                    .HasForeignKey(d => d.ExecutedBy)
                    .OnDelete(DeleteBehavior.Restrict);
            });
            
            // Configure ReportParameter entity
            modelBuilder.Entity<ReportParameter>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.ReportId);
                entity.HasIndex(e => e.ParameterName);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
                
                entity.HasOne(d => d.Report)
                    .WithMany()
                    .HasForeignKey(d => d.ReportId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
            
            // Configure ReportParameterOption entity
            modelBuilder.Entity<ReportParameterOption>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.ParameterId);
                
                entity.HasOne(d => d.Parameter)
                    .WithMany(p => p.Options)
                    .HasForeignKey(d => d.ParameterId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
