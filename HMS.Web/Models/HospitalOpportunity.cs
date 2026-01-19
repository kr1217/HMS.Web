using System;

namespace HMS.Web.Models
{
    /// <summary>
    /// Represents an automatically detected business opportunity based on patient loss data.
    /// </summary>
    public class HospitalOpportunity
    {
        public int OpportunityId { get; set; }
        public string Type { get; set; } = string.Empty; // Department, Specialist, Equipment
        public string Target { get; set; } = string.Empty; // e.g., "Plastic Surgery"
        public int LossCount { get; set; }
        public decimal EstimatedMonthlyRevenue { get; set; }
        public decimal EstimatedAnnualRevenue { get; set; }
        public decimal ConfidenceScore { get; set; }
        public string Status { get; set; } = "New"; // New, Reviewed, Approved, Rejected
        public DateTime GeneratedAt { get; set; } = DateTime.Now;
    }
}
