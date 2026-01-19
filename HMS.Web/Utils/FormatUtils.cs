using System;
using System.Globalization;

namespace HMS.Web.Utils
{
    public static class FormatUtils
    {
        public static string FormatCurrencyCompact(object value)
        {
            if (value is not double && value is not decimal && value is not int && value is not long)
            {
                return value?.ToString() ?? "";
            }

            double d = Convert.ToDouble(value);

            if (d >= 1000000) return (d / 1000000).ToString("C1", CultureInfo.CurrentCulture) + "M";
            if (d >= 1000) return (d / 1000).ToString("C0", CultureInfo.CurrentCulture) + "k";
            return d.ToString("C0", CultureInfo.CurrentCulture);
        }
    }
}
