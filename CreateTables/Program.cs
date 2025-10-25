using Microsoft.Data.SqlClient;
using System;
using System.IO;
using System.Threading.Tasks;

namespace CreateTables
{
    class Program
    {
        static async Task Main(string[] args)
        {
            string connectionString = "Data Source=192.168.0.61\\CODEPAL19;Initial Catalog=CadenceAccounting;Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=True;Connection Timeout=30";
            
            try
            {
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();
                Console.WriteLine("Connected to database successfully.");

                // Read the SQL script
                string sqlScript = File.ReadAllText("../database-setup-report-designer.sql");
                
                // Split the script into individual statements
                string[] statements = sqlScript.Split(new[] { "GO" }, StringSplitOptions.RemoveEmptyEntries);
                
                foreach (string statement in statements)
                {
                    string trimmedStatement = statement.Trim();
                    if (!string.IsNullOrEmpty(trimmedStatement))
                    {
                        try
                        {
                            using var command = new SqlCommand(trimmedStatement, connection);
                            await command.ExecuteNonQueryAsync();
                            Console.WriteLine("Executed statement successfully.");
                        }
                        catch (SqlException ex) when (ex.Number == 2714) // Object already exists
                        {
                            Console.WriteLine($"Table already exists, skipping: {ex.Message}");
                        }
                        catch (SqlException ex) when (ex.Number == 1913) // Object already exists
                        {
                            Console.WriteLine($"Index already exists, skipping: {ex.Message}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error executing statement: {ex.Message}");
                            Console.WriteLine($"Statement: {trimmedStatement.Substring(0, Math.Min(100, trimmedStatement.Length))}...");
                        }
                    }
                }
                
                Console.WriteLine("Script execution completed.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}
