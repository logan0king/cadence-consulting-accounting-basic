using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CadenceAccounting.Data;
using System.Data.SqlClient;

namespace CadenceAccounting.Pages
{
    public class TestConnectionModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TestConnectionModel> _logger;

        public TestConnectionModel(ApplicationDbContext context, IConfiguration configuration, ILogger<TestConnectionModel> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        public bool IsConnected { get; set; }
        public string? ErrorMessage { get; set; }
        public string ConnectionString { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync(bool test = false)
        {
            ConnectionString = _configuration.GetConnectionString("DefaultConnection") ?? "Not configured";

            if (test)
            {
                await TestConnection();
            }

            return Page();
        }

        private async Task TestConnection()
        {
            try
            {
                _logger.LogInformation("Testing database connection...");
                
                // Test direct SQL connection
                using var connection = new SqlConnection(ConnectionString);
                await connection.OpenAsync();
                await connection.CloseAsync();
                
                // Test Entity Framework connection
                await _context.Database.OpenConnectionAsync();
                await _context.Database.CloseConnectionAsync();
                
                IsConnected = true;
                _logger.LogInformation("Database connection test successful");
            }
            catch (Exception ex)
            {
                IsConnected = false;
                ErrorMessage = ex.Message;
                _logger.LogError(ex, "Database connection test failed");
            }
        }
    }
}
