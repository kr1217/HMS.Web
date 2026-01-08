/*
 * FILE: SupportTicket.cs
 * PURPOSE: Data model representing the SupportTicket entity.
 * COMMUNICATES WITH: SupportTicketRepository via DatabaseHelper
 */
using System;
using System.ComponentModel.DataAnnotations;

namespace HMS.Web.Models
{
    public class SupportTicket
    {
        public int TicketId { get; set; }
        public int PatientId { get; set; }

        [Required(ErrorMessage = "Subject is required.")]
        [StringLength(200, ErrorMessage = "Subject cannot exceed 200 characters.")]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "Message is required.")]
        [StringLength(2000, ErrorMessage = "Message cannot exceed 2000 characters.")]
        public string Message { get; set; } = string.Empty;

        public string Status { get; set; } = "Open"; // Open, Closed, In Progress
        public DateTime CreatedDate { get; set; }
        public string? Response { get; set; }
    }
}

