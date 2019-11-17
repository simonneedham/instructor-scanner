using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using InstructorScanner.Core;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace InstructorScanner.FunctionApp
{
    public class ScheduledInstructorScan
    {
        private readonly IOptions<AppSettings> _appSettings;
        private readonly IStorageHelper _storageHelper;

        public ScheduledInstructorScan(
            IOptions<AppSettings> appSettings,
            IStorageHelper storageHelper
        )
        {
            _appSettings = appSettings;
            _storageHelper = storageHelper;
        }

        [FunctionName(nameof(ScheduledInstructorScan))]
        //public async Task Run([TimerTrigger("0 5 8,12,16,20 * * *")]TimerInfo myTimer, ILogger logger)
        public async Task Run([TimerTrigger("0 08 20 17 11 *")]TimerInfo myTimer, ILogger logger)
        {
            logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");


            InstructorCalendar oldInstructorCalendar;
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

            var icb = new InstructorCalendarBuilder(_appSettings.Value, logger);
            var newInstructorCalendar = await icb.BuildInstructorAsync(_appSettings.Value.InstructorName);

            var calendarChanges = InstructorCalendarComparer.Compare(oldInstructorCalendar, newInstructorCalendar);


            var newInstructorCalendarText = JsonConvert.SerializeObject(newInstructorCalendar);
            await _storageHelper.SaveFileAsync("instructor-scan", "instructor-bookings.json", newInstructorCalendarText);

            foreach (var msg in calendarChanges)
            {
                logger.LogInformation(msg);
            }

        }
    }
}
