/*
 * FILE: PatientLossRepository.cs
 * PURPOSE: Manages storage and retrieval of patient loss events and generated opportunities.
 * COMMUNICATES WITH: DatabaseHelper, PatientLossService
 */

using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using HMS.Web.Models;

namespace HMS.Web.DAL
{
    public class PatientLossRepository
    {
        private readonly DatabaseHelper _db;

        public PatientLossRepository(DatabaseHelper db)
        {
            _db = db;
        }

        public async Task AddLossEventAsync(PatientLossEvent lossEvent)
        {
            const string query = @"
                INSERT INTO PatientLossEvents (
                    PatientId, AttemptedAt, EntryPoint, RequestedDepartmentId, 
                    RequestedSpecialization, RequestedProcedureId, RequestedDoctorId, 
                    LossReasonCode, LossReasonDetail, EstimatedValue, Status, RecordedBy
                ) VALUES (
                    @PatientId, @AttemptedAt, @EntryPoint, @RequestedDepartmentId, 
                    @RequestedSpecialization, @RequestedProcedureId, @RequestedDoctorId, 
                    @LossReasonCode, @LossReasonDetail, @EstimatedValue, @Status, @RecordedBy
                )";

            var parameters = new[]
            {
                new SqlParameter("@PatientId", (object?)lossEvent.PatientId ?? DBNull.Value),
                new SqlParameter("@AttemptedAt", lossEvent.AttemptedAt),
                new SqlParameter("@EntryPoint", lossEvent.EntryPoint),
                new SqlParameter("@RequestedDepartmentId", (object?)lossEvent.RequestedDepartmentId ?? DBNull.Value),
                new SqlParameter("@RequestedSpecialization", (object?)lossEvent.RequestedSpecialization ?? DBNull.Value),
                new SqlParameter("@RequestedProcedureId", (object?)lossEvent.RequestedProcedureId ?? DBNull.Value),
                new SqlParameter("@RequestedDoctorId", (object?)lossEvent.RequestedDoctorId ?? DBNull.Value),
                new SqlParameter("@LossReasonCode", lossEvent.LossReasonCode.ToString()),
                new SqlParameter("@LossReasonDetail", (object?)lossEvent.LossReasonDetail ?? DBNull.Value),
                new SqlParameter("@EstimatedValue", lossEvent.EstimatedValue),
                new SqlParameter("@Status", lossEvent.Status),
                new SqlParameter("@RecordedBy", lossEvent.RecordedBy)
            };

            await _db.ExecuteNonQueryAsync(query, parameters);
        }

        public async Task<List<PatientLossEvent>> GetRecentLossesAsync(int days = 30)
        {
            string query = "SELECT * FROM PatientLossEvents WHERE AttemptedAt >= DATEADD(day, -@Days, GETDATE()) ORDER BY AttemptedAt DESC";
            return await _db.ExecuteQueryAsync(query, MapLossEvent, new[] { new SqlParameter("@Days", days) });
        }

        public async Task<List<LossStat>> GetLossStatsByReasonAsync(int days = 30)
        {
            string query = @"
                SELECT LossReasonCode, COUNT(*) as Count, SUM(EstimatedValue) as TotalValue 
                FROM PatientLossEvents 
                WHERE AttemptedAt >= DATEADD(day, -@Days, GETDATE()) 
                GROUP BY LossReasonCode 
                ORDER BY Count DESC";

            return await _db.ExecuteQueryAsync(query, reader => new LossStat
            {
                Reason = reader["LossReasonCode"].ToString() ?? "Unknown",
                Count = reader.GetInt32(reader.GetOrdinal("Count")),
                TotalValue = reader.GetDecimal(reader.GetOrdinal("TotalValue"))
            }, new[] { new SqlParameter("@Days", days) });
        }

