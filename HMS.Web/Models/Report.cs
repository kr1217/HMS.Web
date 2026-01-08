/*
 * FILE: Report.cs
 * PURPOSE: Data model representing the Report entity.
 * COMMUNICATES WITH: ReportRepository via DatabaseHelper
 */
using System;

namespace HMS.Web.Models
{
    public class Report
    {
        public int ReportId { get; set; }
        public int PatientId { get; set; }
        public int DoctorId { get; set; }
        public int? AppointmentId { get; set; }
        public string ReportName { get; set; } = string.Empty;
        public string ReportType { get; set; } = string.Empty;
        public DateTime ReportDate { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public string Status { get; set; } = "Finalized"; // Finalized, Draft
        public string? Observations { get; set; }
    }
}

