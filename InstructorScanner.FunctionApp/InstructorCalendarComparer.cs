using System;
using System.Collections.Generic;
using System.Linq;

namespace InstructorScanner.FunctionApp
{
    static public class InstructorCalendarComparer
    {
        public static List<string> Compare(InstructorCalendar oldValue, InstructorCalendar newValue)
        {
            if (oldValue == null) throw new ArgumentNullException("oldValue");
            if (newValue == null) throw new ArgumentNullException("newValue");

            if (oldValue.Name != newValue.Name) throw new Exception($"Old value instructor name '{oldValue.Name}' does not match new instructor name '{newValue.Name}'");

            var messages = new List<string>();

            //capture new bookings
            var maxOldCalDayDate = DateTime.MinValue;

            foreach (var newCalDay in newValue.CalendarDays)
            {
                if (newCalDay.Slots.Any(s => s.Availability == AvailabilityNames.Free))
                {
                    var oldCalDay = oldValue.CalendarDays.SingleOrDefault(cd => cd.Date == newCalDay.Date);
                    messages.AddRange(CompareCalDays(oldCalDay, newCalDay));
                }
            }

            return messages;
        }

        private static IList<string> CompareCalDays(CalendarDay oldCalDay, CalendarDay newCalDay)
        {
            if (newCalDay == null) throw new ArgumentNullException("newCalDay");
            if(oldCalDay == null)
            {
                oldCalDay = new CalendarDay { Date = newCalDay.Date, Slots = new List<Slot>() };
            }

            if (oldCalDay.Date != newCalDay.Date) throw new Exception($"Old cal day date '{oldCalDay.Date}' does not match new cal day date '{newCalDay.Date}'");

            var messages = new List<string>();

            var newFeeSlots = newCalDay.Slots.Where(s => s.Availability == AvailabilityNames.Free).ToList();
            foreach(var newFreeSlot in newFeeSlots)
            {
                var matchedOldLSlot = oldCalDay.Slots.SingleOrDefault(s => s.Time == newFreeSlot.Time);
                if(matchedOldLSlot == null || (matchedOldLSlot != null && matchedOldLSlot.Availability != newFreeSlot.Availability))
                {
                    messages.Add($"{newCalDay.Date:ddd dd-MMM} {newFreeSlot.Time} is available");
                }
            }

            return messages;
        }
    }
}
