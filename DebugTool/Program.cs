using System;
using Microsoft.Data.SqlClient;

namespace DebugTool
{
    class Program
    {
        static void Main(string[] args)
        {
            string connectionString = "Server=LAPTOP-CPSNA65M;Database=HospitalManagement;Integrated Security=True;TrustServerCertificate=True;";
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT TOP 1 PrescriptionId, Medications FROM Prescriptions ORDER BY PrescriptionId DESC";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            Console.WriteLine($"ID: {reader["PrescriptionId"]}");
                            Console.WriteLine($"JSON: {reader["Medications"]}");
                        }
                        else
                        {
                            Console.WriteLine("No prescriptions found.");
                        }
                    }
                }
            }
        }
    }
}
