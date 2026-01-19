using HMS.Web.DAL;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace HMS.Web.Services
{
    public class MigrationService : IHostedService
    {
        private readonly DatabaseHelper _db;
        private readonly string _sqlFilePath;

        public MigrationService(DatabaseHelper db)
        {
            _db = db;
            _sqlFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Phase4_SchemaUpgrade.sql");
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                // In a real app, we would have a better migration tracker.
                // Here we rely on the implementation of the SQL script itself (IF NOT EXISTS checks) to be idempotent.

                // We need to find the correct path. AppDomain BaseDirectory might be bin/debug...
                // The file was saved to HMS.Web/Data/Phase4_SchemaUpgrade.sql
                // When running, we need to make sure we find it.

                // For development environment 'dotnet run', the Content Root is the project folder.
                string contentRoot = Directory.GetCurrentDirectory();
                string scriptPath = Path.Combine(contentRoot, "Data", "Phase4_SchemaUpgrade.sql");

                if (File.Exists(scriptPath))
                {
                    string script = await File.ReadAllTextAsync(scriptPath, cancellationToken);
                    if (!string.IsNullOrWhiteSpace(script))
                    {
                        // Split script by GO? Or just run it if T-SQL supports it (ADO.NET usually wants single batches or no GO)
                        // Our script doesn't use GO, so we can run it as one block or better:
                        // The script contains IF blocks which are valid T-SQL.

                        await _db.ExecuteNonQueryAsync(script);
                        Console.WriteLine("✅ Phase 4.2 Schema Upgrade applied successfully.");
                    }
                }

                // Phase 4.3 Audit Migration
                string auditScriptPath = Path.Combine(contentRoot, "Data", "Phase4_3_AuditSchema.sql");
                if (File.Exists(auditScriptPath))
                {
                    string auditScript = await File.ReadAllTextAsync(auditScriptPath, cancellationToken);
                    if (!string.IsNullOrWhiteSpace(auditScript))
                    {
                        await _db.ExecuteNonQueryAsync(auditScript);
                        Console.WriteLine("✅ Phase 4.3 Audit Schema applied successfully.");
                    }
                }
                else
                {
                    Console.WriteLine($"⚠️ Migration Script not found at: {scriptPath}");
                }

                // Patient Loss Intelligence Schema
                string lossScriptPath = Path.Combine(contentRoot, "Data", "PatientLoss_Schema.sql");
                if (File.Exists(lossScriptPath))
                {
                    string lossScript = await File.ReadAllTextAsync(lossScriptPath, cancellationToken);
                    if (!string.IsNullOrWhiteSpace(lossScript))
                    {
                        await _db.ExecuteNonQueryAsync(lossScript);
                        Console.WriteLine("✅ Patient Loss Schema applied successfully.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Migration Failed: {ex.Message}");
                // We don't throw, to allow app to start even if migration fails (e.g. transient DB issue), 
                // but in prod we might want to stop.
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
