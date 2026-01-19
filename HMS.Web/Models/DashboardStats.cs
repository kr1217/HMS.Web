/*
 * FILE: DashboardStats.cs
 * PURPOSE: Data model for enterprise-grade operational dashboard metrics.
 */
using System;

namespace HMS.Web.Models
{
    /// <summary>
    /// Aggregated operational metrics for real-time hospital management.
    /// DESIGN: [Operational KPI Engine] Centralizes state for critical hospital workflows.
    /// </summary>
    public class DashboardStats
    {
        // --- Core Financials ---
        public decimal TodayRevenue { get; set; }
        public decimal CashRevenueToday { get; set; }
        public decimal DigitalRevenueToday { get; set; }

        // --- Capacity / Utilization ---
        public int OccupiedBeds { get; set; }
        public int TotalBeds { get; set; }
        public decimal BedOccupancyRate => TotalBeds > 0 ? (decimal)OccupiedBeds / TotalBeds * 100 : 0;

        public int StaffOnShift { get; set; }
        public int TotalStaff { get; set; }
        public decimal StaffUtilizationRate => TotalStaff > 0 ? (decimal)StaffOnShift / TotalStaff * 100 : 0;

        public int OccupiedTheaters { get; set; }
        public int TotalTheaters { get; set; }
        public decimal OTUtilizationRate => TotalTheaters > 0 ? (decimal)OccupiedTheaters / TotalTheaters * 100 : 0;

        // --- Patient Flow (Queues) ---
        public int AdmissionQueueCount { get; set; }
        public int PostOpTransferCount { get; set; }
        public int DischargeReadyCount { get; set; }
        public int PendingOperationAuthorizations { get; set; }

        // --- Exceptions & Alerts ---
        public int SurgeriesToday { get; set; }
        public int ExtendedSurgeryCount { get; set; }
        public int CriticalInventoryAlerts { get; set; }
        public int RecentLossEventsCount { get; set; }
        public int BedBlockagesCount { get; set; } // Beds marked as cleaning or out-of-order

        // --- Performance Analytics ---
        public int AvgPatientWaitTimeMinutes { get; set; }
        public decimal FinancialThroughputPerHour => TodayRevenue / Math.Max(1, DateTime.Now.Hour);
    }
}
