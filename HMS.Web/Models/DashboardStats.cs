/*
 * FILE: DashboardStats.cs
 * PURPOSE: Data model representing the DashboardStats entity.
 * COMMUNICATES WITH: DashboardStatsRepository via DatabaseHelper
 */
using System;

namespace HMS.Web.Models
{
    public class DashboardStats
    {
        public decimal TodayRevenue { get; set; }
        public int OccupiedBeds { get; set; }
        public int TotalBeds { get; set; }
        public int StaffOnShift { get; set; }
        public int SurgeriesToday { get; set; }
    }
}

