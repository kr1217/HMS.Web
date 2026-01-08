using HMS.Web.Models;
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

namespace HMS.Web.DAL
{
    public class StaffRepository
    {
        private readonly DatabaseHelper _db;
        public StaffRepository(DatabaseHelper db)
        {
            _db = db;
        }

        public List<Staff> GetAllStaff()
        {
            try
            {
                // OPTIMIZATION: Default to top 100 if no pagination specified to avoid accidental lag
                string query = "SELECT TOP 100 * FROM Staff ORDER BY FullName";
                return _db.ExecuteQuery(query, MapStaff);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving staff list: {ex.Message}", ex);
            }
        }

        public List<Staff> GetStaffPaged(int skip, int take, string filter, string orderBy)
        {
            try
            {
                // Basic implementation for safety. In production, 'orderBy' and 'filter' should be validated or built using parameters.
                string orderClause = string.IsNullOrEmpty(orderBy) ? "FullName" : orderBy;

                // Keep it simple for now: valid columns for order by
                string query = $@"SELECT * FROM Staff ORDER BY {orderClause} 
                                 OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";

                var parameters = new[] {
                    new SqlParameter("@Skip", skip),
                    new SqlParameter("@Take", take)
                };

                return _db.ExecuteQuery(query, MapStaff, parameters);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving paged staff: {ex.Message}", ex);
            }
        }

        public int GetStaffCount()
        {
            try
            {
                return Convert.ToInt32(_db.ExecuteScalar("SELECT COUNT(*) FROM Staff"));
            }
            catch { return 0; }
        }

        public Staff? GetStaffById(int staffId)
        {
            try
            {
                if (staffId <= 0) return null;
                string query = "SELECT * FROM Staff WHERE StaffId = @StaffId";
                var parameters = new[] { new SqlParameter("@StaffId", staffId) };
                var table = _db.ExecuteDataTable(query, parameters);
                if (table != null && table.Rows.Count > 0)
                {
                    return MapStaff(table.Rows[0]);
                }
                return null;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving staff member {staffId}: {ex.Message}", ex);
            }
        }

        public Staff? GetStaffByUserId(string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId)) return null;
                string query = "SELECT * FROM Staff WHERE UserId = @UserId";
                var parameters = new[] { new SqlParameter("@UserId", userId) };
                var table = _db.ExecuteDataTable(query, parameters);
                if (table != null && table.Rows.Count > 0)
                {
                    return MapStaff(table.Rows[0]);
                }
                return null;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving staff record for user {userId}: {ex.Message}", ex);
            }
        }

        public void CreateStaff(Staff staff)
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
                _db.ExecuteNonQuery(query, parameters);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create staff record: {ex.Message}", ex);
            }
        }

        public void UpdateStaff(Staff staff)
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
                _db.ExecuteNonQuery(query, parameters);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to update staff record {staff?.StaffId}: {ex.Message}", ex);
            }
        }

        private Staff MapStaff(DataRow row)
        {
            return new Staff
            {
                StaffId = (int)row["StaffId"],
                UserId = row["UserId"]?.ToString() ?? "",
                FullName = row["FullName"]?.ToString() ?? "",
                Role = row["Role"]?.ToString() ?? "",
                Department = row["Department"]?.ToString() ?? "",
                Shift = row["Shift"]?.ToString() ?? "",
                Salary = row["Salary"] != DBNull.Value ? (decimal)row["Salary"] : 0,
                JoinDate = (DateTime)row["JoinDate"],
                IsActive = (bool)row["IsActive"],
                Email = row["Email"]?.ToString() ?? "",
                PhoneNumber = row["PhoneNumber"]?.ToString() ?? ""
            };
        }
    }
}
