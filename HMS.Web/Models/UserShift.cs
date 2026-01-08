/*
 * FILE: UserShift.cs
 * PURPOSE: Data model representing the UserShift entity.
 * COMMUNICATES WITH: UserShiftRepository via DatabaseHelper
 */
using System;

namespace HMS.Web.Models
{
    /// <summary>
    /// Tracks cash shifts for hospital tellers.
    /// OPTIMIZATION: [Financial Audit] Enforces cash balance reconciliation at shift-end.
    /// </summary>
    public class UserShift
    {
        public int ShiftId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public DateTime StartTime { get; set; } = DateTime.Now;
        public DateTime? EndTime { get; set; }
        public decimal StartingCash { get; set; }
        public decimal? EndingCash { get; set; }
        public decimal? ActualCash { get; set; }
        public string Status { get; set; } = "Open";
        public string? Notes { get; set; }

        // Optional Joined Properties
        public string? TellerName { get; set; }
        public int? EmployeeId { get; set; }
    }
}

