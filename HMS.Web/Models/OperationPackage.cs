/*
 * FILE: OperationPackage.cs
 * PURPOSE: Data model representing the OperationPackage entity.
 * COMMUNICATES WITH: OperationPackageRepository via DatabaseHelper
 */
using System;

namespace HMS.Web.Models
{
    public class OperationPackage
    {
        public int PackageId { get; set; }
        public string PackageName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Cost { get; set; }
    }
}

