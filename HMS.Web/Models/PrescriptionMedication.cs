/*
 * FILE: PrescriptionMedication.cs
 * PURPOSE: Data model representing the PrescriptionMedication entity.
 * COMMUNICATES WITH: PrescriptionMedicationRepository via DatabaseHelper
 */
using System;

namespace HMS.Web.Models
{
    public class PrescriptionMedication
    {
        public string Name { get; set; } = "";
        public string Dosage { get; set; } = "";
        public string Frequency { get; set; } = "";
        public string Duration { get; set; } = "";
        public string Instructions { get; set; } = "";
    }
}

