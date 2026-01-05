using System;
using Microsoft.Data.SqlClient;

namespace DebugTool
{
    class Program
    {
        static void Main(string[] args)
        {
            string connectionString = "Server=LAPTOP-CPSNA65M;Database=HospitalManagement;Integrated Security=True;TrustServerCertificate=True;";
            string sqlFilePath = @"c:\Users\kills\.gemini\antigravity\scratch\HMS.Web\HMS.Web\setup_payments_table.sql";

            try
            {
                string script = System.IO.File.ReadAllText(sqlFilePath);
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(script, conn))
                    {
                        cmd.ExecuteNonQuery();
                        Console.WriteLine("SQL script executed successfully.");
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
