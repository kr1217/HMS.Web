/*
 * FILE: DoctorShiftRepository.cs
 * PURPOSE: Manages doctor work schedules.
 * COMMUNICATES WITH: DatabaseHelper, Doctor/Dashboard.razor
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
    /// Repository for managing doctor weekly shifts and availability.
    /// OPTIMIZATION: [Bitwise vs Table] Uses a relational table approach (1:N) for shifts to allow complex scheduling (e.g., split shifts) which bitwise flags can't handle easily.
    /// </summary>
    public class DoctorShiftRepository
    {
        private readonly DatabaseHelper _db;
        public DoctorShiftRepository(DatabaseHelper db) { _db = db; }

        private const string ShiftColumns = "ShiftId, DoctorId, DayOfWeek, StartTime, EndTime, ShiftType, IsActive, Notes, CreatedAt";

        /// <summary>
        /// Retrieves all active shifts for a specific doctor.
        /// </summary>
        public async Task<List<DoctorShift>> GetShiftsByDoctorIdAsync(int doctorId)
        {
            try
            {
                if (doctorId <= 0) return new List<DoctorShift>();
                string query = $@"SELECT {ShiftColumns} FROM DoctorShifts 
                                 WHERE DoctorId = @Id AND IsActive = 1 
                                 ORDER BY CASE DayOfWeek 
                                    WHEN 'Monday' THEN 1 WHEN 'Tuesday' THEN 2 WHEN 'Wednesday' THEN 3 
                                    WHEN 'Thursday' THEN 4 WHEN 'Friday' THEN 5 WHEN 'Saturday' THEN 6 
                                    WHEN 'Sunday' THEN 7 END, StartTime";
                var parameters = new[] { new SqlParameter("@Id", doctorId) };
                return await _db.ExecuteQueryAsync(query, MapShift, parameters);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving shifts for doctor {doctorId}: {ex.Message}", ex);
            }
        }

        public List<DoctorShift> GetShiftsByDoctorId(int doctorId)
        {
            if (doctorId <= 0) return new List<DoctorShift>();
            string query = $@"SELECT {ShiftColumns} FROM DoctorShifts WHERE DoctorId = @Id AND IsActive = 1 ORDER BY CASE DayOfWeek WHEN 'Monday' THEN 1 WHEN 'Tuesday' THEN 2 WHEN 'Wednesday' THEN 3 WHEN 'Thursday' THEN 4 WHEN 'Friday' THEN 5 WHEN 'Saturday' THEN 6 WHEN 'Sunday' THEN 7 END, StartTime";
            var parameters = new[] { new SqlParameter("@Id", doctorId) };
            return _db.ExecuteQuery(query, MapShift, parameters);
        }

        /// <summary>
        /// Retrieves a single shift record by its ID.
        /// </summary>
        public async Task<DoctorShift?> GetShiftByIdAsync(int shiftId)
        {
            try
            {
                if (shiftId <= 0) return null;
                string query = $"SELECT {ShiftColumns} FROM DoctorShifts WHERE ShiftId = @Id";
                var parameters = new[] { new SqlParameter("@Id", shiftId) };
                var list = await _db.ExecuteQueryAsync(query, MapShift, parameters);
                return list.FirstOrDefault();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving shift {shiftId}: {ex.Message}", ex);
            }
        }

        public DoctorShift? GetShiftById(int shiftId)
        {
            if (shiftId <= 0) return null;
            string query = $"SELECT {ShiftColumns} FROM DoctorShifts WHERE ShiftId = @Id";
            var parameters = new[] { new SqlParameter("@Id", shiftId) };
            return _db.ExecuteQuery(query, MapShift, parameters).FirstOrDefault();
        }

        /// <summary>
        /// Asynchronously creates a new doctor shift.
        /// </summary>
        public async Task CreateShiftAsync(DoctorShift shift)
        {
            try
            {
                if (shift == null || shift.DoctorId <= 0) throw new ArgumentException("Invalid shift data.");

                string query = @"INSERT INTO DoctorShifts (DoctorId, DayOfWeek, StartTime, EndTime, ShiftType, IsActive, Notes, CreatedAt) 
                                VALUES (@DoctorId, @DayOfWeek, @StartTime, @EndTime, @ShiftType, @IsActive, @Notes, @CreatedAt)";
                var parameters = new[]
                {
                    new SqlParameter("@DoctorId", shift.DoctorId),
                    new SqlParameter("@DayOfWeek", shift.DayOfWeek ?? "Monday"),
                    new SqlParameter("@StartTime", shift.StartTime),
                    new SqlParameter("@EndTime", shift.EndTime),
                    new SqlParameter("@ShiftType", (object?)shift.ShiftType ?? DBNull.Value),
                    new SqlParameter("@IsActive", shift.IsActive),
                    new SqlParameter("@Notes", (object?)shift.Notes ?? DBNull.Value),
                    new SqlParameter("@CreatedAt", shift.CreatedAt == default ? DateTime.Now : shift.CreatedAt)
                };
                await _db.ExecuteNonQueryAsync(query, parameters);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create shift: {ex.Message}", ex);
            }
        }

        public void CreateShift(DoctorShift shift)
        {
            if (shift == null || shift.DoctorId <= 0) throw new ArgumentException("Invalid shift data.");
            string query = @"INSERT INTO DoctorShifts (DoctorId, DayOfWeek, StartTime, EndTime, ShiftType, IsActive, Notes, CreatedAt) VALUES (@DoctorId, @DayOfWeek, @StartTime, @EndTime, @ShiftType, @IsActive, @Notes, @CreatedAt)";
            var parameters = new[] { new SqlParameter("@DoctorId", shift.DoctorId), new SqlParameter("@DayOfWeek", shift.DayOfWeek ?? "Monday"), new SqlParameter("@StartTime", shift.StartTime), new SqlParameter("@EndTime", shift.EndTime), new SqlParameter("@ShiftType", (object?)shift.ShiftType ?? DBNull.Value), new SqlParameter("@IsActive", shift.IsActive), new SqlParameter("@Notes", (object?)shift.Notes ?? DBNull.Value), new SqlParameter("@CreatedAt", shift.CreatedAt == default ? DateTime.Now : shift.CreatedAt) };
            _db.ExecuteNonQuery(query, parameters);
        }

        /// <summary>
        /// Asynchronously updates an existing shift.
        /// </summary>
        public async Task UpdateShiftAsync(DoctorShift shift)
        {
            try
            {
                if (shift == null || shift.ShiftId <= 0) throw new ArgumentException("Invalid shift ID for update.");

                string query = @"UPDATE DoctorShifts SET DayOfWeek=@DayOfWeek, StartTime=@StartTime, EndTime=@EndTime, 
                                ShiftType=@ShiftType, IsActive=@IsActive, Notes=@Notes WHERE ShiftId=@ShiftId";
                var parameters = new[]
                {
                    new SqlParameter("@ShiftId", shift.ShiftId),
                    new SqlParameter("@DayOfWeek", shift.DayOfWeek ?? "Monday"),
                    new SqlParameter("@StartTime", shift.StartTime),
                    new SqlParameter("@EndTime", shift.EndTime),
                    new SqlParameter("@ShiftType", (object?)shift.ShiftType ?? DBNull.Value),
                    new SqlParameter("@IsActive", shift.IsActive),
                    new SqlParameter("@Notes", (object?)shift.Notes ?? DBNull.Value)
                };
                await _db.ExecuteNonQueryAsync(query, parameters);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to update shift {shift?.ShiftId}: {ex.Message}", ex);
            }
        }

        public void UpdateShift(DoctorShift shift)
        {
            if (shift == null || shift.ShiftId <= 0) throw new ArgumentException("Invalid shift ID for update.");
            string query = @"UPDATE DoctorShifts SET DayOfWeek=@DayOfWeek, StartTime=@StartTime, EndTime=@EndTime, ShiftType=@ShiftType, IsActive=@IsActive, Notes=@Notes WHERE ShiftId=@ShiftId";
            var parameters = new[] { new SqlParameter("@ShiftId", shift.ShiftId), new SqlParameter("@DayOfWeek", shift.DayOfWeek ?? "Monday"), new SqlParameter("@StartTime", shift.StartTime), new SqlParameter("@EndTime", shift.EndTime), new SqlParameter("@ShiftType", (object?)shift.ShiftType ?? DBNull.Value), new SqlParameter("@IsActive", shift.IsActive), new SqlParameter("@Notes", (object?)shift.Notes ?? DBNull.Value) };
            _db.ExecuteNonQuery(query, parameters);
        }

        /// <summary>
        /// Asynchronously marks a shift as inactive.
        /// </summary>
        public async Task DeleteShiftAsync(int shiftId)
        {
            try
            {
                if (shiftId <= 0) return;
                string query = "UPDATE DoctorShifts SET IsActive = 0 WHERE ShiftId = @Id";
                var parameters = new[] { new SqlParameter("@Id", shiftId) };
                await _db.ExecuteNonQueryAsync(query, parameters);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to delete shift {shiftId}: {ex.Message}", ex);
            }
        }

        public void DeleteShift(int shiftId)
        {
            if (shiftId <= 0) return;
            string query = "UPDATE DoctorShifts SET IsActive = 0 WHERE ShiftId = @Id";
            var parameters = new[] { new SqlParameter("@Id", shiftId) };
            _db.ExecuteNonQuery(query, parameters);
        }

        /// <summary>
        /// Checks if a doctor is available at a specific date and time based on their weekly shifts.
        /// </summary>
        /// <summary>
        /// Checks if a doctor is available at a specific date and time based on their weekly shifts.
        /// OPTIMIZATION: [In-Database Logic] Checking overlap in SQL is faster than fetching all shifts and checking in C#.
        /// </summary>
        public async Task<bool> IsAvailableAtTimeAsync(int doctorId, DateTime appointmentDateTime)
        {
            string dayOfWeek = appointmentDateTime.DayOfWeek.ToString();
            TimeSpan appointmentTime = appointmentDateTime.TimeOfDay;

            string query = @"SELECT COUNT(*) FROM DoctorShifts 
                            WHERE DoctorId = @DoctorId 
                            AND DayOfWeek = @DayOfWeek 
                            AND StartTime <= @AppointmentTime 
                            AND EndTime >= @AppointmentTime 
                            AND IsActive = 1";

            var parameters = new[]
            {
                new SqlParameter("@DoctorId", doctorId),
                new SqlParameter("@DayOfWeek", dayOfWeek),
                new SqlParameter("@AppointmentTime", appointmentTime)
            };

            return await _db.ExecuteScalarAsync<int>(query, parameters) > 0;
        }

        public bool IsAvailableAtTime(int doctorId, DateTime appointmentDateTime)
        {
            string dayOfWeek = appointmentDateTime.DayOfWeek.ToString();
            TimeSpan appointmentTime = appointmentDateTime.TimeOfDay;
            string query = @"SELECT COUNT(*) FROM DoctorShifts WHERE DoctorId = @DoctorId AND DayOfWeek = @DayOfWeek AND StartTime <= @AppointmentTime AND EndTime >= @AppointmentTime AND IsActive = 1";
            var parameters = new[] { new SqlParameter("@DoctorId", doctorId), new SqlParameter("@DayOfWeek", dayOfWeek), new SqlParameter("@AppointmentTime", appointmentTime) };
            return _db.ExecuteScalar<int>(query, parameters) > 0;
        }

        /// <summary>
        /// Mapping logic from SqlDataReader to DoctorShift model.
        /// </summary>
        private DoctorShift MapShift(SqlDataReader reader)
        {
            return new DoctorShift
            {
                ShiftId = reader.GetInt32(reader.GetOrdinal("ShiftId")),
                DoctorId = reader.GetInt32(reader.GetOrdinal("DoctorId")),
                DayOfWeek = reader["DayOfWeek"]?.ToString() ?? "",
                StartTime = reader.GetTimeSpan(reader.GetOrdinal("StartTime")),
                EndTime = reader.GetTimeSpan(reader.GetOrdinal("EndTime")),
                ShiftType = reader.IsDBNull(reader.GetOrdinal("ShiftType")) ? null : reader["ShiftType"]?.ToString(),
                IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                Notes = reader.IsDBNull(reader.GetOrdinal("Notes")) ? null : reader["Notes"]?.ToString(),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
            };
        }
    }
}

