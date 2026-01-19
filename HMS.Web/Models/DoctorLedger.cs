using System;

namespace HMS.Web.Models
{
    public class DoctorLedger
    {
        public int LedgerId { get; set; }
        public int DoctorId { get; set; }
        public string DoctorName { get; set; } = string.Empty; // For Display Join
        public DateTime TransactionDate { get; set; } = DateTime.Now;
        public string TransactionType { get; set; } = "Consultation"; // Consultation, Surgery, Recommendation
        public int ReferenceId { get; set; } // AppointmentId or OperationId
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending"; // Pending, Approved, Paid
        public DateTime? SettledDate { get; set; }
        public bool IsBlocked { get; set; }
        public string? BlockReason { get; set; }
    }
}
