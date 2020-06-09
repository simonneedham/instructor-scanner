using System;
using System.Collections.Generic;

namespace InstructorScanner.FunctionApp
{
    public class CalendarDay
    {
        public string Id { get; set; }

        public DateTime Date { get; set; }

        public List<InstructorSlots> InstructorSlots { get; set; }

        public string FlyingClub { get; set; } = "FlyingClub";

        public int Ttl { get; set; }

        public void SetDate(DateTime date)
        {
            Date = date;
            Id = date.ToString("yyyyMMdd");
            Ttl= (date - DateTime.Today).Days * 24 * 60 * 60;
        }
    }
}
