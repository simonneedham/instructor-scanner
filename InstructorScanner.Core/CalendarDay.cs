using System;
using System.Collections.Generic;

namespace InstructorScanner.ConsoleApp
{
    public class CalendarDay
    {
        public DateTime Date { get; set; }
        public List<Slot> Slots { get; set; }
    }
}
