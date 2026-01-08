/*
 * FILE: OperationRepository.cs
 * PURPOSE: Manages surgical operations and packages.
 * COMMUNICATES WITH: DatabaseHelper, Patient/Operations.razor, Admin/ManageOperationDialog.razor
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
    /// Repository for managing surgical operations and procedures.
    /// OPTIMIZATION: [Workflow State Machine] Tracks operations through complex states (Proposed -> Pending Deposit -> Scheduled -> Completed -> Transferred).
    /// </summary>
    public class OperationRepository
    {
        private readonly DatabaseHelper _db;
        public OperationRepository(DatabaseHelper db) { _db = db; }

        private const string PackageColumns = "PackageId, PackageName, Description, Cost";
        private const string OperationColumns = "po.OperationId, po.PatientId, po.PackageId, po.Status, po.ScheduledDate, po.Notes, po.DoctorId, po.AgreedOperationCost, po.AgreedMedicineCost, po.AgreedEquipmentCost, po.TheaterId, po.DurationMinutes, po.ActualStartTime, po.Urgency, po.ExpectedStayDays, po.IsTransferred, po.RecommendedMedicines, po.RecommendedEquipment";

        /// <summary>
        /// Retrieves all available operation packages.
        /// </summary>
        public async Task<List<OperationPackage>> GetOperationPackagesAsync()
        {
            try
            {
                string query = $"SELECT {PackageColumns} FROM OperationPackages";
                return await _db.ExecuteQueryAsync(query, reader => new OperationPackage
                {
                    PackageId = reader.GetInt32(reader.GetOrdinal("PackageId")),
                    PackageName = reader["PackageName"]?.ToString() ?? "Basic Package",
                    Description = reader["Description"]?.ToString() ?? "",
                    Cost = reader.GetDecimal(reader.GetOrdinal("Cost"))
                });
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving operation packages: {ex.Message}", ex);
            }
        }

        public List<OperationPackage> GetOperationPackages()
        {
            string query = $"SELECT {PackageColumns} FROM OperationPackages";
            return _db.ExecuteQuery(query, reader => new OperationPackage
            {
                PackageId = reader.GetInt32(reader.GetOrdinal("PackageId")),
                PackageName = reader["PackageName"]?.ToString() ?? "Basic Package",
                Description = reader["Description"]?.ToString() ?? "",
                Cost = reader.GetDecimal(reader.GetOrdinal("Cost"))
            });
        }

        /// <summary>
        /// Retrieves all operations for a specific patient.
        /// </summary>
        public async Task<List<PatientOperation>> GetPatientOperationsAsync(int patientId)
        {
            try
            {
                if (patientId <= 0) return new List<PatientOperation>();
                string query = $@"SELECT {OperationColumns}, op.PackageName, p.FullName as PatientName, d.FullName as DoctorName 
                                 FROM PatientOperations po 
                                 LEFT JOIN OperationPackages op ON po.PackageId = op.PackageId 
                                 LEFT JOIN Patients p ON po.PatientId = p.PatientId
                                 LEFT JOIN Doctors d ON po.DoctorId = d.DoctorId
                                 WHERE po.PatientId = @Id";
                var parameters = new[] { new SqlParameter("@Id", patientId) };
                return await _db.ExecuteQueryAsync(query, MapOperation, parameters);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving patient operations: {ex.Message}", ex);
            }
        }

        public List<PatientOperation> GetPatientOperations(int patientId)
        {
            if (patientId <= 0) return new List<PatientOperation>();
            string query = $@"SELECT {OperationColumns}, op.PackageName, p.FullName as PatientName, d.FullName as DoctorName FROM PatientOperations po LEFT JOIN OperationPackages op ON po.PackageId = op.PackageId LEFT JOIN Patients p ON po.PatientId = p.PatientId LEFT JOIN Doctors d ON po.DoctorId = d.DoctorId WHERE po.PatientId = @Id";
            var parameters = new[] { new SqlParameter("@Id", patientId) };
            return _db.ExecuteQuery(query, MapOperation, parameters);
        }

        /// <summary>
        /// Retrieves operations scheduled for a specific doctor.
        /// </summary>
        public async Task<List<PatientOperation>> GetOperationsByDoctorIdAsync(int doctorId)
        {
            try
            {
                if (doctorId <= 0) return new List<PatientOperation>();
                string query = $@"SELECT {OperationColumns}, op.PackageName, p.FullName as PatientName, d.FullName as DoctorName, ot.TheaterName 
                                 FROM PatientOperations po 
                                 LEFT JOIN OperationPackages op ON po.PackageId = op.PackageId 
                                 LEFT JOIN Patients p ON po.PatientId = p.PatientId
                                 LEFT JOIN Doctors d ON po.DoctorId = d.DoctorId
                                 LEFT JOIN OperationTheaters ot ON po.TheaterId = ot.TheaterId
                                 WHERE po.DoctorId = @Id
                                 ORDER BY po.ScheduledDate DESC";
                var parameters = new[] { new SqlParameter("@Id", doctorId) };
                return await _db.ExecuteQueryAsync(query, MapOperation, parameters);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving operations for doctor {doctorId}: {ex.Message}", ex);
            }
        }

        public List<PatientOperation> GetOperationsByDoctorId(int doctorId)
        {
            if (doctorId <= 0) return new List<PatientOperation>();
            string query = $@"SELECT {OperationColumns}, op.PackageName, p.FullName as PatientName, d.FullName as DoctorName, ot.TheaterName FROM PatientOperations po LEFT JOIN OperationPackages op ON po.PackageId = op.PackageId LEFT JOIN Patients p ON po.PatientId = p.PatientId LEFT JOIN Doctors d ON po.DoctorId = d.DoctorId LEFT JOIN OperationTheaters ot ON po.TheaterId = ot.TheaterId WHERE po.DoctorId = @Id ORDER BY po.ScheduledDate DESC";
            var parameters = new[] { new SqlParameter("@Id", doctorId) };
            return _db.ExecuteQuery(query, MapOperation, parameters);
        }

        /// <summary>
        /// Retrieves pending operation requests.
        /// </summary>
        public async Task<List<PatientOperation>> GetPendingOperationsAsync()
        {
            try
            {
                string query = $@"SELECT TOP 50 {OperationColumns}, op.PackageName, p.FullName as PatientName, d.FullName as DoctorName 
                                 FROM PatientOperations po 
                                 LEFT JOIN OperationPackages op ON po.PackageId = op.PackageId 
                                 LEFT JOIN Patients p ON po.PatientId = p.PatientId
                                 LEFT JOIN Doctors d ON po.DoctorId = d.DoctorId
                                 WHERE po.Status IN ('Recommended', 'Pending Deposit', 'Advance Payment Requested')
                                 ORDER BY po.ScheduledDate DESC";
                return await _db.ExecuteQueryAsync(query, MapOperation);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving pending operations: {ex.Message}", ex);
            }
        }

        public List<PatientOperation> GetPendingOperations()
        {
            string query = $@"SELECT TOP 50 {OperationColumns}, op.PackageName, p.FullName as PatientName, d.FullName as DoctorName FROM PatientOperations po LEFT JOIN OperationPackages op ON po.PackageId = op.PackageId LEFT JOIN Patients p ON po.PatientId = p.PatientId LEFT JOIN Doctors d ON po.DoctorId = d.DoctorId WHERE po.Status IN ('Recommended', 'Pending Deposit', 'Advance Payment Requested') ORDER BY po.ScheduledDate DESC";
            return _db.ExecuteQuery(query, MapOperation);
        }

        /// <summary>
        /// Retrieves a paged list of pending operation requests.
        /// </summary>
        public async Task<List<PatientOperation>> GetPendingOperationsPagedAsync(int skip, int take, string orderBy)
        {
            try
            {
                string orderClause = string.IsNullOrEmpty(orderBy) ? "ScheduledDate DESC" : orderBy;
                string query = $@"SELECT {OperationColumns}, op.PackageName, p.FullName as PatientName, d.FullName as DoctorName 
                                 FROM PatientOperations po 
                                 LEFT JOIN OperationPackages op ON po.PackageId = op.PackageId 
                                 LEFT JOIN Patients p ON po.PatientId = p.PatientId
                                 LEFT JOIN Doctors d ON po.DoctorId = d.DoctorId
                                 WHERE po.Status IN ('Recommended', 'Pending Deposit', 'Advance Payment Requested')
                                 ORDER BY {orderClause}
                                 OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";

                var parameters = new[] {
                    new SqlParameter("@Skip", skip),
                    new SqlParameter("@Take", take)
                };
                return await _db.ExecuteQueryAsync(query, MapOperation, parameters);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving paged pending operations: {ex.Message}", ex);
            }
        }

        public List<PatientOperation> GetPendingOperationsPaged(int skip, int take, string orderBy)
        {
            string orderClause = string.IsNullOrEmpty(orderBy) ? "ScheduledDate DESC" : orderBy;
            string query = $@"SELECT {OperationColumns}, op.PackageName, p.FullName as PatientName, d.FullName as DoctorName FROM PatientOperations po LEFT JOIN OperationPackages op ON po.PackageId = op.PackageId LEFT JOIN Patients p ON po.PatientId = p.PatientId LEFT JOIN Doctors d ON po.DoctorId = d.DoctorId WHERE po.Status IN ('Recommended', 'Pending Deposit', 'Advance Payment Requested') ORDER BY {orderClause} OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";
            var parameters = new[] { new SqlParameter("@Skip", skip), new SqlParameter("@Take", take) };
            return _db.ExecuteQuery(query, MapOperation, parameters);
        }

        /// <summary>
        /// Gets the total count of pending operations.
        /// </summary>
        public async Task<int> GetPendingOperationsCountAsync()
        {
            string query = "SELECT COUNT(*) FROM PatientOperations WHERE Status IN ('Recommended', 'Pending Deposit', 'Advance Payment Requested')";
            return await _db.ExecuteScalarAsync<int>(query);
        }

        public int GetPendingOperationsCount()
        {
            string query = "SELECT COUNT(*) FROM PatientOperations WHERE Status IN ('Recommended', 'Pending Deposit', 'Advance Payment Requested')";
            return _db.ExecuteScalar<int>(query);
        }

        /// <summary>
        /// Asynchronously updates the status and financial details of an operation.
        /// </summary>
        /// <summary>
        /// Asynchronously updates the status and financial details of an operation.
        /// OPTIMIZATION: [Dynamic Updates] Uses COALESCE to update only provided fields, allowing partial updates without overwriting existing data with nulls.
        /// </summary>
        public async Task UpdateOperationStatusAndCostsAsync(int opId, string status, decimal? opCost, decimal? medCost, decimal? eqCost, int? theaterId, DateTime? scheduledDate = null, int? duration = null, DateTime? actualStartTime = null, int? doctorId = null)
        {
            try
            {
                if (opId <= 0) throw new ArgumentException("Invalid Operation ID.");
                string query = @"UPDATE PatientOperations 
                                 SET Status = @Status, 
                                      AgreedOperationCost = @OpCost, 
                                      AgreedMedicineCost = @MedCost, 
                                      AgreedEquipmentCost = @EqCost,
                                      TheaterId = @TheaterId,
                                      ScheduledDate = COALESCE(@ScheduledDate, ScheduledDate),
                                      DurationMinutes = COALESCE(@Duration, DurationMinutes),
                                      ActualStartTime = COALESCE(@ActualStart, ActualStartTime),
                                      DoctorId = COALESCE(@DoctorId, DoctorId)
                                 WHERE OperationId = @OpId";
                await _db.ExecuteNonQueryAsync(query, new[] {
                    new SqlParameter("@Status", status),
                    new SqlParameter("@OpCost", (object?)opCost ?? DBNull.Value),
                    new SqlParameter("@MedCost", (object?)medCost ?? DBNull.Value),
                    new SqlParameter("@EqCost", (object?)eqCost ?? DBNull.Value),
                    new SqlParameter("@TheaterId", (object?)theaterId ?? DBNull.Value),
                    new SqlParameter("@ScheduledDate", (object?)scheduledDate ?? DBNull.Value),
                    new SqlParameter("@Duration", (object?)duration ?? DBNull.Value),
                    new SqlParameter("@ActualStart", (object?)actualStartTime ?? DBNull.Value),
                    new SqlParameter("@DoctorId", (object?)doctorId ?? DBNull.Value),
                    new SqlParameter("@OpId", opId)
                });
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to update operation {opId}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Updates the operational state of a procedure.
        /// </summary>
        public void UpdateOperationStatusAndCosts(int opId, string status, decimal? opCost, decimal? medCost, decimal? eqCost, int? theaterId, DateTime? scheduledDate = null, int? duration = null, DateTime? actualStartTime = null, int? doctorId = null)
        {
            if (opId <= 0) throw new ArgumentException("Invalid Operation ID.");
            string query = @"UPDATE PatientOperations SET Status = @Status, AgreedOperationCost = @OpCost, AgreedMedicineCost = @MedCost, AgreedEquipmentCost = @EqCost, TheaterId = @TheaterId, ScheduledDate = COALESCE(@ScheduledDate, ScheduledDate), DurationMinutes = COALESCE(@Duration, DurationMinutes), ActualStartTime = COALESCE(@ActualStart, ActualStartTime), DoctorId = COALESCE(@DoctorId, DoctorId) WHERE OperationId = @OpId";
            _db.ExecuteNonQuery(query, new[] { new SqlParameter("@Status", status), new SqlParameter("@OpCost", (object?)opCost ?? DBNull.Value), new SqlParameter("@MedCost", (object?)medCost ?? DBNull.Value), new SqlParameter("@EqCost", (object?)eqCost ?? DBNull.Value), new SqlParameter("@TheaterId", (object?)theaterId ?? DBNull.Value), new SqlParameter("@ScheduledDate", (object?)scheduledDate ?? DBNull.Value), new SqlParameter("@Duration", (object?)duration ?? DBNull.Value), new SqlParameter("@ActualStart", (object?)actualStartTime ?? DBNull.Value), new SqlParameter("@DoctorId", (object?)doctorId ?? DBNull.Value), new SqlParameter("@OpId", opId) });
        }

        /// <summary>
        /// Asynchronously creates a new patient operation request.
        /// </summary>
        public async Task CreatePatientOperationAsync(PatientOperation op)
        {
            try
            {
                if (op == null || op.PatientId <= 0) throw new ArgumentException("Invalid operation data.");

                string query = @"INSERT INTO PatientOperations (PatientId, PackageId, Status, ScheduledDate, Notes, DoctorId, Urgency, ExpectedStayDays, RecommendedMedicines, RecommendedEquipment) 
                                 VALUES (@PatientId, @PackageId, @Status, @ScheduledDate, @Notes, @DoctorId, @Urgency, @ExpectedStayDays, @RecMeds, @RecEq)";
                var parameters = new[]
                {
                    new SqlParameter("@PatientId", op.PatientId),
                    new SqlParameter("@PackageId", (object?)op.PackageId ?? DBNull.Value),
                    new SqlParameter("@Status", op.Status ?? "Proposed"),
                    new SqlParameter("@ScheduledDate", op.ScheduledDate),
                    new SqlParameter("@Notes", (object?)op.Notes ?? DBNull.Value),
                    new SqlParameter("@DoctorId", op.DoctorId),
                    new SqlParameter("@Urgency", (object?)op.Urgency ?? DBNull.Value),
                    new SqlParameter("@ExpectedStayDays", op.ExpectedStayDays),
                    new SqlParameter("@RecMeds", (object?)op.RecommendedMedicines ?? DBNull.Value),
                    new SqlParameter("@RecEq", (object?)op.RecommendedEquipment ?? DBNull.Value)
                };
                await _db.ExecuteNonQueryAsync(query, parameters);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create operation: {ex.Message}", ex);
            }
        }

        public void CreatePatientOperation(PatientOperation op)
        {
            if (op == null || op.PatientId <= 0) throw new ArgumentException("Invalid operation data.");
            string query = @"INSERT INTO PatientOperations (PatientId, PackageId, Status, ScheduledDate, Notes, DoctorId, Urgency, ExpectedStayDays, RecommendedMedicines, RecommendedEquipment) VALUES (@PatientId, @PackageId, @Status, @ScheduledDate, @Notes, @DoctorId, @Urgency, @ExpectedStayDays, @RecMeds, @RecEq)";
            var parameters = new[] { new SqlParameter("@PatientId", op.PatientId), new SqlParameter("@PackageId", (object?)op.PackageId ?? DBNull.Value), new SqlParameter("@Status", op.Status ?? "Proposed"), new SqlParameter("@ScheduledDate", op.ScheduledDate), new SqlParameter("@Notes", (object?)op.Notes ?? DBNull.Value), new SqlParameter("@DoctorId", op.DoctorId), new SqlParameter("@Urgency", (object?)op.Urgency ?? DBNull.Value), new SqlParameter("@ExpectedStayDays", op.ExpectedStayDays), new SqlParameter("@RecMeds", (object?)op.RecommendedMedicines ?? DBNull.Value), new SqlParameter("@RecEq", (object?)op.RecommendedEquipment ?? DBNull.Value) };
            _db.ExecuteNonQuery(query, parameters);
        }

        /// <summary>
        /// Mapping logic from SqlDataReader to PatientOperation model.
        /// </summary>
        private PatientOperation MapOperation(SqlDataReader reader)
        {
            var op = new PatientOperation
            {
                OperationId = reader.GetInt32(reader.GetOrdinal("OperationId")),
                PatientId = reader.GetInt32(reader.GetOrdinal("PatientId")),
                PackageId = reader.IsDBNull(reader.GetOrdinal("PackageId")) ? null : (int?)reader.GetInt32(reader.GetOrdinal("PackageId")),
                Status = reader["Status"]?.ToString() ?? "Proposed",
                ScheduledDate = reader.GetDateTime(reader.GetOrdinal("ScheduledDate")),
                Notes = reader["Notes"]?.ToString() ?? "",
                DoctorId = reader.IsDBNull(reader.GetOrdinal("DoctorId")) ? 0 : reader.GetInt32(reader.GetOrdinal("DoctorId")),
                AgreedOperationCost = reader.IsDBNull(reader.GetOrdinal("AgreedOperationCost")) ? null : (decimal?)reader.GetDecimal(reader.GetOrdinal("AgreedOperationCost")),
                AgreedMedicineCost = reader.IsDBNull(reader.GetOrdinal("AgreedMedicineCost")) ? null : (decimal?)reader.GetDecimal(reader.GetOrdinal("AgreedMedicineCost")),
                AgreedEquipmentCost = reader.IsDBNull(reader.GetOrdinal("AgreedEquipmentCost")) ? null : (decimal?)reader.GetDecimal(reader.GetOrdinal("AgreedEquipmentCost")),
                TheaterId = reader.IsDBNull(reader.GetOrdinal("TheaterId")) ? null : (int?)reader.GetInt32(reader.GetOrdinal("TheaterId")),
                DurationMinutes = reader.IsDBNull(reader.GetOrdinal("DurationMinutes")) ? 0 : reader.GetInt32(reader.GetOrdinal("DurationMinutes")),
                ActualStartTime = reader.IsDBNull(reader.GetOrdinal("ActualStartTime")) ? null : (DateTime?)reader.GetDateTime(reader.GetOrdinal("ActualStartTime")),
                Urgency = reader.IsDBNull(reader.GetOrdinal("Urgency")) ? null : reader["Urgency"]?.ToString(),
                ExpectedStayDays = reader.IsDBNull(reader.GetOrdinal("ExpectedStayDays")) ? 0 : reader.GetInt32(reader.GetOrdinal("ExpectedStayDays")),
                IsTransferred = reader.GetBoolean(reader.GetOrdinal("IsTransferred")),
                RecommendedMedicines = reader.IsDBNull(reader.GetOrdinal("RecommendedMedicines")) ? null : reader["RecommendedMedicines"]?.ToString(),
                RecommendedEquipment = reader.IsDBNull(reader.GetOrdinal("RecommendedEquipment")) ? null : reader["RecommendedEquipment"]?.ToString()
            };

            for (int i = 0; i < reader.FieldCount; i++)
            {
                string colName = reader.GetName(i);
                if (colName == "PackageName") op.PackageName = reader[i]?.ToString() ?? "";
                else if (colName == "PatientName") op.PatientName = reader[i]?.ToString() ?? "Unknown";
                else if (colName == "DoctorName") op.DoctorName = reader[i]?.ToString() ?? "Unknown";
                else if (colName == "TheaterName") op.TheaterName = reader[i]?.ToString() ?? "";
            }

            return op;
        }

        /// <summary>
        /// Retrieves operations scheduled for a specific theater and date.
        /// </summary>
        public async Task<List<PatientOperation>> GetOperationsByTheaterAndDateAsync(int theaterId, DateTime date)
        {
            try
            {
                string query = $@"SELECT {OperationColumns}, p.FullName as PatientName, opkg.PackageName 
                                 FROM PatientOperations po 
                                 JOIN Patients p ON po.PatientId = p.PatientId
                                 LEFT JOIN OperationPackages opkg ON po.PackageId = opkg.PackageId
                                 WHERE po.TheaterId = @TheaterId 
                                 AND CAST(po.ScheduledDate AS DATE) = CAST(@Date AS DATE)
                                 AND po.Status IN ('Scheduled', 'Running')";
                var parameters = new[] {
                    new SqlParameter("@TheaterId", theaterId),
                    new SqlParameter("@Date", date)
                };
                return await _db.ExecuteQueryAsync(query, MapOperation, parameters);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving operations for theater {theaterId}: {ex.Message}", ex);
            }
        }

        public List<PatientOperation> GetOperationsByTheaterAndDate(int theaterId, DateTime date)
        {
            string query = $@"SELECT {OperationColumns}, p.FullName as PatientName, opkg.PackageName FROM PatientOperations po JOIN Patients p ON po.PatientId = p.PatientId LEFT JOIN OperationPackages opkg ON po.PackageId = opkg.PackageId WHERE po.TheaterId = @TheaterId AND CAST(po.ScheduledDate AS DATE) = CAST(@Date AS DATE) AND po.Status IN ('Scheduled', 'Running')";
            var parameters = new[] { new SqlParameter("@TheaterId", theaterId), new SqlParameter("@Date", date) };
            return _db.ExecuteQuery(query, MapOperation, parameters);
        }

        /// <summary>
        /// Retrieves all currently scheduled or active operations.
        /// </summary>
        public async Task<List<PatientOperation>> GetAllScheduledOperationsAsync()
        {
            try
            {
                string query = $@"SELECT TOP 200 {OperationColumns}, p.FullName as PatientName, ot.TheaterName, opkg.PackageName, d.FullName as DoctorName
                                 FROM PatientOperations po 
                                 JOIN Patients p ON po.PatientId = p.PatientId
                                 LEFT JOIN OperationTheaters ot ON po.TheaterId = ot.TheaterId
                                 LEFT JOIN OperationPackages opkg ON po.PackageId = opkg.PackageId
                                 LEFT JOIN Doctors d ON po.DoctorId = d.DoctorId
                                 WHERE po.Status IN ('Scheduled', 'Running')
                                 ORDER BY po.ScheduledDate DESC";
                return await _db.ExecuteQueryAsync(query, MapOperation);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving scheduled operations: {ex.Message}", ex);
            }
        }

        public List<PatientOperation> GetAllScheduledOperations()
        {
            string query = $@"SELECT TOP 200 {OperationColumns}, p.FullName as PatientName, ot.TheaterName, opkg.PackageName, d.FullName as DoctorName FROM PatientOperations po JOIN Patients p ON po.PatientId = p.PatientId LEFT JOIN OperationTheaters ot ON po.TheaterId = ot.TheaterId LEFT JOIN OperationPackages opkg ON po.PackageId = opkg.PackageId LEFT JOIN Doctors d ON po.DoctorId = d.DoctorId WHERE po.Status IN ('Scheduled', 'Running') ORDER BY po.ScheduledDate DESC";
            return _db.ExecuteQuery(query, MapOperation);
        }

        /// <summary>
        /// Retrieves scheduled operations within a specific date range.
        /// </summary>
        public async Task<List<PatientOperation>> GetScheduledOperationsByRangeAsync(DateTime start, DateTime end)
        {
            try
            {
                string query = $@"SELECT {OperationColumns}, p.FullName as PatientName, ot.TheaterName, opkg.PackageName, d.FullName as DoctorName
                                 FROM PatientOperations po 
                                 JOIN Patients p ON po.PatientId = p.PatientId
                                 LEFT JOIN OperationTheaters ot ON po.TheaterId = ot.TheaterId
                                 LEFT JOIN OperationPackages opkg ON po.PackageId = opkg.PackageId
                                 LEFT JOIN Doctors d ON po.DoctorId = d.DoctorId
                                 WHERE po.ScheduledDate >= @Start AND po.ScheduledDate <= @End
                                 AND po.Status NOT IN ('Cancelled')";

                var parameters = new[] {
                    new SqlParameter("@Start", start),
                    new SqlParameter("@End", end)
                };
                return await _db.ExecuteQueryAsync(query, MapOperation, parameters);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving operations by range: {ex.Message}", ex);
            }
        }

        public List<PatientOperation> GetScheduledOperationsByRange(DateTime start, DateTime end)
        {
            string query = $@"SELECT {OperationColumns}, p.FullName as PatientName, ot.TheaterName, opkg.PackageName, d.FullName as DoctorName FROM PatientOperations po JOIN Patients p ON po.PatientId = p.PatientId LEFT JOIN OperationTheaters ot ON po.TheaterId = ot.TheaterId LEFT JOIN OperationPackages opkg ON po.PackageId = opkg.PackageId LEFT JOIN Doctors d ON po.DoctorId = d.DoctorId WHERE po.ScheduledDate >= @Start AND po.ScheduledDate <= @End AND po.Status NOT IN ('Cancelled')";
            var parameters = new[] { new SqlParameter("@Start", start), new SqlParameter("@End", end) };
            return _db.ExecuteQuery(query, MapOperation, parameters);
        }

        /// <summary>
        /// Retrieves operations with a specific status.
        /// </summary>
        public async Task<List<PatientOperation>> GetOperationsByStatusAsync(string status)
        {
            try
            {
                string query = $@"SELECT TOP 100 {OperationColumns}, p.FullName as PatientName, ot.TheaterName, opkg.PackageName, d.FullName as DoctorName
                                 FROM PatientOperations po 
                                 JOIN Patients p ON po.PatientId = p.PatientId
                                 LEFT JOIN OperationTheaters ot ON po.TheaterId = ot.TheaterId
                                 LEFT JOIN OperationPackages opkg ON po.PackageId = opkg.PackageId
                                 LEFT JOIN Doctors d ON po.DoctorId = d.DoctorId
                                 WHERE po.Status = @Status";
                var parameters = new[] { new SqlParameter("@Status", status) };
                return await _db.ExecuteQueryAsync(query, MapOperation, parameters);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving operations for status {status}: {ex.Message}", ex);
            }
        }

        public List<PatientOperation> GetOperationsByStatus(string status)
        {
            string query = $@"SELECT TOP 100 {OperationColumns}, p.FullName as PatientName, ot.TheaterName, opkg.PackageName, d.FullName as DoctorName FROM PatientOperations po JOIN Patients p ON po.PatientId = p.PatientId LEFT JOIN OperationTheaters ot ON po.TheaterId = ot.TheaterId LEFT JOIN OperationPackages opkg ON po.PackageId = opkg.PackageId LEFT JOIN Doctors d ON po.DoctorId = d.DoctorId WHERE po.Status = @Status";
            var parameters = new[] { new SqlParameter("@Status", status) };
            return _db.ExecuteQuery(query, MapOperation, parameters);
        }

        /// <summary>
        /// Retrieves a paged list of operations with a specific status.
        /// </summary>
        public async Task<List<PatientOperation>> GetOperationsByStatusPagedAsync(string status, int skip, int take, string orderBy)
        {
            try
            {
                string orderClause = string.IsNullOrEmpty(orderBy) ? "ScheduledDate DESC" : orderBy;
                string query = $@"SELECT {OperationColumns}, p.FullName as PatientName, ot.TheaterName, opkg.PackageName, d.FullName as DoctorName
                                 FROM PatientOperations po 
                                 JOIN Patients p ON po.PatientId = p.PatientId
                                 LEFT JOIN OperationTheaters ot ON po.TheaterId = ot.TheaterId
                                 LEFT JOIN OperationPackages opkg ON po.PackageId = opkg.PackageId
                                 LEFT JOIN Doctors d ON po.DoctorId = d.DoctorId
                                 WHERE po.Status = @Status
                                 ORDER BY {orderClause}
                                 OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";

                var parameters = new[] {
                    new SqlParameter("@Status", status),
                    new SqlParameter("@Skip", skip),
                    new SqlParameter("@Take", take)
                };
                return await _db.ExecuteQueryAsync(query, MapOperation, parameters);
            }
            catch { return new List<PatientOperation>(); }
        }

        public List<PatientOperation> GetOperationsByStatusPaged(string status, int skip, int take, string orderBy)
        {
            string orderClause = string.IsNullOrEmpty(orderBy) ? "ScheduledDate DESC" : orderBy;
            string query = $@"SELECT {OperationColumns}, p.FullName as PatientName, ot.TheaterName, opkg.PackageName, d.FullName as DoctorName FROM PatientOperations po JOIN Patients p ON po.PatientId = p.PatientId LEFT JOIN OperationTheaters ot ON po.TheaterId = ot.TheaterId LEFT JOIN OperationPackages opkg ON po.PackageId = opkg.PackageId LEFT JOIN Doctors d ON po.DoctorId = d.DoctorId WHERE po.Status = @Status ORDER BY {orderClause} OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";
            var parameters = new[] { new SqlParameter("@Status", status), new SqlParameter("@Skip", skip), new SqlParameter("@Take", take) };
            return _db.ExecuteQuery(query, MapOperation, parameters);
        }

        /// <summary>
        /// Gets the total count of operations with a specific status.
        /// </summary>
        public async Task<int> GetOperationsByStatusCountAsync(string status)
        {
            string query = "SELECT COUNT(*) FROM PatientOperations WHERE Status = @Status";
            return await _db.ExecuteScalarAsync<int>(query, new[] { new SqlParameter("@Status", status) });
        }

        public int GetOperationsByStatusCount(string status)
        {
            return _db.ExecuteScalar<int>("SELECT COUNT(*) FROM PatientOperations WHERE Status = @Status",
                new[] { new SqlParameter("@Status", status) });
        }

        /// <summary>
        /// Retrieves operations that are completed and ready for patient transfer to a ward/bed.
        /// </summary>
        public async Task<List<PatientOperation>> GetOperationsReadyForTransferAsync()
        {
            try
            {
                string query = $@"SELECT TOP 50 {OperationColumns}, p.FullName as PatientName, ot.TheaterName, opkg.PackageName, d.FullName as DoctorName
                                 FROM PatientOperations po 
                                 JOIN Patients p ON po.PatientId = p.PatientId
                                 LEFT JOIN OperationTheaters ot ON po.TheaterId = ot.TheaterId
                                 LEFT JOIN OperationPackages opkg ON po.PackageId = opkg.PackageId
                                 LEFT JOIN Doctors d ON po.DoctorId = d.DoctorId
                                 WHERE po.Status = 'Completed' AND po.IsTransferred = 0
                                 ORDER BY po.ScheduledDate DESC";
                return await _db.ExecuteQueryAsync(query, MapOperation);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving operations ready for transfer: {ex.Message}", ex);
            }
        }

        public List<PatientOperation> GetOperationsReadyForTransfer()
        {
            string query = $@"SELECT TOP 50 {OperationColumns}, p.FullName as PatientName, ot.TheaterName, opkg.PackageName, d.FullName as DoctorName FROM PatientOperations po JOIN Patients p ON po.PatientId = p.PatientId LEFT JOIN OperationTheaters ot ON po.TheaterId = ot.TheaterId LEFT JOIN OperationPackages opkg ON po.PackageId = opkg.PackageId LEFT JOIN Doctors d ON po.DoctorId = d.DoctorId WHERE po.Status = 'Completed' AND po.IsTransferred = 0 ORDER BY po.ScheduledDate DESC";
            return _db.ExecuteQuery(query, MapOperation);
        }

        /// <summary>
        /// Asynchronously marks an operation as having been transferred to a ward.
        /// </summary>
        public async Task MarkOperationAsTransferredAsync(int operationId)
        {
            string sql = "UPDATE PatientOperations SET IsTransferred = 1 WHERE OperationId = @Id";
            await _db.ExecuteNonQueryAsync(sql, new[] { new SqlParameter("@Id", operationId) });
        }

        public void MarkOperationAsTransferred(int operationId)
        {
            string sql = "UPDATE PatientOperations SET IsTransferred = 1 WHERE OperationId = @Id";
            _db.ExecuteNonQuery(sql, new[] { new SqlParameter("@Id", operationId) });
        }
    }
}

