using System;
using System.Globalization;

namespace Flappr.Helpers
{
    public static class TimeHelper
    {
        public static string FormatTime(DateTime createdDate)
        {
            var now = DateTime.Now;
            var diff = now - createdDate;

            if (diff.TotalDays < 1)
            {
                int hours = (int)diff.TotalHours;
                if (hours < 1)
                {
                    int minutes = (int)diff.TotalMinutes;
                    return minutes <= 0 ? "şimdi" : $"{minutes} dk önce";
                }
                return $"{hours} s önce";
            }
            else if (diff.TotalDays < 7)
            {
                int days = (int)diff.TotalDays;
                return $"{days} g önce";
            }
            else
            {
                return createdDate.ToString("d", CultureInfo.GetCultureInfo("tr"));
            }
        }
    }
}
