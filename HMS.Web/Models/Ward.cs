/*
 * FILE: Ward.cs
 * PURPOSE: Data model representing the Ward entity.
 * COMMUNICATES WITH: WardRepository via DatabaseHelper
 */
using System;
using System.ComponentModel.DataAnnotations;

namespace HMS.Web.Models
{
    public class Ward
    {
        public int WardId { get; set; }
        [Required]
        public string WardName { get; set; } = string.Empty;
        public string Floor { get; set; } = string.Empty;
        public string Wing { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }
}

