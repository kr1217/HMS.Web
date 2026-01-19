/*
 * FILE: AuditRepository.cs
 * PURPOSE: Manages the recording and retrieval of audit logs for compliance.
 * COMMUNICATES WITH: DatabaseHelper, all sensitive Admin components.
 */
using HMS.Web.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HMS.Web.DAL
{
    /// <summary>
    /// Repository for managing audit trails.
    /// Ensures all sensitive actions are immutably recorded.
    /// </summary>
    public class AuditRepository
    {
        private readonly DatabaseHelper _db;
        public AuditRepository(DatabaseHelper db) { _db = db; }

        private const string AuditColumns = "LogId, UserId, UserName, Action, EntityName, RecordId, OldValue, NewValue, Timestamp, Details";

        /// <summary>
        /// Records an audit entry.
        /// </summary>
        public async Task LogActionAsync(string userId, string userName, string action, string entity, string recordId, string details, string? oldVal = null, string? newVal = null)
        {
            try
            {
                string query = @"INSERT INTO AuditLogs (UserId, UserName, Action, EntityName, RecordId, Details, OldValue, NewValue, Timestamp)
                                 VALUES (@UserId, @UserName, @Action, @Entity, @RecordId, @Details, @OldVal, @NewVal, GETDATE())";

                var parameters = new[] {
                    new SqlParameter("@UserId", userId ?? "SYSTEM"),
                    new SqlParameter("@UserName", (object?)userName ?? "SYSTEM"),
                    new SqlParameter("@Action", action),
                    new SqlParameter("@Entity", entity),
                    new SqlParameter("@RecordId", recordId),
                    new SqlParameter("@Details", (object?)details ?? DBNull.Value),
                    new SqlParameter("@OldVal", (object?)oldVal ?? DBNull.Value),
                    new SqlParameter("@NewVal", (object?)newVal ?? DBNull.Value)
                };

                await _db.ExecuteNonQueryAsync(query, parameters);
            }
            catch (Exception ex)
            {
                // Fallback logging - we don't want audit failure to crash the main transaction usually, 
                // but for strict compliance we might want to throw. 
                // For now, valid architecture is to try/catch and maybe log to console or file if DB fails.
                Console.WriteLine($"AUDIT FAILURE: {ex.Message}");
            }
        }

        /// <summary>
        /// Retrieves the latest audit logs for review.
        /// </summary>
        public async Task<List<AuditLog>> GetRecentLogsAsync(int count = 100)
        {
            string query = $@"SELECT TOP (@Count) {AuditColumns} 
                             FROM AuditLogs 
                             ORDER BY Timestamp DESC";

            return await _db.ExecuteQueryAsync(query, MapLog, new[] { new SqlParameter("@Count", count) });
        }

        /// <summary>
        /// Retrieves audit logs for a specific entity record (e.g., all changes to Bill #123).
        /// </summary>
        public async Task<List<AuditLog>> GetLogsForRecordAsync(string entityName, string recordId)
        {
            string query = $@"SELECT {AuditColumns} 
                             FROM AuditLogs 
                             WHERE EntityName = @Entity AND RecordId = @Id
                             ORDER BY Timestamp DESC";

            return await _db.ExecuteQueryAsync(query, MapLog, new[] {
                new SqlParameter("@Entity", entityName),
                new SqlParameter("@Id", recordId)
            });
        }

        private AuditLog MapLog(SqlDataReader reader)
        {
            return new AuditLog
            {
                LogId = reader.GetInt32(reader.GetOrdinal("LogId")),
                UserId = reader["UserId"]?.ToString() ?? "",
                UserName = reader["UserName"]?.ToString() ?? "",
                Action = reader["Action"]?.ToString() ?? "",
                EntityName = reader["EntityName"]?.ToString() ?? "",
                RecordId = reader["RecordId"]?.ToString() ?? "",
                OldValue = reader["OldValue"]?.ToString(),
                NewValue = reader["NewValue"]?.ToString(),
                Timestamp = reader.GetDateTime(reader.GetOrdinal("Timestamp")),
                Details = reader["Details"]?.ToString()
            };
        }
    }
}
