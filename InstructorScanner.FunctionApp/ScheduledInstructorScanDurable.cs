using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SendGrid;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace InstructorScanner.FunctionApp
{
    public class ScheduledInstructorScanDurable
    {
        private readonly IOptions<AppSettings> _appSettings;
        private readonly ICalendarDaysPersistanceService _calendarDaysPersistanceService;
        private readonly IHtmlPageCreatorService _htmlPageCreatorService;
        private readonly ISendEmailService _sendEmailService;

        public ScheduledInstructorScanDurable(
            IOptions<AppSettings> appSettings,
            ICalendarDaysPersistanceService calendarDaysPersistanceService,
            IHtmlPageCreatorService htmlPageCreatorService,
            ISendEmailService sendEmailService
        )
        {
            _appSettings = appSettings;
            _calendarDaysPersistanceService = calendarDaysPersistanceService;
            _htmlPageCreatorService = htmlPageCreatorService;
            _sendEmailService = sendEmailService;
        }

        [FunctionName("ScheduledInstructorScanDurableTimerTrigger")]
        public static async Task TimerTrigger(
            [TimerTrigger("0 5 8 * * *", RunOnStartup = true)]TimerInfo myTimer,
            //[TimerTrigger("0 5 11,15,19 * * *", RunOnStartup = true)]TimerInfo myTimer,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger logger
            )
        {
            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync("ScheduledInstructorScanDurable", null);

            logger.LogInformation($"Started {nameof(ScheduledInstructorScanDurable)} orchestration with ID = '{instanceId}'.");
        }

        [FunctionName("ScheduledInstructorScanDurable")]
        public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context, ILogger logger, CancellationToken cancellationToken)
        {
            var instructorCount = _appSettings.Value.Instructors.Count;
            var started = context.CurrentUtcDateTime;

            if (!context.IsReplaying)
                logger.LogInformation($"Initiating scan of {instructorCount} instructors for {_appSettings.Value.DaysToScan} days at {context.CurrentUtcDateTime: dd-MMM-yyy HH:mm:ss}");

            // get the previous calendar days
            var previousCalendarDays = await context.CallActivityAsync<List<CalendarDay>>("ScheduledInstructorScanDurableGetPreviousCalendarDays", new Object());

            // build a list of the days to scan
            var allDatesToScan = Enumerable
                .Range(1, _appSettings.Value.DaysToScan)
                .Select(offset => DateTime.Today.AddDays(offset))
                .ToList();

            // calculate the number of dates that can be scanned in a 10 minute activity
            var maxTimePerActivity = 570m; //seconds (9.5 minutes)
            var scanTime = 10m + (decimal)_appSettings.Value.DelaySecondsPerScan; //10 seconds for page scan + delay time
            var noOfDatesPerActivity = (int)Math.Round(maxTimePerActivity / scanTime, 0);
            noOfDatesPerActivity = noOfDatesPerActivity == 0 ? 1 : noOfDatesPerActivity;


            // scan all the dates
            var newCalendarDays = new List<CalendarDay>();
            foreach(var activityDatesChunk in Chunk(allDatesToScan, noOfDatesPerActivity))
            {
                newCalendarDays.AddRange(await context.CallActivityAsync<List<CalendarDay>>("ScheduledInstructorScanDurableInstructorScan", activityDatesChunk));
            }

            // determine the calendar changes
            var calendarChanges = CalendarDaysComparer.Compare(previousCalendarDays, newCalendarDays);

            // save the new calendar scan results
            await context.CallActivityAsync<List<CalendarDay>>("ScheduledInstructorScanDurableStoreCalendarDays", newCalendarDays);
            await context.CallActivityAsync<List<CalendarDay>>("ScheduledInstructorScanDurableCreateHtmlPage", newCalendarDays);

            // send an email if necessary
            logger.LogInformation($"{calendarChanges.Count} calendar changes found.");
            if (calendarChanges.Count > (instructorCount * 2))
            {
                logger.LogInformation("New changes found, sending an email.");

                calendarChanges.Add(string.Empty);
                calendarChanges.Add($"Slot summary: {_appSettings.Value.WebRootUrl}");

                await context.CallActivityAsync<List<CalendarDay>>("ScheduledInstructorScanDurableCreateHtmlPage", new EmailOptions { Subject = "FI Booking Scan Results", MessagBody = calendarChanges });
            }
            else
            {
                logger.LogInformation($"Not sending an email as {calendarChanges.Count} calendar changes line count is less than or equal to the minimum of {instructorCount * 2}");
            }

            // finish
            var stopped = context.CurrentUtcDateTime;
            var orchestratorRunTime = stopped - started;
            logger.LogInformation($"Scan completed after {orchestratorRunTime.Minutes}m {orchestratorRunTime.Seconds}s.");
        }

        [FunctionName("ScheduledInstructorScanDurableInstructorScan")]
        public async Task<List<CalendarDay>> InstructorScan([ActivityTrigger] DateTime[] datesToScan, ILogger logger, CancellationToken cancellationToken)
        {
            var calendarDays = new List<CalendarDay>();

            using (var bpp = new BookingPageParser(_appSettings, logger))
            {
                foreach(var scanDate in datesToScan)
                {
                    logger.LogInformation($"Parsing bookings page for {scanDate:dd/MM/yyyy}");

                    try
                    {
                        var calDay = await bpp.GetBookings(scanDate, _appSettings.Value.Instructors);
                        calendarDays.Add(calDay);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, $"Failed to parse {scanDate:dd/MM/yyyy}.");
                    }

                    Task.Delay(_appSettings.Value.DelaySecondsPerScan * 1000).Wait();
                }
            }

            return calendarDays;
        }

        [FunctionName("ScheduledInstructorScanDurableGetPreviousCalendarDays")]
        public async Task<List<CalendarDay>> GetPreviousCalendarDays([ActivityTrigger] object obj, ILogger logger, CancellationToken cancellationToken)
        {
            var previousCalendarDays = await _calendarDaysPersistanceService.RetrieveAsync(cancellationToken);
            return previousCalendarDays;
        }

        [FunctionName("ScheduledInstructorScanDurableStoreCalendarDays")]
        public async Task StoreCalendarDays([ActivityTrigger] List<CalendarDay> calendarDays, ILogger logger, CancellationToken cancellationToken)
        {
            await _calendarDaysPersistanceService.StoreAsync(calendarDays, cancellationToken);
        }

        [FunctionName("ScheduledInstructorScanDurableCreateHtmlPage")]
        public async Task CreateHtmlPage([ActivityTrigger] List<CalendarDay> calendarDays, ILogger logger, CancellationToken cancellationToken)
        {
            await _htmlPageCreatorService.CreateHtmlPageAsync(calendarDays, cancellationToken);
        }

        [FunctionName("ScheduledInstructorScanDurableSendEmail")]
        public async Task SendEmail([ActivityTrigger] EmailOptions emailOptions, ILogger logger, CancellationToken cancellationToken)
        {
            await _sendEmailService.SendEmailAsync(emailOptions.Subject, emailOptions.MessagBody, MimeType.Html, cancellationToken);
        }

        private static IEnumerable<IEnumerable<T>> Chunk<T>(IEnumerable<T> source, int chunkSize)
        {
            var pos = 0;
            while (source.Skip(pos).Any())
            {
                yield return source.Skip(pos).Take(chunkSize);
                pos += chunkSize;
            }
        }

        public class EmailOptions
        {
            public string Subject { get; set; }
            public IList<string> MessagBody { get; set; }
        }
    }
}