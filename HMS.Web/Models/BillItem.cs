/*
 * FILE: BillItem.cs
 * PURPOSE: Data model representing the BillItem entity.
 * COMMUNICATES WITH: BillItemRepository via DatabaseHelper
 */
using System;

namespace HMS.Web.Models
{
    public class BillItem
    {
        public int BillItemId { get; set; }
        public int BillId { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Category { get; set; } = "General"; // Room, Doctor, Medicine, Lab, etc.
    }
}

