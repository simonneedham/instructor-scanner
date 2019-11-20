using System;
using System.Collections.Generic;

namespace InstructorScanner.FunctionApp
{
    public class CalendarDay
    {
        public DateTime Date { get; set; }
        public List<Slot> Slots { get; set; }
    }
}
