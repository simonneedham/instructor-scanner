using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SendGrid;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InstructorScanner.FunctionApp
{
    public class ScheduleInstructorScanStatusCheck
    {
        private readonly IOptions<AppSettings> _appSettings;
        private readonly ISendEmailService _sendEmailService;
        private readonly ICalendarDaysPersistanceService _calendarDayPersistanceService;

        public ScheduleInstructorScanStatusCheck(
            IOptions<AppSettings> appSettings,
            ISendEmailService sendEmailService,
            ICalendarDaysPersistanceService calendarDayPersistanceService
        )
        {
            _appSettings = appSettings;
            _sendEmailService = sendEmailService;
            _calendarDayPersistanceService = calendarDayPersistanceService;
        }

        [FunctionName(nameof(ScheduleInstructorScanStatusCheck))]
        public async Task Run(
            [TimerTrigger("0 05 06 * * *")]TimerInfo myTimer,
            ILogger logger
        )
        {
            var emailContent = new List<string>();
            var previousCalendarDays = await _calendarDayPersistanceService.RetrieveAsync();

            if(previousCalendarDays == null)
            {
                emailContent.Add("Unable to retrieve previous calendar days.");
            }
            else
            {
                emailContent.Add("Currently tracking the following instructors/slots:");
                emailContent.Add(string.Empty);

                var distinctIntials = previousCalendarDays
                    .SelectMany(cd => cd.InstructorSlots)
                    .Select(iSlots => iSlots.InstructorInitials)
                    .Distinct()
                    .ToList();

                foreach(var initial in distinctIntials)
                {
                    var freeSlotCount = previousCalendarDays
                        .SelectMany(cd => cd.InstructorSlots)
                        .GroupBy(instructorSlots => instructorSlots.InstructorInitials)
                        .Where(grp => grp.Key == initial)
                        .Select(grp => grp.Sum(iSlots => iSlots.Slots.Where(s => s.Availability == AvailabilityNames.Free).ToList().Count))
                        .Sum();

                    emailContent.Add($"    {initial}: {freeSlotCount} slots");
                }
            }

            emailContent.Add(string.Empty);
            emailContent.Add($"Slot summary: {_appSettings.Value.WebRootUrl}");

            await _sendEmailService.SendEmailAsync("Instructor Scan Status", emailContent, MimeType.Text);
        }
    }
}
