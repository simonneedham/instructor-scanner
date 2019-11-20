using Microsoft.Extensions.Options;
using System;

namespace InstructorScanner.Core
{
    public interface IPreviousInstructorCalendarService
    {
        InstructorCalendar Retrieve();
    }

    public class PreviousInstructorCalendarService : IPreviousInstructorCalendarService
    {
        public PreviousInstructorCalendarService(
            IOptions<AppSettings> appSettings,
            IStorageHelper storageHelper
        )
        {
        }

        public InstructorCalendar Retrieve()
        {
            if (await _storageHelper.FileExistsAsync("instructor-scan", "instructor-bookings.json"))
            {
                oldInstructorCalendar = JsonConvert.DeserializeObject<InstructorCalendar>(await _storageHelper.ReadFileAsync("instructor-scan", "instructor-bookings.json"));
            }
            else
            {
                oldInstructorCalendar = new InstructorCalendar
                {
                    CalendarDays = new List<CalendarDay>(),
                    Name = _appSettings.Value.InstructorName
                };
            }
        }
    }
}
