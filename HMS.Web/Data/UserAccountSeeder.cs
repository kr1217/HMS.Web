using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using HMS.Web.Data;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace HMS.Web.SeedUsers
{
    /// <summary>
    /// Utility class to seed user accounts with proper password hashing
    /// Run this after seeding the database with patient and doctor data
    /// </summary>
    public class UserAccountSeeder
    {
        public static async Task SeedUserAccounts(IServiceProvider serviceProvider)
        {
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var dbHelper = serviceProvider.GetRequiredService<HMS.Web.DAL.DatabaseHelper>();

            Console.WriteLine("========================================");
            Console.WriteLine("SEEDING USER ACCOUNTS");
            Console.WriteLine("========================================");

            // Ensure roles exist
            string[] roles = { "Patient", "Doctor", "Admin" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Seed Patient Accounts (First 10)
            Console.WriteLine("\nCreating Patient Accounts...");
            for (int i = 1; i <= 10; i++)
            {
                var email = $"patient{i}@email.com";

                if (await userManager.FindByEmailAsync(email) == null)
                {
                    var user = new ApplicationUser
                    {
                        UserName = email,
                        Email = email,
                        EmailConfirmed = true
                    };

                    var result = await userManager.CreateAsync(user, "Test@123");

                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(user, "Patient");

                        // Update Patient record with UserId
                        var updateSql = "UPDATE Patients SET UserId = @UserId WHERE PatientId = @PatientId";
                        dbHelper.ExecuteNonQuery(updateSql, new[] {
                            new Microsoft.Data.SqlClient.SqlParameter("@UserId", user.Id),
                            new Microsoft.Data.SqlClient.SqlParameter("@PatientId", i)
                        });

                        Console.WriteLine($"✓ Created: {email} (Password: Test@123)");
                    }
                    else
                    {
                        Console.WriteLine($"✗ Failed to create {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    }
                }
                else
                {
                    Console.WriteLine($"- Skipped: {email} (already exists)");
                }
            }

            // Seed Doctor Accounts (First 10)
            Console.WriteLine("\nCreating Doctor Accounts...");
            var doctorsSql = "SELECT TOP 10 DoctorId, Email FROM Doctors WHERE Email != 'admin@hospital.com' ORDER BY DoctorId";
            var doctorsTable = dbHelper.ExecuteDataTable(doctorsSql);

            if (doctorsTable != null)
            {
                foreach (System.Data.DataRow row in doctorsTable.Rows)
                {
                    var doctorId = (int)row["DoctorId"];
                    var email = row["Email"].ToString();

                    if (await userManager.FindByEmailAsync(email) == null)
                    {
                        var user = new ApplicationUser
                        {
                            UserName = email,
                            Email = email,
                            EmailConfirmed = true
                        };

                        var result = await userManager.CreateAsync(user, "Test@123");

                        if (result.Succeeded)
                        {
                            await userManager.AddToRoleAsync(user, "Doctor");

                            // Update Doctor record with UserId
                            var updateSql = "UPDATE Doctors SET UserId = @UserId WHERE DoctorId = @DoctorId";
                            dbHelper.ExecuteNonQuery(updateSql, new[] {
                                new Microsoft.Data.SqlClient.SqlParameter("@UserId", user.Id),
                                new Microsoft.Data.SqlClient.SqlParameter("@DoctorId", doctorId)
                            });

                            Console.WriteLine($"✓ Created: {email} (Password: Test@123)");
                        }
                        else
                        {
                            Console.WriteLine($"✗ Failed to create {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"- Skipped: {email} (already exists)");
                    }
                }
            }

            Console.WriteLine("\n========================================");
            Console.WriteLine("USER ACCOUNT SEEDING COMPLETE!");
            Console.WriteLine("========================================");
            Console.WriteLine("\nLogin Credentials:");
            Console.WriteLine("- Patients: patient1@email.com through patient10@email.com");
            Console.WriteLine("- Doctors: (see doctor emails in output above)");
            Console.WriteLine("- Password for all: Test@123");
            Console.WriteLine("========================================");
        }
    }
}
