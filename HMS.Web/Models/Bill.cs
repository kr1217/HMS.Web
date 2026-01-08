/*
 * FILE: Bill.cs
 * PURPOSE: Data model representing the Bill entity.
 * COMMUNICATES WITH: BillRepository via DatabaseHelper
 */
using System;
using System.Collections.Generic;

namespace HMS.Web.Models
{
    /// <summary>
    /// Financial ledger for a patient visit or admission.
    /// OPTIMIZATION: [Calculated Truth] TotalAmount and DueAmount are derived from BillItems at runtime.
    /// </summary>
    public class Bill
    {
        public int BillId { get; set; }
        public int PatientId { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal DueAmount { get; set; }
        public string Status { get; set; } = "Pending"; // Paid, Pending, Partial
        public DateTime BillDate { get; set; }
        public int? ShiftId { get; set; }
        public string? CreatedBy { get; set; }

        // Optional Joined Properties
        public string? PatientName { get; set; }
        public int? AdmissionId { get; set; }
        public List<BillItem> Items { get; set; } = new List<BillItem>();
    }
}

