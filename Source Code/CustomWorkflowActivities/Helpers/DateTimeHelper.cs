using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NGN.Helpers
{
    public static class DateTimeHelper
    {
        public static bool IsWeekend(this DateTime input)
        {
            return (input.DayOfWeek == DayOfWeek.Saturday || input.DayOfWeek == DayOfWeek.Sunday);
        }

        public static bool IsHoliday(this DateTime dateToCheck, EntityCollection calendarRules)
        {
            bool isHoliday = false;

            foreach (var calendarRule in calendarRules.Entities)
            {
                // Date is not stored as UTC
                var startTime = calendarRule.GetAttributeValue<DateTime>("starttime");
                // Subtract 1 so the last minute is not double counted 
                // 4/2/2018 12:00 AM + 1440 minutes = 4/3/2018 12:00 AM - so the last minute is handled twice
                var duration = calendarRule.GetAttributeValue<int>("duration") - 1;
                var endTime = startTime.AddMinutes(duration);

                if (dateToCheck >= startTime && dateToCheck <= endTime)
                {
                    isHoliday = true;
                    break;
                }

            }

            return isHoliday;
        }
    }
}
