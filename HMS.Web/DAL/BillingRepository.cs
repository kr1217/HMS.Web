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
        public BillingRepository(DatabaseHelper db) { _db = db; }

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
                return await _db.ExecuteQueryAsync(query, reader =>
                {
                    var bill = new Bill
                    {
                        BillId = reader.GetInt32(0),
                        PatientId = reader.GetInt32(1),
                        TotalAmount = reader.GetDecimal(2),
                        PaidAmount = reader.GetDecimal(3),
                        Status = reader["Status"]?.ToString() ?? "Pending",
                        BillDate = reader.GetDateTime(5),
                        ShiftId = reader.IsDBNull(6) ? null : (int?)reader.GetInt32(6),
                        CreatedBy = reader.IsDBNull(7) ? null : reader["CreatedBy"]?.ToString(),
                        AdmissionId = reader.IsDBNull(8) ? null : (int?)reader.GetInt32(8)
                    };
                    bill.DueAmount = bill.TotalAmount - bill.PaidAmount;
                    return bill;
                }, parameters);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving bills for patient {patientId}: {ex.Message}", ex);
            }
        }

        public List<Bill> GetBillsByPatientId(int patientId)
        {
            if (patientId <= 0) return new List<Bill>();
            string query = $"SELECT {BillColumns} FROM Bills WHERE PatientId = @Id ORDER BY BillDate DESC";
            var parameters = new[] { new SqlParameter("@Id", patientId) };
            return _db.ExecuteQuery(query, MapBill, parameters);
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

                        if (bill.Items != null && bill.Items.Any())
                        {
                            foreach (var item in bill.Items)
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

        public int CreateBill(Bill bill)
        {
            if (bill.PatientId <= 0) throw new ArgumentException("Invalid Patient ID for bill.");
            if (bill.TotalAmount < 0) throw new ArgumentException("Bill Total Amount cannot be negative.");

            using (var connection = _db.GetConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        const string sql = @"INSERT INTO Bills (PatientId, TotalAmount, PaidAmount, DueAmount, Status, BillDate, ShiftId, CreatedBy, AdmissionId) 
                                           OUTPUT INSERTED.BillId
                                           VALUES (@PatientId, @TotalAmount, @PaidAmount, @DueAmount, @Status, @BillDate, @ShiftId, @CreatedBy, @AdmissionId)";

                        int billId = _db.ExecuteScalar<int>(sql, new[] {
                            new SqlParameter("@PatientId", bill.PatientId),
                            new SqlParameter("@TotalAmount", bill.TotalAmount),
                            new SqlParameter("@PaidAmount", bill.PaidAmount),
                            new SqlParameter("@DueAmount", bill.TotalAmount),
                            new SqlParameter("@Status", bill.Status ?? "Pending"),
                            new SqlParameter("@BillDate", bill.BillDate == default ? DateTime.Now : bill.BillDate),
                            new SqlParameter("@ShiftId", (object?)bill.ShiftId ?? DBNull.Value),
                            new SqlParameter("@CreatedBy", (object?)bill.CreatedBy ?? DBNull.Value),
                            new SqlParameter("@AdmissionId", (object?)bill.AdmissionId ?? DBNull.Value)
                        }, transaction);

                        if (bill.Items != null && bill.Items.Any())
                        {
                            foreach (var item in bill.Items)
                            {
                                const string itemSql = @"INSERT INTO BillItems (BillId, Description, Amount, Category) VALUES (@BillId, @Description, @Amount, @Category)";
                                _db.ExecuteNonQuery(itemSql, new[] {
                                    new SqlParameter("@BillId", billId),
                                    new SqlParameter("@Description", item.Description ?? "No description"),
                                    new SqlParameter("@Amount", item.Amount),
                                    new SqlParameter("@Category", item.Category ?? "General")
                                }, transaction);
                            }
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

        public List<BillItem> GetBillItems(int billId)
        {
            const string sql = "SELECT BillItemId, BillId, Description, Amount, Category FROM BillItems WHERE BillId = @Id";
            return _db.ExecuteQuery(sql, reader => new BillItem
            {
                BillItemId = reader.GetInt32(reader.GetOrdinal("BillItemId")),
                BillId = reader.GetInt32(reader.GetOrdinal("BillId")),
                Description = reader["Description"]?.ToString() ?? "",
                Amount = reader.GetDecimal(reader.GetOrdinal("Amount")),
                Category = reader["Category"]?.ToString() ?? "General"
            }, new[] { new SqlParameter("@Id", billId) });
        }

        public void CreateComprehensiveBill(Bill bill)
        {
            bill.BillId = CreateBill(bill);
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

