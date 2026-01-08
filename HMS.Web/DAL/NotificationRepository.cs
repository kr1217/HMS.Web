/*
 * FILE: NotificationRepository.cs
 * PURPOSE: Manages system notifications.
 * COMMUNICATES WITH: DatabaseHelper, Shared/MainLayout.razor
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
    /// Repository for managing system-wide alerts and notifications.
    /// OPTIMIZATION: [Role-Based Delivery] Notifications are targeted by Role (e.g., 'Admin') or specific UserID to reduce noise.
    /// </summary>
    public class NotificationRepository
    {
        private readonly DatabaseHelper _db;
        public NotificationRepository(DatabaseHelper db) { _db = db; }

        private const string NotificationColumns = "NotificationId, PatientId, DoctorId, TargetRole, Title, Message, CreatedDate, IsRead";

        /// <summary>
        /// Retrieves notifications for a specific patient.
        /// </summary>
        public async Task<List<Notification>> GetNotificationsByPatientIdAsync(int patientId)
        {
            try
            {
                if (patientId <= 0) return new List<Notification>();
                string query = $"SELECT {NotificationColumns} FROM Notifications WHERE PatientId = @Id ORDER BY CreatedDate DESC";
                var parameters = new[] { new SqlParameter("@Id", patientId) };
                return await _db.ExecuteQueryAsync(query, MapNotification, parameters);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving notifications for patient {patientId}: {ex.Message}", ex);
            }
        }

        public List<Notification> GetNotificationsByPatientId(int patientId)
        {
            if (patientId <= 0) return new List<Notification>();
            string query = $"SELECT {NotificationColumns} FROM Notifications WHERE PatientId = @Id ORDER BY CreatedDate DESC";
            var parameters = new[] { new SqlParameter("@Id", patientId) };
            return _db.ExecuteQuery(query, MapNotification, parameters);
        }

        /// <summary>
        /// Retrieves notifications for a specific doctor.
        /// </summary>
        public async Task<List<Notification>> GetNotificationsByDoctorIdAsync(int doctorId)
        {
            try
            {
                if (doctorId <= 0) return new List<Notification>();
                string query = $"SELECT {NotificationColumns} FROM Notifications WHERE DoctorId = @Id ORDER BY CreatedDate DESC";
                var parameters = new[] { new SqlParameter("@Id", doctorId) };
                return await _db.ExecuteQueryAsync(query, MapNotification, parameters);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving notifications for doctor {doctorId}: {ex.Message}", ex);
            }
        }

        public List<Notification> GetNotificationsByDoctorId(int doctorId)
        {
            if (doctorId <= 0) return new List<Notification>();
            string query = $"SELECT {NotificationColumns} FROM Notifications WHERE DoctorId = @Id ORDER BY CreatedDate DESC";
            var parameters = new[] { new SqlParameter("@Id", doctorId) };
            return _db.ExecuteQuery(query, MapNotification, parameters);
        }

        /// <summary>
        /// Retrieves the most recent notifications for administrators.
        /// </summary>
        public async Task<List<Notification>> GetAdminNotificationsAsync()
        {
            try
            {
                string query = $"SELECT TOP 50 {NotificationColumns} FROM Notifications WHERE (PatientId IS NULL AND DoctorId IS NULL) OR TargetRole = 'Admin' ORDER BY CreatedDate DESC";
                return await _db.ExecuteQueryAsync(query, MapNotification);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving admin notifications: {ex.Message}", ex);
            }
        }

        public List<Notification> GetAdminNotifications()
        {
            string query = $"SELECT TOP 50 {NotificationColumns} FROM Notifications WHERE (PatientId IS NULL AND DoctorId IS NULL) OR TargetRole = 'Admin' ORDER BY CreatedDate DESC";
            return _db.ExecuteQuery(query, MapNotification);
        }

        /// <summary>
        /// Gets the count of unread admin notifications.
        /// </summary>
        public async Task<int> GetAdminUnreadCountAsync()
        {
            string query = "SELECT COUNT(*) FROM Notifications WHERE ((PatientId IS NULL AND DoctorId IS NULL) OR TargetRole = 'Admin') AND IsRead = 0";
            return await _db.ExecuteScalarAsync<int>(query);
        }

        public int GetAdminUnreadCount()
        {
            string query = "SELECT COUNT(*) FROM Notifications WHERE ((PatientId IS NULL AND DoctorId IS NULL) OR TargetRole = 'Admin') AND IsRead = 0";
            return _db.ExecuteScalar<int>(query);
        }

        /// <summary>
        /// Marks all admin notifications as read.
        /// </summary>
        public async Task MarkAdminNotificationsAsReadAsync()
        {
            const string sql = "UPDATE Notifications SET IsRead = 1 WHERE (PatientId IS NULL AND DoctorId IS NULL) OR TargetRole = 'Admin'";
            await _db.ExecuteNonQueryAsync(sql);
        }

        public void MarkAdminNotificationsAsRead()
        {
            const string sql = "UPDATE Notifications SET IsRead = 1 WHERE (PatientId IS NULL AND DoctorId IS NULL) OR TargetRole = 'Admin'";
            _db.ExecuteNonQuery(sql);
        }

        /// <summary>
        /// Retrieves recent notifications for a specific role.
        /// </summary>
        public async Task<List<Notification>> GetNotificationsByRoleAsync(string role)
        {
            try
            {
                if (string.IsNullOrEmpty(role)) return new List<Notification>();
                string query = $"SELECT TOP 50 {NotificationColumns} FROM Notifications WHERE TargetRole = @Role ORDER BY CreatedDate DESC";
                var parameters = new[] { new SqlParameter("@Role", role) };
                return await _db.ExecuteQueryAsync(query, MapNotification, parameters);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving notifications for role {role}: {ex.Message}", ex);
            }
        }

        public List<Notification> GetNotificationsByRole(string role)
        {
            if (string.IsNullOrEmpty(role)) return new List<Notification>();
            string query = $"SELECT TOP 50 {NotificationColumns} FROM Notifications WHERE TargetRole = @Role ORDER BY CreatedDate DESC";
            var parameters = new[] { new SqlParameter("@Role", role) };
            return _db.ExecuteQuery(query, MapNotification, parameters);
        }

        /// <summary>
        /// Gets the unread notification count for a specific role.
        /// </summary>
        public async Task<int> GetRoleUnreadCountAsync(string role)
        {
            if (string.IsNullOrEmpty(role)) return 0;
            string query = "SELECT COUNT(*) FROM Notifications WHERE TargetRole = @Role AND IsRead = 0";
            var parameters = new[] { new SqlParameter("@Role", role) };
            return await _db.ExecuteScalarAsync<int>(query, parameters);
        }

        public int GetRoleUnreadCount(string role)
        {
            if (string.IsNullOrEmpty(role)) return 0;
            string query = "SELECT COUNT(*) FROM Notifications WHERE TargetRole = @Role AND IsRead = 0";
            var parameters = new[] { new SqlParameter("@Role", role) };
            return _db.ExecuteScalar<int>(query, parameters);
        }

        /// <summary>
        /// Gets the unread notification count for a specific doctor.
        /// </summary>
        public async Task<int> GetDoctorUnreadCountAsync(int doctorId)
        {
            string query = "SELECT COUNT(*) FROM Notifications WHERE DoctorId = @Id AND IsRead = 0";
            return await _db.ExecuteScalarAsync<int>(query, new[] { new SqlParameter("@Id", doctorId) });
        }

        public int GetDoctorUnreadCount(int doctorId)
        {
            string query = "SELECT COUNT(*) FROM Notifications WHERE DoctorId = @Id AND IsRead = 0";
            return _db.ExecuteScalar<int>(query, new[] { new SqlParameter("@Id", doctorId) });
        }

        /// <summary>
        /// Gets the unread notification count for a specific patient.
        /// </summary>
        public async Task<int> GetPatientUnreadCountAsync(int patientId)
        {
            string query = "SELECT COUNT(*) FROM Notifications WHERE PatientId = @Id AND IsRead = 0";
            return await _db.ExecuteScalarAsync<int>(query, new[] { new SqlParameter("@Id", patientId) });
        }

        public int GetPatientUnreadCount(int patientId)
        {
            string query = "SELECT COUNT(*) FROM Notifications WHERE PatientId = @Id AND IsRead = 0";
            return _db.ExecuteScalar<int>(query, new[] { new SqlParameter("@Id", patientId) });
        }

        /// <summary>
        /// Marks all notifications as read for a specific doctor or patient.
        /// </summary>
        public async Task MarkNotificationsAsReadAsync(int? doctorId, int? patientId)
        {
            string sql = "UPDATE Notifications SET IsRead = 1 WHERE ";
            if (doctorId.HasValue)
                await _db.ExecuteNonQueryAsync(sql + "DoctorId = @Id", new[] { new SqlParameter("@Id", doctorId.Value) });
            else if (patientId.HasValue)
                await _db.ExecuteNonQueryAsync(sql + "PatientId = @Id", new[] { new SqlParameter("@Id", patientId.Value) });
        }

        public void MarkNotificationsAsRead(int? doctorId, int? patientId)
        {
            string sql = "UPDATE Notifications SET IsRead = 1 WHERE ";
            if (doctorId.HasValue)
                _db.ExecuteNonQuery(sql + "DoctorId = @Id", new[] { new SqlParameter("@Id", doctorId.Value) });
            else if (patientId.HasValue)
                _db.ExecuteNonQuery(sql + "PatientId = @Id", new[] { new SqlParameter("@Id", patientId.Value) });
        }

        /// <summary>
        /// Marks all notifications as read for a specific role.
        /// </summary>
        public async Task MarkRoleNotificationsAsReadAsync(string role)
        {
            if (string.IsNullOrEmpty(role)) return;
            const string sql = "UPDATE Notifications SET IsRead = 1 WHERE TargetRole = @Role";
            await _db.ExecuteNonQueryAsync(sql, new[] { new SqlParameter("@Role", role) });
        }

        public void MarkRoleNotificationsAsRead(string role)
        {
            if (string.IsNullOrEmpty(role)) return;
            const string sql = "UPDATE Notifications SET IsRead = 1 WHERE TargetRole = @Role";
            _db.ExecuteNonQuery(sql, new[] { new SqlParameter("@Role", role) });
        }

        /// <summary>
        /// Asynchronously creates a new system notification.
        /// </summary>
        public async Task CreateNotificationAsync(Notification n)
        {
            try
            {
                if (n == null) throw new ArgumentException("Notification data is required.");
                string query = "INSERT INTO Notifications (PatientId, DoctorId, TargetRole, Title, Message, CreatedDate, IsRead) VALUES (@PatientId, @DoctorId, @TargetRole, @Title, @Message, @CreatedDate, @IsRead)";
                await _db.ExecuteNonQueryAsync(query, new[] {
                    new SqlParameter("@PatientId", (object?)n.PatientId ?? DBNull.Value),
                    new SqlParameter("@DoctorId", (object?)n.DoctorId ?? DBNull.Value),
                    new SqlParameter("@TargetRole", (object?)n.TargetRole ?? DBNull.Value),
                    new SqlParameter("@Title", n.Title ?? "System Notification"),
                    new SqlParameter("@Message", n.Message ?? ""),
                    new SqlParameter("@CreatedDate", n.CreatedDate == default ? DateTime.Now : n.CreatedDate),
                    new SqlParameter("@IsRead", n.IsRead)
                });
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create notification: {ex.Message}", ex);
            }
        }

        public void CreateNotification(Notification n)
        {
            if (n == null) throw new ArgumentException("Notification data is required.");
            string query = "INSERT INTO Notifications (PatientId, DoctorId, TargetRole, Title, Message, CreatedDate, IsRead) VALUES (@PatientId, @DoctorId, @TargetRole, @Title, @Message, @CreatedDate, @IsRead)";
            _db.ExecuteNonQuery(query, new[] { new SqlParameter("@PatientId", (object?)n.PatientId ?? DBNull.Value), new SqlParameter("@DoctorId", (object?)n.DoctorId ?? DBNull.Value), new SqlParameter("@TargetRole", (object?)n.TargetRole ?? DBNull.Value), new SqlParameter("@Title", n.Title ?? "System Notification"), new SqlParameter("@Message", n.Message ?? ""), new SqlParameter("@CreatedDate", n.CreatedDate == default ? DateTime.Now : n.CreatedDate), new SqlParameter("@IsRead", n.IsRead) });
        }

        /// <summary>
        /// Mapping logic from SqlDataReader to Notification model.
        /// </summary>
        private Notification MapNotification(SqlDataReader reader)
        {
            return new Notification
            {
                NotificationId = reader.GetInt32(reader.GetOrdinal("NotificationId")),
                PatientId = reader.IsDBNull(reader.GetOrdinal("PatientId")) ? null : (int?)reader.GetInt32(reader.GetOrdinal("PatientId")),
                DoctorId = reader.IsDBNull(reader.GetOrdinal("DoctorId")) ? null : (int?)reader.GetInt32(reader.GetOrdinal("DoctorId")),
                TargetRole = reader.IsDBNull(reader.GetOrdinal("TargetRole")) ? null : reader["TargetRole"]?.ToString(),
                Title = reader["Title"]?.ToString() ?? string.Empty,
                Message = reader["Message"]?.ToString() ?? "",
                CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
                IsRead = reader.GetBoolean(reader.GetOrdinal("IsRead"))
            };
        }
    }
}

