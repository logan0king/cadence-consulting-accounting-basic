using Microsoft.Data.SqlClient;
using System;
using System.IO;

namespace SqlExecutor
{
    class Program
    {
        static void Main(string[] args)
        {
            string connectionString = "Data Source=192.168.0.61\\CODEPAL19;Initial Catalog=CadenceAccounting;Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=True;Connection Timeout=30";
            
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    Console.WriteLine("Connected to database successfully.");
                    
                    // Check all report designer tables
                    string query = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME LIKE 'Report%' ORDER BY TABLE_NAME";
                    
                    using (var command = new SqlCommand(query, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            Console.WriteLine("Report Designer Tables:");
                            while (reader.Read())
                            {
                                Console.WriteLine($"- {reader["TABLE_NAME"]}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}