        public async Task AddOpportunityAsync(HospitalOpportunity opportunity)
        {
            const string query = @"
                INSERT INTO HospitalOpportunities (
                    Type, Target, LossCount, EstimatedMonthlyRevenue, 
                    EstimatedAnnualRevenue, ConfidenceScore, Status, GeneratedAt
                ) VALUES (
                    @Type, @Target, @LossCount, @EstimatedMonthlyRevenue, 
                    @EstimatedAnnualRevenue, @ConfidenceScore, @Status, @GeneratedAt
                )";

            var parameters = new[]
            {
                new SqlParameter("@Type", opportunity.Type),
                new SqlParameter("@Target", opportunity.Target),
                new SqlParameter("@LossCount", opportunity.LossCount),
                new SqlParameter("@EstimatedMonthlyRevenue", opportunity.EstimatedMonthlyRevenue),
                new SqlParameter("@EstimatedAnnualRevenue", opportunity.EstimatedAnnualRevenue),
                new SqlParameter("@ConfidenceScore", opportunity.ConfidenceScore),
                new SqlParameter("@Status", opportunity.Status),
                new SqlParameter("@GeneratedAt", opportunity.GeneratedAt)
            };

            await _db.ExecuteNonQueryAsync(query, parameters);
        }

        public async Task<List<HospitalOpportunity>> GetActiveOpportunitiesAsync()
        {
            string query = "SELECT * FROM HospitalOpportunities WHERE Status IN ('New', 'Reviewed') ORDER BY EstimatedAnnualRevenue DESC";
            return await _db.ExecuteQueryAsync(query, MapOpportunity);
        }

        private PatientLossEvent MapLossEvent(SqlDataReader reader)
        {
            return new PatientLossEvent
            {
                LossId = reader.GetInt32(reader.GetOrdinal("LossId")),
                PatientId = reader.IsDBNull(reader.GetOrdinal("PatientId")) ? null : reader.GetInt32(reader.GetOrdinal("PatientId")),
                AttemptedAt = reader.GetDateTime(reader.GetOrdinal("AttemptedAt")),
                EntryPoint = reader["EntryPoint"].ToString() ?? "",
                RequestedDepartmentId = reader.IsDBNull(reader.GetOrdinal("RequestedDepartmentId")) ? null : reader.GetInt32(reader.GetOrdinal("RequestedDepartmentId")),
                RequestedSpecialization = reader["RequestedSpecialization"]?.ToString(),
                RequestedProcedureId = reader.IsDBNull(reader.GetOrdinal("RequestedProcedureId")) ? null : reader.GetInt32(reader.GetOrdinal("RequestedProcedureId")),
                RequestedDoctorId = reader.IsDBNull(reader.GetOrdinal("RequestedDoctorId")) ? null : reader.GetInt32(reader.GetOrdinal("RequestedDoctorId")),
                LossReasonCode = Enum.Parse<LossReasonCode>(reader["LossReasonCode"].ToString() ?? "OTHER"),
                LossReasonDetail = reader["LossReasonDetail"]?.ToString(),
                EstimatedValue = reader.GetDecimal(reader.GetOrdinal("EstimatedValue")),
                Status = reader["Status"].ToString() ?? "Lost",
                RecordedBy = reader["RecordedBy"].ToString() ?? "System"
            };
        }

        private HospitalOpportunity MapOpportunity(SqlDataReader reader)
        {
            return new HospitalOpportunity
            {
                OpportunityId = reader.GetInt32(reader.GetOrdinal("OpportunityId")),
                Type = reader["Type"].ToString() ?? "",
                Target = reader["Target"].ToString() ?? "",
                LossCount = reader.GetInt32(reader.GetOrdinal("LossCount")),
                EstimatedMonthlyRevenue = reader.GetDecimal(reader.GetOrdinal("EstimatedMonthlyRevenue")),
                EstimatedAnnualRevenue = reader.GetDecimal(reader.GetOrdinal("EstimatedAnnualRevenue")),
                ConfidenceScore = reader.GetDecimal(reader.GetOrdinal("ConfidenceScore")),
                Status = reader["Status"].ToString() ?? "New",
                GeneratedAt = reader.GetDateTime(reader.GetOrdinal("GeneratedAt"))
            };
        }
    }
}
