/*
 * FILE: DoctorPayment.cs
 * PURPOSE: Data model representing the DoctorPayment entity.
 * COMMUNICATES WITH: DoctorPaymentRepository via DatabaseHelper
 */
using System;

namespace HMS.Web.Models
{
    public class DoctorPayment
    {
        public int PaymentId { get; set; }
        public int DoctorId { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; } = DateTime.Now;
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public string Status { get; set; } = "Processed";
        public string? Notes { get; set; }

        // Optional Joined Properties
        public string? DoctorName { get; set; }
    }
}

