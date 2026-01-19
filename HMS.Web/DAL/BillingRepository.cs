/*
 * FILE: BillingRepository.cs
 * PURPOSE: Handles billing, invoices, and payments.
 * COMMUNICATES WITH: DatabaseHelper, Patient/Bills.razor, Teller/TellerDashboard.razor
 */
using HMS.Web.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HMS.Web.DAL
{
    /// <summary>
    /// Repository for managing financial billing and invoices.
    /// OPTIMIZATION: [Atomic Transactions] Uses ADO.NET Transactions to ensure Bill and BillItems are created as a single unit.
    /// OPTIMIZATION: [Runtime Calculations] Derives financial totals using SQL SUM() to maintain SSOT (Single Source of Truth).
    /// </summary>
    public class BillingRepository
    {
        private readonly DatabaseHelper _db;
        private readonly AuditRepository _audit;
        public BillingRepository(DatabaseHelper db, AuditRepository audit)
        {
            _db = db;
            _audit = audit;
        }

        /// <summary>
        /// Asynchronously cancels a bill (Soft Delete) and logs it for auditing.
        /// </summary>
        public async Task CancelBillAsync(int billId, string reason, string userId, string userName)
        {
            try
            {
                const string sql = "UPDATE Bills SET Status = 'Cancelled' WHERE BillId = @Id";
                await _db.ExecuteNonQueryAsync(sql, new[] { new SqlParameter("@Id", billId) });

                await _audit.LogActionAsync(userId, userName, "Bill_Deletion", "Bills", billId.ToString(), $"Reason: {reason}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to cancel bill {billId}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Applies a manual override to a bill amount.
        /// OPTIMIZATION: [Audit Compliance] Mandates a reason and logs the original amount before modification.
        /// </summary>
        public async Task ApplyPriceOverrideAsync(int billId, decimal newAmount, string reason, string userId, string userName)
        {
            try
            {
                // 1. Fetch current amount for audit
                var currentAmountObj = await _db.ExecuteScalarAsync("SELECT TotalAmount FROM Bills WHERE BillId = @Id", new[] { new SqlParameter("@Id", billId) });
                decimal originalAmount = Convert.ToDecimal(currentAmountObj ?? 0);

                // 2. Apply Override
                const string sql = @"UPDATE Bills 
                                     SET TotalAmount = @NewAmount, 
                                         DueAmount = @NewAmount - PaidAmount,
                                         IsOverridden = 1, 
                                         OverrideReason = @Reason, 
                                         OverriddenBy = @UserName, 
                                         OverrideDate = GETDATE(),
                                         OriginalAmount = @Original
                                     WHERE BillId = @Id";

                await _db.ExecuteNonQueryAsync(sql, new[] {
                    new SqlParameter("@Id", billId),
                    new SqlParameter("@NewAmount", newAmount),
                    new SqlParameter("@Reason", reason),
                    new SqlParameter("@UserName", userName),
                    new SqlParameter("@Original", originalAmount)
                });

                await _audit.LogActionAsync(userId, userName, "Price_Override", "Bills", billId.ToString(), $"Original: {originalAmount:C}, New: {newAmount:C}, Reason: {reason}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to apply price override for bill {billId}: {ex.Message}", ex);
            }
        }

        private const string BillColumns = "BillId, PatientId, TotalAmount, PaidAmount, DueAmount, Status, BillDate, ShiftId, CreatedBy, AdmissionId";

        /// <summary>
        /// Retrieves bills for a patient with totals calculated at runtime from BillItems for maximum accuracy.
        /// </summary>
        public async Task<List<Bill>> GetBillsByPatientIdAsync(int patientId)
        {
            try
            {
                // OPTIMIZATION: [Runtime Arithmetic] We calculate the TotalAmount using a subquery instead of trusting stored table values.
                // WHY: Ensured data integrityâ€”if a bill item changes, the total updates automatically without redundant 'Update' calls.
                // HOW: Uses SQL SUM() during retrieval to maintain a single source of truth (SSOT).
                string query = $@"
                    SELECT b.BillId, b.PatientId, 
                           (SELECT ISNULL(SUM(Amount), 0) FROM BillItems WHERE BillId = b.BillId) as CalculatedTotal,
                           b.PaidAmount, b.Status, b.BillDate, b.ShiftId, b.CreatedBy, b.AdmissionId
                    FROM Bills b 
                    WHERE b.PatientId = @Id 
                    ORDER BY b.BillDate DESC";

                var parameters = new[] { new SqlParameter("@Id", patientId) };
                var rawData = await _db.ExecuteQueryAsync(query, MapRawBill, parameters);

                // Domain Logic: Final processing (Sync) using IEnumerable
                return ProcessCalculatedBills(rawData);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving bills for patient {patientId}: {ex.Message}", ex);
            }
        }

        private Bill MapRawBill(SqlDataReader r)
        {
            return new Bill
            {
                BillId = r.GetInt32(0),
                PatientId = r.GetInt32(1),
                TotalAmount = r.GetDecimal(2),
                PaidAmount = r.GetDecimal(3),
                Status = r["Status"]?.ToString() ?? "Pending",
                BillDate = r.GetDateTime(5),
                ShiftId = r.IsDBNull(6) ? null : (int?)r.GetInt32(6),
                CreatedBy = r.IsDBNull(7) ? null : r["CreatedBy"]?.ToString(),
                AdmissionId = r.IsDBNull(8) ? null : (int?)r.GetInt32(8)
            };
        }

        private List<Bill> ProcessCalculatedBills(IEnumerable<Bill> bills)
        {
            var list = bills.ToList();
            foreach (var b in list)
            {
                b.DueAmount = b.TotalAmount - b.PaidAmount;
            }
            return list;
        }



        /// <summary>
        /// Asynchronously creates a comprehensive bill with items within a transaction.
        /// </summary>
        /// <summary>
        /// Asynchronously creates a comprehensive bill with items within a database transaction.
        /// OPTIMIZATION: [Transaction Integrity] Guarantees that we never have a bill without items or vice-versa due to a crash.
        /// HOW IT WORKS: Starts a SqlTransaction, inserts the Bill header, retrieves the ID, inserts items, and commits.
        /// </summary>
        public async Task<int> CreateBillAsync(Bill bill)
        {
            if (bill.PatientId <= 0) throw new ArgumentException("Invalid Patient ID for bill.");
            if (bill.TotalAmount < 0) throw new ArgumentException("Bill Total Amount cannot be negative.");

            using (var connection = _db.GetConnection())
            {
                await connection.OpenAsync();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        const string sql = @"INSERT INTO Bills (PatientId, TotalAmount, PaidAmount, DueAmount, Status, BillDate, ShiftId, CreatedBy, AdmissionId) 
                                           OUTPUT INSERTED.BillId
                                           VALUES (@PatientId, @TotalAmount, @PaidAmount, @DueAmount, @Status, @BillDate, @ShiftId, @CreatedBy, @AdmissionId)";

                        int billId = await _db.ExecuteScalarAsync<int>(sql, new[] {
                            new SqlParameter("@PatientId", bill.PatientId),
                            new SqlParameter("@TotalAmount", bill.TotalAmount),
                            new SqlParameter("@PaidAmount", bill.PaidAmount),
                            new SqlParameter("@DueAmount", bill.TotalAmount), // Default due is total
                            new SqlParameter("@Status", bill.Status ?? "Pending"),
                            new SqlParameter("@BillDate", bill.BillDate == default ? DateTime.Now : bill.BillDate),
                            new SqlParameter("@ShiftId", (object?)bill.ShiftId ?? DBNull.Value),
                            new SqlParameter("@CreatedBy", (object?)bill.CreatedBy ?? DBNull.Value),
                            new SqlParameter("@AdmissionId", (object?)bill.AdmissionId ?? DBNull.Value)
                        }, transaction);

                        if (bill.Items != null)
                        {
                            await ProcessBillItemsAsync(billId, bill.Items, transaction);
                        }

                        transaction.Commit();
                        return billId;
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw new Exception($"Failed to create bill: {ex.Message}", ex);
                    }
                }
            }
        }



        /// <summary>
        /// Retrieves items for a specific bill.
        /// </summary>
        public async Task<List<BillItem>> GetBillItemsAsync(int billId)
        {
            try
            {
                const string sql = "SELECT BillItemId, BillId, Description, Amount, Category FROM BillItems WHERE BillId = @Id";
                return await _db.ExecuteQueryAsync(sql, reader => new BillItem
                {
                    BillItemId = reader.GetInt32(reader.GetOrdinal("BillItemId")),
                    BillId = reader.GetInt32(reader.GetOrdinal("BillId")),
                    Description = reader["Description"]?.ToString() ?? "",
                    Amount = reader.GetDecimal(reader.GetOrdinal("Amount")),
                    Category = reader["Category"]?.ToString() ?? "General"
                }, new[] { new SqlParameter("@Id", billId) });
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving bill items for bill {billId}: {ex.Message}", ex);
            }
        }





        /// <summary>
        /// Mapping logic from SqlDataReader to Bill model.
        /// </summary>
        private async Task ProcessBillItemsAsync(int billId, IEnumerable<BillItem> items, SqlTransaction transaction)
        {
            foreach (var item in items)
            {
                const string itemSql = @"INSERT INTO BillItems (BillId, Description, Amount, Category) 
                                       VALUES (@BillId, @Description, @Amount, @Category)";
                await _db.ExecuteNonQueryAsync(itemSql, new[] {
                    new SqlParameter("@BillId", billId),
                    new SqlParameter("@Description", item.Description ?? "No description"),
                    new SqlParameter("@Amount", item.Amount),
                    new SqlParameter("@Category", item.Category ?? "General")
                }, transaction);
            }
        }


        /// <summary>
        /// Mapping logic from SqlDataReader to Bill model.
        /// </summary>
        private Bill MapBill(SqlDataReader reader)
        {
            return new Bill
            {
                BillId = reader.GetInt32(reader.GetOrdinal("BillId")),
                PatientId = reader.GetInt32(reader.GetOrdinal("PatientId")),
                TotalAmount = reader.GetDecimal(reader.GetOrdinal("TotalAmount")),
                PaidAmount = reader.GetDecimal(reader.GetOrdinal("PaidAmount")),
                DueAmount = reader.GetDecimal(reader.GetOrdinal("DueAmount")),
                Status = reader["Status"]?.ToString() ?? "Pending",
                BillDate = reader.GetDateTime(reader.GetOrdinal("BillDate")),
                ShiftId = reader.IsDBNull(reader.GetOrdinal("ShiftId")) ? null : (int?)reader.GetInt32(reader.GetOrdinal("ShiftId")),
                CreatedBy = reader.IsDBNull(reader.GetOrdinal("CreatedBy")) ? null : reader["CreatedBy"]?.ToString(),
                AdmissionId = reader.IsDBNull(reader.GetOrdinal("AdmissionId")) ? null : (int?)reader.GetInt32(reader.GetOrdinal("AdmissionId"))
            };
        }
    }
}

