/*
 * FILE: StaffRepository.cs
 * PURPOSE: Manages hospital staff accounts.
 * COMMUNICATES WITH: DatabaseHelper, Admin/StaffManagement.razor
 */
using HMS.Web.Models;
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using System.Linq;

namespace HMS.Web.DAL
{
    /// <summary>
    /// Repository for managing hospital staff and their details (excluding doctors).
    /// OPTIMIZATION: [Explicit Columns] See notes below on explicit column selection vs SELECT *.
    /// </summary>
    public class StaffRepository
    {
        private readonly DatabaseHelper _db;
        public StaffRepository(DatabaseHelper db) { _db = db; }

        // OPTIMIZATION: [Explicit Columns] Avoided SELECT * to reduce memory footprint and enable SQL Index Covering.
        // WHY: Pulling unnecessary blob/text columns from the DB wastes bandwidth and slows down query execution.
        private const string StaffColumns = "StaffId, UserId, FullName, Role, Department, Shift, Salary, JoinDate, IsActive, Email, PhoneNumber";

        /// <summary>
        /// Retrieves a list of all staff members (limited to top 100).
        /// OPTIMIZATION: [Strict Fetching] Hard limit of 100 records for non-paged calls to prevent accidental UI locks.
        /// </summary>
        public async Task<List<Staff>> GetAllStaffAsync()
        {
            try
            {
                // OPTIMIZATION: Default to top 100 if no pagination specified to avoid accidental lag
                string query = $"SELECT TOP 100 {StaffColumns} FROM Staff ORDER BY FullName";
                return await _db.ExecuteQueryAsync(query, MapStaff);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving staff list: {ex.Message}", ex);
            }
        }

        public List<Staff> GetAllStaff()
        {
            string query = $"SELECT TOP 100 {StaffColumns} FROM Staff ORDER BY FullName";
            return _db.ExecuteQuery(query, MapStaff);
        }

        /// <summary>
        /// Retrieves a paged list of staff members with optional ordering.
        /// </summary>
        public async Task<List<Staff>> GetStaffPagedAsync(int skip, int take, string filter, string orderBy)
        {
            try
            {
                string orderClause = string.IsNullOrEmpty(orderBy) ? "FullName" : orderBy;
                string query = $@"SELECT {StaffColumns} FROM Staff ORDER BY {orderClause} 
                                 OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";

                var parameters = new[] {
                    new SqlParameter("@Skip", skip),
                    new SqlParameter("@Take", take)
                };

                return await _db.ExecuteQueryAsync(query, MapStaff, parameters);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving paged staff: {ex.Message}", ex);
            }
        }

        public List<Staff> GetStaffPaged(int skip, int take, string filter, string orderBy)
        {
            string orderClause = string.IsNullOrEmpty(orderBy) ? "FullName" : orderBy;
            string query = $@"SELECT {StaffColumns} FROM Staff ORDER BY {orderClause} OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";
            var parameters = new[] { new SqlParameter("@Skip", skip), new SqlParameter("@Take", take) };
            return _db.ExecuteQuery(query, MapStaff, parameters);
        }

        /// <summary>
        /// Gets the total count of staff members.
        /// </summary>
        public async Task<int> GetStaffCountAsync()
        {
            var result = await _db.ExecuteScalarAsync("SELECT COUNT(*) FROM Staff");
            return Convert.ToInt32(result ?? 0);
        }

        public int GetStaffCount()
        {
            return Convert.ToInt32(_db.ExecuteScalar("SELECT COUNT(*) FROM Staff") ?? 0);
        }

        /// <summary>
        /// Retrieves a staff member by their primary key.
        /// </summary>
        public async Task<Staff?> GetStaffByIdAsync(int staffId)
        {
            try
            {
                if (staffId <= 0) return null;
                string query = $"SELECT {StaffColumns} FROM Staff WHERE StaffId = @Id";
                var parameters = new[] { new SqlParameter("@Id", staffId) };
                var list = await _db.ExecuteQueryAsync(query, MapStaff, parameters);
                return list.FirstOrDefault();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving staff member {staffId}: {ex.Message}", ex);
            }
        }

