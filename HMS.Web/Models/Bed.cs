/*
 * FILE: Bed.cs
 * PURPOSE: Data model representing the Bed entity.
 * COMMUNICATES WITH: BedRepository via DatabaseHelper
 */
using System;

namespace HMS.Web.Models
{
    /// <summary>
    /// Represents a hospital bed. 
    /// OPTIMIZATION: [Resource Tracking] Status tracks occupancy and maintenance in real-time.
    /// </summary>
    public class Bed
    {
        public int BedId { get; set; }
        public int RoomId { get; set; }
        public string BedNumber { get; set; } = string.Empty; // e.g. "ICU-101-A"
        public string Status { get; set; } = "Available"; // Available, Occupied, Maintenance, Cleaning
        public bool IsActive { get; set; } = true;

        // Optional Joined Properties
        public string? RoomNumber { get; set; }
        public string? WardName { get; set; }
    }
}

