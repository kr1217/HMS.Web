/*
 * FILE: RoomType.cs
 * PURPOSE: Data model representing the RoomType entity.
 * COMMUNICATES WITH: RoomTypeRepository via DatabaseHelper
 */
using System;
using System.ComponentModel.DataAnnotations;

namespace HMS.Web.Models
{
    public class RoomType
    {
        public int RoomTypeId { get; set; }
        [Required]
        public string TypeName { get; set; } = string.Empty; // General, Private, Deluxe
        public decimal DailyRate { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}

