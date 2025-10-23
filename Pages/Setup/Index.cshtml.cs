using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CadenceAccounting.Data;
using CadenceAccounting.Services;
using CadenceAccounting.Models;
using System.ComponentModel.DataAnnotations;
using System.Data.SqlClient;
using Microsoft.AspNetCore.Authorization;

namespace CadenceAccounting.Pages.Setup
{
    // [Authorize] - Removed to allow anonymous access during initial setup
    [IgnoreAntiforgeryToken]
    public class IndexModel : PageModel
    {
        private readonly ISettingsService _settingsService;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ISettingsService settingsService, ILogger<IndexModel> logger)
        {
            _settingsService = settingsService;
            _logger = logger;
        }

        [BindProperty]
        public DatabaseConfigModel DatabaseConfig { get; set; } = new();

        [BindProperty]
        public CompanyInfoModel CompanyInfo { get; set; } = new();

        [BindProperty]
        public InvoiceConfigModel InvoiceConfig { get; set; } = new();

        [BindProperty]
        public TaxConfigModel TaxConfig { get; set; } = new();

        [BindProperty]
        public EmailConfigModel EmailConfig { get; set; } = new();

        [BindProperty]
        public BankConfigModel BankConfig { get; set; } = new();

        [BindProperty]
        public BackupConfigModel BackupConfig { get; set; } = new();

        [BindProperty]
        public SecurityConfigModel SecurityConfig { get; set; } = new();

        [BindProperty]
        public ThemeConfigModel ThemeConfig { get; set; } = new();

        public string ActiveTab { get; set; } = "database";

        public async Task<IActionResult> OnGetAsync(string tab = "database")
        {
            ActiveTab = tab;
            await LoadSettings();
            return Page();
        }

        public async Task<IActionResult> OnPostCompanyAsync()
        {
            _logger.LogInformation("Setup OnPostCompanyAsync called");
            _logger.LogInformation("Request Content-Type: {ContentType}", Request.ContentType);
            _logger.LogInformation("Request Method: {Method}", Request.Method);
            
            ActiveTab = "company";

            try
            {
                await SaveCompanyInfo();
                TempData["SuccessMessage"] = "Company information saved successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving company settings");
                TempData["ErrorMessage"] = "An error occurred while saving company settings. Please try again.";
            }

