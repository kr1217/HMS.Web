/*
 * FILE: DoctorDashboardStats.cs
 * PURPOSE: Data model representing the DoctorDashboardStats entity.
 * COMMUNICATES WITH: DoctorDashboardStatsRepository via DatabaseHelper
 */
using System;

namespace HMS.Web.Models
{
    public class DoctorDashboardStats
    {
        public int AppointmentsToday { get; set; }
        public int PendingApprovals { get; set; }
        public int TotalPatientsServed { get; set; }
        public decimal MonthlyCommission { get; set; }
        public int PendingReports { get; set; }
    }
}

