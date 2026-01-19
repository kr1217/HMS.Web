/*
 * FILE: DoctorLedgerRepository.cs
 * PURPOSE: Manages the immutable financial ledger for doctor compensation.
 * COMMUNICATES WITH: DatabaseHelper, Admin/DoctorLedger.razor
 */
using HMS.Web.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace HMS.Web.DAL
{
    /// <summary>
    /// Repository for the Doctor Ledger, ensuring atomic recording of payable events.
    /// OPTIMIZATION: [Immutable Log] Updates are restricted to Status changes only; amounts and refs are immutable.
    /// </summary>
    public class DoctorLedgerRepository
    {
        private readonly DatabaseHelper _db;
        public DoctorLedgerRepository(DatabaseHelper db) { _db = db; }

        private const string LedgerColumns = "l.LedgerId, l.DoctorId, l.TransactionDate, l.TransactionType, l.ReferenceId, l.Amount, l.Description, l.Status, l.SettledDate, l.IsBlocked, l.BlockReason";

        /// <summary>
        /// Records a new financial event (accrual) in the ledger.
        /// </summary>
        public async Task AddEntryAsync(DoctorLedger entry)
        {
            try
            {
                if (entry == null) throw new ArgumentNullException(nameof(entry));

                string query = @"INSERT INTO DoctorLedger (DoctorId, TransactionDate, TransactionType, ReferenceId, Amount, Description, Status, IsBlocked, BlockReason)
                                 VALUES (@DocId, @Date, @Type, @RefId, @Amount, @Desc, @Status, @Blocked, @Reason)";

                var parameters = new[] {
                    new SqlParameter("@DocId", entry.DoctorId),
                    new SqlParameter("@Date", entry.TransactionDate),
                    new SqlParameter("@Type", entry.TransactionType),
                    new SqlParameter("@RefId", entry.ReferenceId),
                    new SqlParameter("@Amount", entry.Amount),
                    new SqlParameter("@Desc", (object?)entry.Description ?? DBNull.Value),
                    new SqlParameter("@Status", "Pending"),
                    new SqlParameter("@Blocked", entry.IsBlocked),
                    new SqlParameter("@Reason", (object?)entry.BlockReason ?? DBNull.Value)
                };

                await _db.ExecuteNonQueryAsync(query, parameters);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to record ledger entry for Doctor {entry?.DoctorId}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Retrieves pending ledger entries for approval.
        /// </summary>
        public async Task<List<DoctorLedger>> GetPendingEntriesAsync()
        {
            string query = $@"SELECT {LedgerColumns}, d.FullName as DoctorName 
                             FROM DoctorLedger l 
                             JOIN Doctors d ON l.DoctorId = d.DoctorId
                             WHERE l.Status = 'Pending'
                             ORDER BY l.TransactionDate DESC";

            return await _db.ExecuteQueryAsync(query, MapLedger);
        }

        /// <summary>
        /// Gets the ledger history for a specific doctor.
        /// </summary>
        public async Task<List<DoctorLedger>> GetLedgerByDoctorAsync(int doctorId)
        {
            string query = $@"SELECT {LedgerColumns}, d.FullName as DoctorName 
                             FROM DoctorLedger l 
                             JOIN Doctors d ON l.DoctorId = d.DoctorId
                             WHERE l.DoctorId = @Id
                             ORDER BY l.TransactionDate DESC";

            return await _db.ExecuteQueryAsync(query, MapLedger, new[] { new SqlParameter("@Id", doctorId) });
        }

        /// <summary>
        /// Approves a ledger entry, making it listed for payout.
        /// </summary>
        public async Task ApproveEntryAsync(int ledgerId)
        {
            string query = "UPDATE DoctorLedger SET Status = 'Approved' WHERE LedgerId = @Id AND Status = 'Pending'";
            await _db.ExecuteNonQueryAsync(query, new[] { new SqlParameter("@Id", ledgerId) });
        }

        /// <summary>
        /// Marks entries as 'Paid' (Settled).
        /// </summary>
        public async Task SettleEntriesAsync(IEnumerable<int> ledgerIds)
        {
            if (ledgerIds == null || !System.Linq.Enumerable.Any(ledgerIds)) return;

            string ids = string.Join(",", ledgerIds);
            string query = $"UPDATE DoctorLedger SET Status = 'Paid', SettledDate = GETDATE() WHERE LedgerId IN ({ids})";

            await _db.ExecuteNonQueryAsync(query);
        }

        public async Task BlockSettlementAsync(int ledgerId, string reason)
        {
            const string sql = "UPDATE DoctorLedger SET IsBlocked = 1, BlockReason = @Reason WHERE LedgerId = @Id";
            await _db.ExecuteNonQueryAsync(sql, new[] {
                new SqlParameter("@Id", ledgerId),
                new SqlParameter("@Reason", reason)
            });
        }

        public async Task UnblockSettlementAsync(int ledgerId)
        {
            const string sql = "UPDATE DoctorLedger SET IsBlocked = 0, BlockReason = NULL WHERE LedgerId = @Id";
            await _db.ExecuteNonQueryAsync(sql, new[] { new SqlParameter("@Id", ledgerId) });
        }

        private DoctorLedger MapLedger(SqlDataReader reader)
        {
            var l = new DoctorLedger
            {
                LedgerId = reader.GetInt32(reader.GetOrdinal("LedgerId")),
                DoctorId = reader.GetInt32(reader.GetOrdinal("DoctorId")),
                TransactionDate = reader.GetDateTime(reader.GetOrdinal("TransactionDate")),
                TransactionType = reader["TransactionType"]?.ToString() ?? "",
                ReferenceId = reader.GetInt32(reader.GetOrdinal("ReferenceId")),
                Amount = reader.GetDecimal(reader.GetOrdinal("Amount")),
                Description = reader["Description"]?.ToString() ?? "",
                Status = reader["Status"]?.ToString() ?? "",
                SettledDate = reader.IsDBNull(reader.GetOrdinal("SettledDate")) ? null : (DateTime?)reader.GetDateTime(reader.GetOrdinal("SettledDate")),
                IsBlocked = reader.GetBoolean(reader.GetOrdinal("IsBlocked")),
                BlockReason = reader.IsDBNull(reader.GetOrdinal("BlockReason")) ? null : reader["BlockReason"]?.ToString()
            };

            if (reader.HasColumn("DoctorName")) l.DoctorName = reader["DoctorName"]?.ToString() ?? "";

            return l;
        }
    }
}
