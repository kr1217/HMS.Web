/*
 * FILE: Payment.cs
 * PURPOSE: Data model representing the Payment entity.
 * COMMUNICATES WITH: PaymentRepository via DatabaseHelper
 */
using System;

namespace HMS.Web.Models
{
    public class Payment
    {
        public int PaymentId { get; set; }
        public int BillId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty; // Cash, Card, BankTransfer
        public DateTime PaymentDate { get; set; } = DateTime.Now;
        public string? ReferenceNumber { get; set; }
        public string TellerId { get; set; } = string.Empty;
        public int ShiftId { get; set; }
        public string? Remarks { get; set; }
    }
}

