/*
 * FILE: Notification.cs
 * PURPOSE: Data model representing the Notification entity.
 * COMMUNICATES WITH: NotificationRepository via DatabaseHelper
 */
using System;

namespace HMS.Web.Models
{
    public class Notification
    {
        public int NotificationId { get; set; }
        public int? PatientId { get; set; }
        public int? DoctorId { get; set; }
        public string? TargetRole { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public bool IsRead { get; set; } = false;
    }
}

