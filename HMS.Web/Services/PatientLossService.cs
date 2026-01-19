/*
 * FILE: PatientLossService.cs
 * PURPOSE: Domain logic for capturing patient loss and generating opportunities.
 * COMMUNICATES WITH: PatientLossRepository
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HMS.Web.DAL;
using HMS.Web.Models;

namespace HMS.Web.Services
{
    public interface IPatientLossService
    {
        Task RecordLossAsync(string entryPoint, LossReasonCode reason, string? detail = null, int? deptId = null, string? spec = null, int? docId = null);
        Task AnalyzeAndGenerateOpportunitiesAsync();
        Task<List<HospitalOpportunity>> GetOpportunitiesAsync();
        Task<List<LossStat>> GetLossAnalyticsAsync();
        Task<List<PatientLossEvent>> GetRecentLossesAsync(int days = 30);
        Task SeedSampleDataAsync();
    }

    public class PatientLossService : IPatientLossService
    {
        private readonly PatientLossRepository _repository;
        // In a real app, we might inject other repos to get Avg fees. 
        // For now, we will use static averages or simple assumption logic for demonstration
        // as per "Historic Averages" requirement.

        public PatientLossService(PatientLossRepository repository)
        {
            _repository = repository;
        }

        /// <summary>
        /// Records a patient loss event with calculated estimated value.
        /// </summary>
        public async Task RecordLossAsync(string entryPoint, LossReasonCode reason, string? detail = null, int? deptId = null, string? spec = null, int? docId = null)
        {
            // 1. Calculate Estimated Value (Domain Logic)
            // This should ideally come from a FinanceService or Cached Statistics.
            // Using simplified logic for now.
            decimal estimatedValue = CalculateEstimatedLoss(entryPoint, reason, spec);

            var lossEvent = new PatientLossEvent
            {
                AttemptedAt = DateTime.Now,
                EntryPoint = entryPoint,
                LossReasonCode = reason,
                LossReasonDetail = detail,
                RequestedDepartmentId = deptId,
                RequestedSpecialization = spec,
                RequestedDoctorId = docId,
                EstimatedValue = estimatedValue,
                Status = "Lost",
                RecordedBy = "System" // could be user info if available
            };

            await _repository.AddLossEventAsync(lossEvent);
        }

        /// <summary>
        /// Example of "Synchronous Core Domain Logic" for value calculation.
        /// </summary>
        private decimal CalculateEstimatedLoss(string entryPoint, LossReasonCode reason, string? specialization)
        {
            // Simple heuristics based on entry point
            return entryPoint switch
            {
                "Appointment" => 500m, // Avg Consult
                "Surgery" => 15000m,   // Avg Surgery
                "Admission" => 5000m,  // Avg Admission Deposit
                _ => 100m
            };
        }

        public async Task AnalyzeAndGenerateOpportunitiesAsync()
        {
            // Get recent losses (last 30 days)
            var stats = await _repository.GetLossStatsByReasonAsync(30);

            // Simple Rule Engine
            foreach (var stat in stats)
            {
                string reasonCodeStr = stat.Reason;
                if (!Enum.TryParse<LossReasonCode>(reasonCodeStr, out var reason)) continue;

                int count = stat.Count;
                decimal totalValue = stat.TotalValue;

                // Rule 1: High Demand for Missing Service
                if (reason == LossReasonCode.NO_SPECIALIST && count >= 5)
                {
                    await CreateOpportunityAsync("Specialist", "Required Specialist", count, totalValue);
                }
                // Rule 2: OT Bottleneck
                else if (reason == LossReasonCode.NO_OT_AVAILABLE && count >= 3)
                {
                    await CreateOpportunityAsync("Equipment", "Additional OT Slots", count, totalValue);
                }
                // Rule 3: Missing Dept
                else if (reason == LossReasonCode.NO_DEPARTMENT && count >= 5)
                {
                    await CreateOpportunityAsync("Department", "New Department Needed", count, totalValue);
                }
                // Rule 4: High Value Leakage
                else if (totalValue > 50000)
                {
                    await CreateOpportunityAsync("Strategic", $"High Value Loss: {stat.Reason}", count, totalValue);
                }
            }
        }

        private async Task CreateOpportunityAsync(string type, string target, int count, decimal totalValue)
        {
            var monthly = totalValue; // assuming stats are 30 days
            var annual = monthly * 12;

            var opp = new HospitalOpportunity
            {
                Type = type,
                Target = target,
                LossCount = count,
                EstimatedMonthlyRevenue = monthly,
                EstimatedAnnualRevenue = annual,
                ConfidenceScore = 85.0m, // Simplified confidence
                Status = "New",
                GeneratedAt = DateTime.Now
            };

            await _repository.AddOpportunityAsync(opp);
        }

        public async Task<List<HospitalOpportunity>> GetOpportunitiesAsync()
        {
            return await _repository.GetActiveOpportunitiesAsync();
        }

        public async Task<List<LossStat>> GetLossAnalyticsAsync()
        {
            var raw = await _repository.GetLossStatsByReasonAsync(30);

            // Format Enums to Readable Strings
            foreach (var stat in raw)
            {
                stat.Reason = stat.Reason.Replace("_", " ").ToLower();
                // Title Case: "no specialist" -> "No Specialist"
                stat.Reason = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(stat.Reason);
            }

            return raw;
        }

        public async Task<List<PatientLossEvent>> GetRecentLossesAsync(int days = 30)
        {
            return await _repository.GetRecentLossesAsync(days);
        }

        public async Task SeedSampleDataAsync()
        {
            var random = new Random();
            var reasons = new[] {
                LossReasonCode.NO_SPECIALIST, LossReasonCode.NO_SPECIALIST, LossReasonCode.NO_SPECIALIST,
                LossReasonCode.NO_BEDS, LossReasonCode.NO_BEDS,
                LossReasonCode.NO_OT_AVAILABLE,
                LossReasonCode.PRICE_TOO_HIGH
            };

            var specialties = new[] { "Neurology", "Neurology", "Oncology", "Cardiology" };

            // Generate 50 random loss events over the last 30 days
            for (int i = 0; i < 50; i++)
            {
                var reason = reasons[random.Next(reasons.Length)];
                var daysAgo = random.Next(0, 30);
                var spec = reason == LossReasonCode.NO_SPECIALIST ? specialties[random.Next(specialties.Length)] : null;
                var val = reason == LossReasonCode.NO_SPECIALIST ? 1200 : (reason == LossReasonCode.NO_BEDS ? 5000 : 800);

                var evt = new PatientLossEvent
                {
                    AttemptedAt = DateTime.Now.AddDays(-daysAgo),
                    EntryPoint = "Appointment",
                    LossReasonCode = reason,
                    LossReasonDetail = "Simulated Loss",
                    RequestedSpecialization = spec,
                    EstimatedValue = val,
                    Status = "Lost",
                    RecordedBy = "Seed"
                };

                await _repository.AddLossEventAsync(evt);
            }
        }
    }
}
