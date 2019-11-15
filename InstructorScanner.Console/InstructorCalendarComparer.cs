using System;
using System.Collections.Generic;
using System.Linq;

namespace InstructorScanner.ConsoleApp
{
    static class InstructorCalendarComparer
    {
        public static List<string> Compare(InstructorCalendar oldValue, InstructorCalendar newValue)
        {
            if (oldValue == null) throw new ArgumentNullException("oldValue");
            if (newValue == null) throw new ArgumentNullException("newValue");

            if (oldValue.Name != newValue.Name) throw new Exception($"Old value instructor name '{oldValue.Name}' does not match new instructor name '{newValue.Name}'");

            var messages = new List<string>();

            //compare matching dates, capture new bookings
            var maxOldCalDayDate = DateTime.MinValue;
            foreach(var oldCalDay in oldValue.CalendarDays)
            {
                var newCalDay = newValue.CalendarDays.SingleOrDefault(cd => cd.Date == oldCalDay.Date) ?? new CalendarDay { Date = oldCalDay.Date, Slots = new List<Slot>() };
                messages.AddRange(CompareCalDays(oldCalDay, newCalDay));

                if(oldCalDay.Date > maxOldCalDayDate)
                {
                    maxOldCalDayDate = oldCalDay.Date;
                }
            }

            // capture new free bookings


            return messages;
        }

        private static IEnumerable<string> CompareCalDays(CalendarDay oldCalDay, CalendarDay newCalDay)
        {
            throw new NotImplementedException();
        }
    }
}
