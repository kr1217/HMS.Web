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
            string query = "SELECT * FROM Staff ORDER BY FullName";
            var table = _db.ExecuteDataTable(query);
            var list = new List<Staff>();
            if (table != null)
            {
                foreach (DataRow row in table.Rows)
                {
                    list.Add(MapStaff(row));
                }
            }
            return list;
        }

        public Staff? GetStaffById(int staffId)
        {
            string query = "SELECT * FROM Staff WHERE StaffId = @StaffId";
            var parameters = new[] { new SqlParameter("@StaffId", staffId) };
            var table = _db.ExecuteDataTable(query, parameters);
            if (table != null && table.Rows.Count > 0)
            {
                return MapStaff(table.Rows[0]);
            }
            return null;
        }

        public Staff? GetStaffByUserId(string userId)
        {
            string query = "SELECT * FROM Staff WHERE UserId = @UserId";
            var parameters = new[] { new SqlParameter("@UserId", userId) };
            var table = _db.ExecuteDataTable(query, parameters);
            if (table != null && table.Rows.Count > 0)
            {
                return MapStaff(table.Rows[0]);
            }
            return null;
        }

        public void CreateStaff(Staff staff)
        {
            string query = @"INSERT INTO Staff (UserId, FullName, Role, Department, Shift, Salary, JoinDate, IsActive, Email, PhoneNumber) 
                            VALUES (@UserId, @FullName, @Role, @Department, @Shift, @Salary, @JoinDate, @IsActive, @Email, @PhoneNumber)";
            var parameters = new[]
            {
                new SqlParameter("@UserId", (object?)staff.UserId ?? DBNull.Value),
                new SqlParameter("@FullName", staff.FullName),
                new SqlParameter("@Role", staff.Role),
                new SqlParameter("@Department", (object?)staff.Department ?? DBNull.Value),
                new SqlParameter("@Shift", (object?)staff.Shift ?? DBNull.Value),
                new SqlParameter("@Salary", staff.Salary),
                new SqlParameter("@JoinDate", staff.JoinDate),
                new SqlParameter("@IsActive", staff.IsActive),
                new SqlParameter("@Email", (object?)staff.Email ?? DBNull.Value),
                new SqlParameter("@PhoneNumber", (object?)staff.PhoneNumber ?? DBNull.Value)
            };
            _db.ExecuteNonQuery(query, parameters);
        }

        public void UpdateStaff(Staff staff)
        {
            string query = @"UPDATE Staff SET FullName=@FullName, Role=@Role, Department=@Department, Shift=@Shift, 
                             Salary=@Salary, IsActive=@IsActive, Email=@Email, PhoneNumber=@PhoneNumber WHERE StaffId=@StaffId";
            var parameters = new[]
            {
                new SqlParameter("@StaffId", staff.StaffId),
                new SqlParameter("@FullName", staff.FullName),
                new SqlParameter("@Role", staff.Role),
                new SqlParameter("@Department", (object?)staff.Department ?? DBNull.Value),
                new SqlParameter("@Shift", (object?)staff.Shift ?? DBNull.Value),
                new SqlParameter("@Salary", staff.Salary),
                new SqlParameter("@IsActive", staff.IsActive),
                new SqlParameter("@Email", (object?)staff.Email ?? DBNull.Value),
                new SqlParameter("@PhoneNumber", (object?)staff.PhoneNumber ?? DBNull.Value)
            };
            _db.ExecuteNonQuery(query, parameters);
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
