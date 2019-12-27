using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SendGrid;
using System;
using System.Threading.Tasks;

namespace InstructorScanner.FunctionApp
{
    public class ScheduledInstructorScan
    {
        private readonly IOptions<AppSettings> _appSettings;
        private readonly ICalendarDayListBuilder _calendarDayListBuilder;
        private readonly ICalendarDaysPersistanceService _calendarDaysPersistanceService;
        private readonly ISendEmailService _sendEmailService;

        public ScheduledInstructorScan(
            IOptions<AppSettings> appSettings,
            ICalendarDayListBuilder calendarDayListBuilder,
            ICalendarDaysPersistanceService calendarDaysPersistanceService,
            ISendEmailService sendEmailService
        )
        {
            _appSettings = appSettings;
            _calendarDayListBuilder = calendarDayListBuilder;
            _calendarDaysPersistanceService = calendarDaysPersistanceService;
            _sendEmailService = sendEmailService;
        }

        [FunctionName(nameof(ScheduledInstructorScan))]
        public async Task Run([TimerTrigger("0 5 8,12,16,20 * * *")]TimerInfo myTimer, ILogger logger)
        //public async Task Run([TimerTrigger("0 57 00 23 12 *")]TimerInfo myTimer, ILogger logger)
        {
            logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            var previousCalendarDays = await _calendarDaysPersistanceService.Retrieve();

            var newCalendarDays = await _calendarDayListBuilder.BuildAsync();
            var calendarChanges = CalendarDaysComparer.Compare(previousCalendarDays, newCalendarDays);

            await _calendarDaysPersistanceService.Store(newCalendarDays);

            logger.LogInformation($"{calendarChanges.Count} calendar changes found.");
            if (calendarChanges.Count > 0)
            {
                logger.LogInformation("New changes found, sending an email.");
                await _sendEmailService.SendEmailAsync("FI Booking Scan Results", calendarChanges, MimeType.Html);
            }
        }
    }
}
