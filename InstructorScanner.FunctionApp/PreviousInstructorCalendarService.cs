using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InstructorScanner.FunctionApp
{
    public interface IPreviousInstructorCalendarService
    {
        Task<InstructorCalendar> Retrieve();
    }

    public class PreviousInstructorCalendarService : IPreviousInstructorCalendarService
    {
        private readonly IOptions<AppSettings> _appSettings;
        private readonly IStorageHelper _storageHelper;

        public PreviousInstructorCalendarService(
            IOptions<AppSettings> appSettings,
            IStorageHelper storageHelper
        )
        {
            _appSettings = appSettings;
            _storageHelper = storageHelper;
        }

        public async Task<InstructorCalendar> Retrieve()
        {
            InstructorCalendar previousInstructorCalendar;
            if (await _storageHelper.FileExistsAsync("instructor-scan", "instructor-bookings.json"))
            {
                previousInstructorCalendar = JsonConvert.DeserializeObject<InstructorCalendar>(await _storageHelper.ReadFileAsync("instructor-scan", "instructor-bookings.json"));
            }
            else
            {
                previousInstructorCalendar = new InstructorCalendar
                {
                    CalendarDays = new List<CalendarDay>(),
                    Name = _appSettings.Value.InstructorName
                };
            }

            return previousInstructorCalendar;
        }
    }
}
