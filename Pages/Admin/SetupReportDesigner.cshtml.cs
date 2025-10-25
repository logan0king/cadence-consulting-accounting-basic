using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System;
using System.IO;

namespace CadenceAccounting.Pages.Admin
{
    public class SetupReportDesignerModel : PageModel
    {
        public bool Success { get; set; } = false;
        public string ErrorMessage { get; set; } = string.Empty;

        public void OnGet()
        {
        }

        public IActionResult OnPost(string action)
        {
            if (action == "create")
            {
                try
                {
                    CreateReportDesignerTables();
                    Success = true;
                }
                catch (Exception ex)
                {
                    ErrorMessage = ex.Message;
                }
            }

            return Page();
        }

        private void CreateReportDesignerTables()
        {
            string connectionString = "Server=(localdb)\\MSSQLLocalDB;Database=CadenceAccounting;Trusted_Connection=true;MultipleActiveResultSets=true";
            
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                
                // Read the SQL script
                string sqlScript = System.IO.File.ReadAllText("database-setup-report-designer.sql");
                
                // Split the script into individual statements
                string[] statements = sqlScript.Split(new[] { "GO" }, StringSplitOptions.RemoveEmptyEntries);
                
                foreach (string statement in statements)
                {
                    if (!string.IsNullOrWhiteSpace(statement))
                    {
                        try
                        {
                            using (var command = new SqlCommand(statement.Trim(), connection))
                            {
                                command.ExecuteNonQuery();
                            }
                        }
                        catch (Exception ex)
                        {
                            // Log the error but continue with other statements
                            Console.WriteLine($"Error executing statement: {ex.Message}");
                        }
                    }
                }
            }
        }
    }
}
