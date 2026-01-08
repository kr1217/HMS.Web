/*
 * FILE: Room.cs
 * PURPOSE: Data model representing the Room entity.
 * COMMUNICATES WITH: RoomRepository via DatabaseHelper
 */
using System;

namespace HMS.Web.Models
{
    public class Room
    {
        public int RoomId { get; set; }
        public int WardId { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public int RoomTypeId { get; set; }
        public bool IsActive { get; set; } = true;

        // Optional Joined Properties
        public string? WardName { get; set; }
        public string? RoomTypeName { get; set; }
    }
}

