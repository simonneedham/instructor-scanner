using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InstructorScanner.FunctionApp
{
    public class ScheduleInstructorScanStatusCheck
    {
        private readonly IOptions<AppSettings> _appSettings;
        private readonly ISendEmailService _sendEmailService;
        private readonly IPreviousInstructorCalendarService _previousInstructorCalendarService;

        public ScheduleInstructorScanStatusCheck(
            IOptions<AppSettings> appSettings,
            ISendEmailService sendEmailService,
            IPreviousInstructorCalendarService previousInstructorCalendarService
        )
        {
            _appSettings = appSettings;
            _sendEmailService = sendEmailService;
            _previousInstructorCalendarService = previousInstructorCalendarService;
        }

        [FunctionName(nameof(ScheduleInstructorScanStatusCheck))]
        public async Task Run(
            [TimerTrigger("0 05 06 * * *")]TimerInfo myTimer,
            ILogger logger
        )
        {
            var emailContent = new List<string>();
            var previousInstructorCalendar = await _previousInstructorCalendarService.Retrieve();

            if(previousInstructorCalendar == null)
            {
                emailContent.Add("Unable to retrieve a previous instructor calendar.");
            }
            else
            {
                var freeBookingCount = previousInstructorCalendar
                    .CalendarDays
                    .SelectMany(cd => cd.Slots)
                    .Where(s => s.Availability == AvailabilityNames.Free)
                    .ToList()
                    .Count;

                emailContent.Add($"There are currently {freeBookingCount} free slots being tracked.");
            }


            await _sendEmailService.SendEmailAsync("Instructor Scan Status", emailContent, MimeType.Text);
        }
    }
}
