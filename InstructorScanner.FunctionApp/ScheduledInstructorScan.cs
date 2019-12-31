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
        private readonly IHtmlPageCreatorService _htmlPageCreatorService;
        private readonly ISendEmailService _sendEmailService;

        public ScheduledInstructorScan(
            IOptions<AppSettings> appSettings,
            ICalendarDayListBuilder calendarDayListBuilder,
            ICalendarDaysPersistanceService calendarDaysPersistanceService,
            IHtmlPageCreatorService htmlPageCreatorService,
            ISendEmailService sendEmailService
        )
        {
            _appSettings = appSettings;
            _calendarDayListBuilder = calendarDayListBuilder;
            _calendarDaysPersistanceService = calendarDaysPersistanceService;
            _htmlPageCreatorService = htmlPageCreatorService;
            _sendEmailService = sendEmailService;
        }

        [FunctionName(nameof(ScheduledInstructorScan))]
        public async Task Run([TimerTrigger("0 5 8,12,16,20 * * *")]TimerInfo myTimer, ILogger logger)
        //public async Task Run([TimerTrigger("0 57 00 23 12 *")]TimerInfo myTimer, ILogger logger)
        {
            var instructorCount = _appSettings.Value.Instructors.Count;
            logger.LogInformation($"Initiating scan for of {instructorCount} instructors for {_appSettings.Value.DaysToScan} days at {DateTime.Now: dd-MMM-yyy HH:mm:ss}");

            var previousCalendarDays = await _calendarDaysPersistanceService.RetrieveAsync();

            var newCalendarDays = await _calendarDayListBuilder.BuildAsync();
            var calendarChanges = CalendarDaysComparer.Compare(previousCalendarDays, newCalendarDays);

            await _calendarDaysPersistanceService.StoreAsync(newCalendarDays);
            await _htmlPageCreatorService.CreateHtmlPageAsync(newCalendarDays);

            logger.LogInformation($"{calendarChanges.Count} calendar changes found.");
            if (calendarChanges.Count > (instructorCount*2))
            {
                logger.LogInformation("New changes found, sending an email.");

                calendarChanges.Add(string.Empty);
                calendarChanges.Add($"Slot summary: {_appSettings.Value.WebRootUrl}");

                await _sendEmailService.SendEmailAsync("FI Booking Scan Results", calendarChanges, MimeType.Html);
            }
            else
            {
                logger.LogInformation($"Not sending an email as {calendarChanges.Count} calender changes line count is less than or equal to the minimum of {instructorCount * 2}");
            }
        }
    }
}
