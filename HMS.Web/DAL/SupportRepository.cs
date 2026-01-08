/*
 * FILE: SupportRepository.cs
 * PURPOSE: Manages helpdesk support tickets.
 * COMMUNICATES WITH: DatabaseHelper, Patient/Support.razor
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
    /// Repository for managing patient support tickets.
    /// OPTIMIZATION: [Simple Indexing] High-volume table optimized with indexes on PatientID and Status for quick dashboard filtering.
    /// </summary>
    public class SupportRepository
    {
        private readonly DatabaseHelper _db;
        public SupportRepository(DatabaseHelper db) { _db = db; }

        private const string TicketColumns = "TicketId, PatientId, Subject, Message, Status, CreatedDate, Response";

        /// <summary>
        /// Retrieves all support tickets for a specific patient.
        /// </summary>
        public async Task<List<SupportTicket>> GetTicketsByPatientIdAsync(int patientId)
        {
            try
            {
                if (patientId <= 0) return new List<SupportTicket>();
                string query = $"SELECT {TicketColumns} FROM SupportTickets WHERE PatientId = @Id ORDER BY CreatedDate DESC";
                var parameters = new[] { new SqlParameter("@Id", patientId) };
                return await _db.ExecuteQueryAsync(query, MapSupportTicket, parameters);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving support tickets for patient {patientId}: {ex.Message}", ex);
            }
        }

        public List<SupportTicket> GetTicketsByPatientId(int patientId)
        {
            if (patientId <= 0) return new List<SupportTicket>();
            string query = $"SELECT {TicketColumns} FROM SupportTickets WHERE PatientId = @Id ORDER BY CreatedDate DESC";
            var parameters = new[] { new SqlParameter("@Id", patientId) };
            return _db.ExecuteQuery(query, MapSupportTicket, parameters);
        }

        /// <summary>
        /// Asynchronously creates a new support ticket.
        /// </summary>
        public async Task CreateTicketAsync(SupportTicket ticket)
        {
            try
            {
                if (ticket == null || ticket.PatientId <= 0) throw new ArgumentException("Invalid ticket data.");

                string query = "INSERT INTO SupportTickets (PatientId, Subject, Message, Status, CreatedDate) VALUES (@PatientId, @Subject, @Message, 'Open', @CreatedDate)";
                var parameters = new[]
                {
                    new SqlParameter("@PatientId", ticket.PatientId),
                    new SqlParameter("@Subject", ticket.Subject ?? "No Subject"),
                    new SqlParameter("@Message", ticket.Message ?? ""),
                    new SqlParameter("@CreatedDate", DateTime.Now)
                };
                await _db.ExecuteNonQueryAsync(query, parameters);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create support ticket: {ex.Message}", ex);
            }
        }

        public void CreateTicket(SupportTicket ticket)
        {
            if (ticket == null || ticket.PatientId <= 0) throw new ArgumentException("Invalid ticket data.");
            string query = "INSERT INTO SupportTickets (PatientId, Subject, Message, Status, CreatedDate) VALUES (@PatientId, @Subject, @Message, 'Open', @CreatedDate)";
            var parameters = new[] { new SqlParameter("@PatientId", ticket.PatientId), new SqlParameter("@Subject", ticket.Subject ?? "No Subject"), new SqlParameter("@Message", ticket.Message ?? ""), new SqlParameter("@CreatedDate", DateTime.Now) };
            _db.ExecuteNonQuery(query, parameters);
        }

        /// <summary>
        /// Mapping logic from SqlDataReader to SupportTicket model.
        /// </summary>
        private SupportTicket MapSupportTicket(SqlDataReader reader)
        {
            return new SupportTicket
            {
                TicketId = reader.GetInt32(reader.GetOrdinal("TicketId")),
                PatientId = reader.GetInt32(reader.GetOrdinal("PatientId")),
                Subject = reader["Subject"]?.ToString() ?? "No Subject",
                Message = reader["Message"]?.ToString() ?? "",
                Status = reader["Status"]?.ToString() ?? "Open",
                CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
                Response = reader.IsDBNull(reader.GetOrdinal("Response")) ? null : reader["Response"]?.ToString()
            };
        }
    }
}