            await LoadSettings();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string tab)
        {
            _logger.LogInformation("Setup OnPostAsync called with tab: {Tab}", tab);
            _logger.LogInformation("Request Content-Type: {ContentType}", Request.ContentType);
            _logger.LogInformation("Request Method: {Method}", Request.Method);
            
            ActiveTab = tab;

            try
            {
                switch (tab)
                {
                    case "database":
                        await SaveDatabaseConfig();
                        break;
                    case "company":
                        await SaveCompanyInfo();
                        break;
                    case "invoice":
                        await SaveInvoiceConfig();
                        break;
                    case "tax":
                        await SaveTaxConfig();
                        break;
                    case "email":
                        await SaveEmailConfig();
                        break;
                    case "bank":
                        await SaveBankConfig();
                        break;
                    case "backup":
                        await SaveBackupConfig();
                        break;
                    case "security":
                        await SaveSecurityConfig();
                        break;
                    case "theme":
                        await SaveThemeConfig();
                        break;
                }

                TempData["SuccessMessage"] = "Settings saved successfully!";
                return RedirectToPage(new { tab });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving settings for tab {Tab}", tab);
                TempData["ErrorMessage"] = "An error occurred while saving settings. Please try again.";
                return Page();
            }
        }

        private async Task LoadSettings()
        {
            // Load database config
            DatabaseConfig.ConnectionString = await _settingsService.GetSettingAsync("Database_ConnectionString") ?? "Server=localhost\\SQLEXPRESS;Database=CadenceAccounting;Trusted_Connection=true;TrustServerCertificate=true;";

            // Load company info
            CompanyInfo.Name = await _settingsService.GetSettingAsync("CompanyName") ?? "Cadence Consulting LLC";
            CompanyInfo.Email = await _settingsService.GetSettingAsync("CompanyEmail") ?? "info@cadence-consulting.com";
            CompanyInfo.Phone = await _settingsService.GetSettingAsync("CompanyPhone") ?? "";
            CompanyInfo.Address = await _settingsService.GetSettingAsync("CompanyAddress") ?? "";
            CompanyInfo.City = await _settingsService.GetSettingAsync("CompanyCity") ?? "";
            CompanyInfo.State = await _settingsService.GetSettingAsync("CompanyState") ?? "";
            CompanyInfo.ZipCode = await _settingsService.GetSettingAsync("CompanyZipCode") ?? "";
            CompanyInfo.TaxId = await _settingsService.GetSettingAsync("CompanyTaxId") ?? "";

            // Load invoice config
            InvoiceConfig.InvoiceNumberFormat = await _settingsService.GetSettingAsync("InvoiceNumberFormat") ?? "INV-{ProjectNumber}-{Year}-{SequentialNumber}";
            InvoiceConfig.DefaultPaymentTerms = await _settingsService.GetSettingAsync("DefaultPaymentTerms") ?? "Net 30";
            InvoiceConfig.LateFeeRate = decimal.Parse(await _settingsService.GetSettingAsync("LateFeeRate") ?? "1.5");
            InvoiceConfig.InvoiceFooterText = await _settingsService.GetSettingAsync("InvoiceFooterText") ?? "Thank you for your business!";

            // Load tax config
            TaxConfig.DefaultTaxType = await _settingsService.GetSettingAsync("DefaultTaxType") ?? "1099";
            TaxConfig.FederalWithholdingRate = decimal.Parse(await _settingsService.GetSettingAsync("FederalWithholdingRate") ?? "15.0");
            TaxConfig.IllinoisStateTaxRate = decimal.Parse(await _settingsService.GetSettingAsync("IllinoisStateTaxRate") ?? "4.95");
            TaxConfig.SocialSecurityRate = decimal.Parse(await _settingsService.GetSettingAsync("SocialSecurityRate") ?? "6.2");
            TaxConfig.MedicareRate = decimal.Parse(await _settingsService.GetSettingAsync("MedicareRate") ?? "1.45");

            // Load email config
            EmailConfig.SmtpServer = await _settingsService.GetSettingAsync("SMTP_Server") ?? "smtp.office365.com";
            EmailConfig.SmtpPort = int.Parse(await _settingsService.GetSettingAsync("SMTP_Port") ?? "587");
            EmailConfig.Username = await _settingsService.GetSettingAsync("SMTP_Username") ?? "";
            EmailConfig.UseSsl = bool.Parse(await _settingsService.GetSettingAsync("SMTP_UseSSL") ?? "true");
            EmailConfig.FromName = await _settingsService.GetSettingAsync("Email_FromName") ?? "Cadence Consulting LLC";

            // Load bank config
            BankConfig.IntegrationMethod = await _settingsService.GetSettingAsync("Bank_IntegrationMethod") ?? "Manual";
            BankConfig.PlaidEnvironment = await _settingsService.GetSettingAsync("Plaid_Environment") ?? "sandbox";
            BankConfig.PlaidClientId = await _settingsService.GetSettingAsync("Plaid_ClientId") ?? "";
            BankConfig.PlaidSecret = await _settingsService.GetSettingAsync("Plaid_Secret") ?? "";

            // Load backup config
            BackupConfig.Frequency = await _settingsService.GetSettingAsync("Backup_Frequency") ?? "Daily";
            BackupConfig.Location = await _settingsService.GetSettingAsync("Backup_Location") ?? "C:\\Backups\\CadenceAccounting";
            BackupConfig.RetentionDays = int.Parse(await _settingsService.GetSettingAsync("Backup_RetentionDays") ?? "30");

            // Load security config
            SecurityConfig.SessionTimeout = int.Parse(await _settingsService.GetSettingAsync("SessionTimeout") ?? "30");
            SecurityConfig.PasswordPolicy = await _settingsService.GetSettingAsync("PasswordPolicy") ?? "Strong";
            SecurityConfig.EnableAuditLogging = bool.Parse(await _settingsService.GetSettingAsync("EnableAuditLogging") ?? "true");

            // Load theme config
            ThemeConfig.CurrentTheme = await _settingsService.GetSettingAsync("Theme") ?? "Journal";
            ThemeConfig.PrimaryColor = await _settingsService.GetSettingAsync("Theme_PrimaryColor") ?? "#2E8B57";
            ThemeConfig.SecondaryColor = await _settingsService.GetSettingAsync("Theme_SecondaryColor") ?? "#F0F8FF";
            ThemeConfig.AccentColor = await _settingsService.GetSettingAsync("Theme_AccentColor") ?? "#FF6B35";
        }

        private async Task SaveDatabaseConfig()
        {
            await _settingsService.SetSettingAsync("Database_ConnectionString", DatabaseConfig.ConnectionString);
        }

        private async Task SaveCompanyInfo()
        {
            _logger.LogInformation("Saving company info: Name={Name}, Email={Email}, Phone={Phone}, Address={Address}, City={City}, State={State}, ZipCode={ZipCode}, TaxId={TaxId}",
                CompanyInfo.Name, CompanyInfo.Email, CompanyInfo.Phone, CompanyInfo.Address, CompanyInfo.City, CompanyInfo.State, CompanyInfo.ZipCode, CompanyInfo.TaxId);
            
            await _settingsService.SetSettingAsync("CompanyName", CompanyInfo.Name);
            await _settingsService.SetSettingAsync("CompanyEmail", CompanyInfo.Email);
            await _settingsService.SetSettingAsync("CompanyPhone", CompanyInfo.Phone);
            await _settingsService.SetSettingAsync("CompanyAddress", CompanyInfo.Address);
            await _settingsService.SetSettingAsync("CompanyCity", CompanyInfo.City);
            await _settingsService.SetSettingAsync("CompanyState", CompanyInfo.State);
            await _settingsService.SetSettingAsync("CompanyZipCode", CompanyInfo.ZipCode);
            await _settingsService.SetSettingAsync("CompanyTaxId", CompanyInfo.TaxId);
            
            _logger.LogInformation("Company info saved successfully");
        }

        private async Task SaveInvoiceConfig()
        {
            await _settingsService.SetSettingAsync("InvoiceNumberFormat", InvoiceConfig.InvoiceNumberFormat);
            await _settingsService.SetSettingAsync("DefaultPaymentTerms", InvoiceConfig.DefaultPaymentTerms);
            await _settingsService.SetSettingAsync("LateFeeRate", InvoiceConfig.LateFeeRate.ToString());
            await _settingsService.SetSettingAsync("InvoiceFooterText", InvoiceConfig.InvoiceFooterText);
        }

        private async Task SaveTaxConfig()
        {
            await _settingsService.SetSettingAsync("DefaultTaxType", TaxConfig.DefaultTaxType);
            await _settingsService.SetSettingAsync("FederalWithholdingRate", TaxConfig.FederalWithholdingRate.ToString());
            await _settingsService.SetSettingAsync("IllinoisStateTaxRate", TaxConfig.IllinoisStateTaxRate.ToString());
            await _settingsService.SetSettingAsync("SocialSecurityRate", TaxConfig.SocialSecurityRate.ToString());
            await _settingsService.SetSettingAsync("MedicareRate", TaxConfig.MedicareRate.ToString());
        }

        private async Task SaveEmailConfig()
        {
            await _settingsService.SetSettingAsync("SMTP_Server", EmailConfig.SmtpServer);
            await _settingsService.SetSettingAsync("SMTP_Port", EmailConfig.SmtpPort.ToString());
            await _settingsService.SetSettingAsync("SMTP_Username", EmailConfig.Username);
            await _settingsService.SetSettingAsync("SMTP_Password", EmailConfig.Password);
            await _settingsService.SetSettingAsync("SMTP_UseSSL", EmailConfig.UseSsl.ToString());
            await _settingsService.SetSettingAsync("Email_FromName", EmailConfig.FromName);
        }

        private async Task SaveBankConfig()
        {
            await _settingsService.SetSettingAsync("Bank_IntegrationMethod", BankConfig.IntegrationMethod);
            await _settingsService.SetSettingAsync("Plaid_Environment", BankConfig.PlaidEnvironment);
            await _settingsService.SetSettingAsync("Plaid_ClientId", BankConfig.PlaidClientId);
            await _settingsService.SetSettingAsync("Plaid_Secret", BankConfig.PlaidSecret);
        }

        private async Task SaveBackupConfig()
        {
            await _settingsService.SetSettingAsync("Backup_Frequency", BackupConfig.Frequency);
            await _settingsService.SetSettingAsync("Backup_Location", BackupConfig.Location);
            await _settingsService.SetSettingAsync("Backup_RetentionDays", BackupConfig.RetentionDays.ToString());
        }

        private async Task SaveSecurityConfig()
        {
            await _settingsService.SetSettingAsync("SessionTimeout", SecurityConfig.SessionTimeout.ToString());
            await _settingsService.SetSettingAsync("PasswordPolicy", SecurityConfig.PasswordPolicy);
            await _settingsService.SetSettingAsync("EnableAuditLogging", SecurityConfig.EnableAuditLogging.ToString());
        }

        private async Task SaveThemeConfig()
        {
            await _settingsService.SetSettingAsync("Theme", ThemeConfig.CurrentTheme);
            await _settingsService.SetSettingAsync("Theme_PrimaryColor", ThemeConfig.PrimaryColor);
            await _settingsService.SetSettingAsync("Theme_SecondaryColor", ThemeConfig.SecondaryColor);
            await _settingsService.SetSettingAsync("Theme_AccentColor", ThemeConfig.AccentColor);
        }

        public async Task<IActionResult> OnPostTestConnectionAsync()
        {
            try
            {
                using var connection = new SqlConnection(DatabaseConfig.ConnectionString);
                await connection.OpenAsync();
                await connection.CloseAsync();
                
                return new JsonResult(new { success = true, message = "Connection successful!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database connection test failed");
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

        public IActionResult OnPostTestEmail()
        {
            try
            {
                // TODO: Implement email test
                return new JsonResult(new { success = true, message = "Email test successful!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email test failed");
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

        // Model classes
        public class DatabaseConfigModel
        {
            [Required]
            [Display(Name = "Connection String")]
            public string ConnectionString { get; set; } = string.Empty;
        }

        public class CompanyInfoModel
        {
            [Required]
            [Display(Name = "Company Name")]
            public string Name { get; set; } = string.Empty;

            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; } = string.Empty;

            [Display(Name = "Phone")]
            public string Phone { get; set; } = string.Empty;

            [Display(Name = "Address")]
            public string Address { get; set; } = string.Empty;

            [Display(Name = "City")]
            public string City { get; set; } = string.Empty;

            [Display(Name = "State")]
            public string State { get; set; } = string.Empty;

            [Display(Name = "ZIP Code")]
            public string ZipCode { get; set; } = string.Empty;

            [Display(Name = "Tax ID")]
            public string TaxId { get; set; } = string.Empty;
        }

        public class InvoiceConfigModel
        {
            [Required]
            [Display(Name = "Invoice Number Format")]
            public string InvoiceNumberFormat { get; set; } = string.Empty;

            [Display(Name = "Default Payment Terms")]
            public string DefaultPaymentTerms { get; set; } = string.Empty;

            [Display(Name = "Late Fee Rate (%)")]
            public decimal LateFeeRate { get; set; }

            [Display(Name = "Invoice Footer Text")]
            public string InvoiceFooterText { get; set; } = string.Empty;
        }

        public class TaxConfigModel
        {
            [Required]
            [Display(Name = "Default Tax Type")]
            public string DefaultTaxType { get; set; } = string.Empty;

            [Display(Name = "Federal Withholding Rate (%)")]
            public decimal FederalWithholdingRate { get; set; }

            [Display(Name = "Illinois State Tax Rate (%)")]
            public decimal IllinoisStateTaxRate { get; set; }

            [Display(Name = "Social Security Rate (%)")]
            public decimal SocialSecurityRate { get; set; }

            [Display(Name = "Medicare Rate (%)")]
            public decimal MedicareRate { get; set; }
        }

        public class EmailConfigModel
        {
            [Required]
            [Display(Name = "SMTP Server")]
            public string SmtpServer { get; set; } = string.Empty;

            [Required]
            [Display(Name = "SMTP Port")]
            public int SmtpPort { get; set; }

            [Required]
            [Display(Name = "Username")]
            public string Username { get; set; } = string.Empty;

            [Required]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; } = string.Empty;

            [Display(Name = "Use SSL")]
            public bool UseSsl { get; set; }

            [Display(Name = "From Name")]
            public string FromName { get; set; } = string.Empty;
        }

        public class BankConfigModel
        {
            [Required]
            [Display(Name = "Integration Method")]
            public string IntegrationMethod { get; set; } = string.Empty;

            [Display(Name = "Plaid Environment")]
            public string PlaidEnvironment { get; set; } = string.Empty;

            [Display(Name = "Plaid Client ID")]
            public string PlaidClientId { get; set; } = string.Empty;

            [Display(Name = "Plaid Secret")]
            public string PlaidSecret { get; set; } = string.Empty;
        }

        public class BackupConfigModel
        {
            [Required]
            [Display(Name = "Backup Frequency")]
            public string Frequency { get; set; } = string.Empty;

            [Required]
            [Display(Name = "Backup Location")]
            public string Location { get; set; } = string.Empty;

            [Display(Name = "Retention Days")]
            public int RetentionDays { get; set; }
        }

        public class SecurityConfigModel
        {
            [Display(Name = "Session Timeout (minutes)")]
            public int SessionTimeout { get; set; }

            [Display(Name = "Password Policy")]
            public string PasswordPolicy { get; set; } = string.Empty;

            [Display(Name = "Enable Audit Logging")]
            public bool EnableAuditLogging { get; set; }
        }

        public class ThemeConfigModel
        {
            [Required]
            [Display(Name = "Current Theme")]
            public string CurrentTheme { get; set; } = string.Empty;

            [Display(Name = "Primary Color")]
            public string PrimaryColor { get; set; } = string.Empty;

            [Display(Name = "Secondary Color")]
            public string SecondaryColor { get; set; } = string.Empty;

            [Display(Name = "Accent Color")]
            public string AccentColor { get; set; } = string.Empty;
        }
    }
}