        public Staff? GetStaffById(int staffId)
        {
            if (staffId <= 0) return null;
            string query = $"SELECT {StaffColumns} FROM Staff WHERE StaffId = @Id";
            var parameters = new[] { new SqlParameter("@Id", staffId) };
            return _db.ExecuteQuery(query, MapStaff, parameters).FirstOrDefault();
        }

        /// <summary>
        /// Retrieves a staff member by their associated User ID.
        /// </summary>
        public async Task<Staff?> GetStaffByUserIdAsync(string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId)) return null;
                string query = $"SELECT {StaffColumns} FROM Staff WHERE UserId = @Id";
                var parameters = new[] { new SqlParameter("@Id", userId) };
                var list = await _db.ExecuteQueryAsync(query, MapStaff, parameters);
                return list.FirstOrDefault();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving staff record for user {userId}: {ex.Message}", ex);
            }
        }

        public Staff? GetStaffByUserId(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return null;
            string query = $"SELECT {StaffColumns} FROM Staff WHERE UserId = @Id";
            var parameters = new[] { new SqlParameter("@Id", userId) };
            return _db.ExecuteQuery(query, MapStaff, parameters).FirstOrDefault();
        }

        /// <summary>
        /// Asynchronously creates a new staff record.
        /// </summary>
        public async Task CreateStaffAsync(Staff staff)
        {
            try
            {
                if (staff == null || string.IsNullOrEmpty(staff.FullName))
                    throw new ArgumentException("Staff data and Full Name are required.");

                string query = @"INSERT INTO Staff (UserId, FullName, Role, Department, Shift, Salary, JoinDate, IsActive, Email, PhoneNumber) 
                                VALUES (@UserId, @FullName, @Role, @Department, @Shift, @Salary, @JoinDate, @IsActive, @Email, @PhoneNumber)";
                var parameters = new[]
                {
                    new SqlParameter("@UserId", (object?)staff.UserId ?? DBNull.Value),
                    new SqlParameter("@FullName", staff.FullName),
                    new SqlParameter("@Role", staff.Role ?? "Staff"),
                    new SqlParameter("@Department", (object?)staff.Department ?? DBNull.Value),
                    new SqlParameter("@Shift", (object?)staff.Shift ?? DBNull.Value),
                    new SqlParameter("@Salary", staff.Salary),
                    new SqlParameter("@JoinDate", staff.JoinDate == default ? DateTime.Now : staff.JoinDate),
                    new SqlParameter("@IsActive", staff.IsActive),
                    new SqlParameter("@Email", (object?)staff.Email ?? DBNull.Value),
                    new SqlParameter("@PhoneNumber", (object?)staff.PhoneNumber ?? DBNull.Value)
                };
                await _db.ExecuteNonQueryAsync(query, parameters);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create staff record: {ex.Message}", ex);
            }
        }

        public void CreateStaff(Staff staff)
        {
            if (staff == null || string.IsNullOrEmpty(staff.FullName)) throw new ArgumentException("Staff data and Full Name are required.");
            string query = @"INSERT INTO Staff (UserId, FullName, Role, Department, Shift, Salary, JoinDate, IsActive, Email, PhoneNumber) VALUES (@UserId, @FullName, @Role, @Department, @Shift, @Salary, @JoinDate, @IsActive, @Email, @PhoneNumber)";
            var parameters = new[] { new SqlParameter("@UserId", (object?)staff.UserId ?? DBNull.Value), new SqlParameter("@FullName", staff.FullName), new SqlParameter("@Role", staff.Role ?? "Staff"), new SqlParameter("@Department", (object?)staff.Department ?? DBNull.Value), new SqlParameter("@Shift", (object?)staff.Shift ?? DBNull.Value), new SqlParameter("@Salary", staff.Salary), new SqlParameter("@JoinDate", staff.JoinDate == default ? DateTime.Now : staff.JoinDate), new SqlParameter("@IsActive", staff.IsActive), new SqlParameter("@Email", (object?)staff.Email ?? DBNull.Value), new SqlParameter("@PhoneNumber", (object?)staff.PhoneNumber ?? DBNull.Value) };
            _db.ExecuteNonQuery(query, parameters);
        }

        /// <summary>
        /// Asynchronously updates an existing staff record.
        /// </summary>
        public async Task UpdateStaffAsync(Staff staff)
        {
            try
            {
                if (staff == null || staff.StaffId <= 0)
                    throw new ArgumentException("Invalid staff record for update.");

                string query = @"UPDATE Staff SET FullName=@FullName, Role=@Role, Department=@Department, Shift=@Shift, 
                                 Salary=@Salary, IsActive=@IsActive, Email=@Email, PhoneNumber=@PhoneNumber WHERE StaffId=@StaffId";
                var parameters = new[]
                {
                    new SqlParameter("@StaffId", staff.StaffId),
                    new SqlParameter("@FullName", staff.FullName ?? ""),
                    new SqlParameter("@Role", staff.Role ?? ""),
                    new SqlParameter("@Department", (object?)staff.Department ?? DBNull.Value),
                    new SqlParameter("@Shift", (object?)staff.Shift ?? DBNull.Value),
                    new SqlParameter("@Salary", staff.Salary),
                    new SqlParameter("@IsActive", staff.IsActive),
                    new SqlParameter("@Email", (object?)staff.Email ?? DBNull.Value),
                    new SqlParameter("@PhoneNumber", (object?)staff.PhoneNumber ?? DBNull.Value)
                };
                await _db.ExecuteNonQueryAsync(query, parameters);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to update staff record {staff?.StaffId}: {ex.Message}", ex);
            }
        }

        public void UpdateStaff(Staff staff)
        {
            if (staff == null || staff.StaffId <= 0) throw new ArgumentException("Invalid staff record for update.");
            string query = @"UPDATE Staff SET FullName=@FullName, Role=@Role, Department=@Department, Shift=@Shift, Salary=@Salary, IsActive=@IsActive, Email=@Email, PhoneNumber=@PhoneNumber WHERE StaffId=@StaffId";
            var parameters = new[] { new SqlParameter("@StaffId", staff.StaffId), new SqlParameter("@FullName", staff.FullName ?? ""), new SqlParameter("@Role", staff.Role ?? ""), new SqlParameter("@Department", (object?)staff.Department ?? DBNull.Value), new SqlParameter("@Shift", (object?)staff.Shift ?? DBNull.Value), new SqlParameter("@Salary", staff.Salary), new SqlParameter("@IsActive", staff.IsActive), new SqlParameter("@Email", (object?)staff.Email ?? DBNull.Value), new SqlParameter("@PhoneNumber", (object?)staff.PhoneNumber ?? DBNull.Value) };
            _db.ExecuteNonQuery(query, parameters);
        }

        /// <summary>
        /// Mapping logic from SqlDataReader to Staff model.
        /// </summary>
        private Staff MapStaff(SqlDataReader reader)
        {
            return new Staff
            {
                StaffId = reader.GetInt32(reader.GetOrdinal("StaffId")),
                UserId = reader["UserId"]?.ToString() ?? "",
                FullName = reader["FullName"]?.ToString() ?? "",
                Role = reader["Role"]?.ToString() ?? "",
                Department = reader["Department"]?.ToString() ?? "",
                Shift = reader["Shift"]?.ToString() ?? "",
                Salary = reader.IsDBNull(reader.GetOrdinal("Salary")) ? 0 : reader.GetDecimal(reader.GetOrdinal("Salary")),
                JoinDate = reader.GetDateTime(reader.GetOrdinal("JoinDate")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                Email = reader["Email"]?.ToString() ?? "",
                PhoneNumber = reader["PhoneNumber"]?.ToString() ?? ""
            };
        }
    }
}

