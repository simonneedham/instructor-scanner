using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using InstructorScanner.FunctionApp;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SendGrid;

namespace InstructorScanner.FunctionApp
{
    public class ScheduledInstructorScan
    {
        private readonly IOptions<AppSettings> _appSettings;
        private readonly IPreviousInstructorCalendarService _previousInstructorCalendarService;
        private readonly ISendEmailService _sendEmailService;
        private readonly IStorageHelper _storageHelper;

        public ScheduledInstructorScan(
            IOptions<AppSettings> appSettings,
            IPreviousInstructorCalendarService previousInstructorCalendarService,
            ISendEmailService sendEmailService,
            IStorageHelper storageHelper
        )
        {
            _appSettings = appSettings;
            _previousInstructorCalendarService = previousInstructorCalendarService;
            _sendEmailService = sendEmailService;
            _storageHelper = storageHelper;
        }

        [FunctionName(nameof(ScheduledInstructorScan))]
        public async Task Run([TimerTrigger("0 5 8,12,16,20 * * *")]TimerInfo myTimer, ILogger logger)
        //public async Task Run([TimerTrigger("0 10 00 20 11 *")]TimerInfo myTimer, ILogger logger)
        {
            logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            var previousInstructorCalendar = await _previousInstructorCalendarService.Retrieve();

            var icb = new InstructorCalendarBuilder(_appSettings, logger);
            var newInstructorCalendar = await icb.BuildInstructorAsync(_appSettings.Value.InstructorName);

            var calendarChanges = InstructorCalendarComparer.Compare(previousInstructorCalendar, newInstructorCalendar);

            var newInstructorCalendarText = JsonConvert.SerializeObject(newInstructorCalendar);
            await _storageHelper.SaveFileAsync("instructor-scan", "instructor-bookings.json", newInstructorCalendarText);

            logger.LogInformation($"{calendarChanges.Count} calendar changes found.");
            if (calendarChanges.Count > 0)
            {
                logger.LogInformation("New changes found, sending an email.");
                await _sendEmailService.SendEmailAsync("CFI Booking Scan Results", calendarChanges, MimeType.Text);
            }
        }
    }
}
