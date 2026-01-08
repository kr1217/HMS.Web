/*
 * FILE: OperationTheater.cs
 * PURPOSE: Data model representing the OperationTheater entity.
 * COMMUNICATES WITH: OperationTheaterRepository via DatabaseHelper
 */
using System;
using System.ComponentModel.DataAnnotations;

namespace HMS.Web.Models
{
    public class OperationTheater
    {
        public int TheaterId { get; set; }
        [Required]
        public string TheaterName { get; set; } = string.Empty;
        public string Status { get; set; } = "Available"; // Available, Maintenance, InUse
        public bool IsActive { get; set; } = true;
    }
}

