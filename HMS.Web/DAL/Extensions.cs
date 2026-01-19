using System;
using Microsoft.Data.SqlClient;

namespace HMS.Web.DAL
{
    /// <summary>
    /// Helper extension to check for column existence in SqlDataReader.
    /// </summary>
    public static class SqlDataReaderExtensions
    {
        public static bool HasColumn(this SqlDataReader reader, string columnName)
        {
            for (int i = 0; i < reader.FieldCount; i++)
            {
                if (reader.GetName(i).Equals(columnName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
    }
}
