using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InstructorScanner.FunctionApp
{
    public class InstructorCalendarBuilder
    {
        private readonly IOptions<AppSettings> _appSettings;
        private readonly ILogger _logger;

        public InstructorCalendarBuilder(
            IOptions<AppSettings> appSettings,
            ILogger logger
        )
        {
            _appSettings = appSettings;
            _logger = logger;
        }

        public async Task<InstructorCalendar> BuildInstructorAsync(string instructorName)
        {
            var instructor = new InstructorCalendar { Name = _appSettings.Value.InstructorName, CalendarDays = new List<CalendarDay>() };

            var today = DateTime.Today;
            using (var bpp = new BookingPageParser(_appSettings.Value, _logger))
            {
                for (var d = 0; d < _appSettings.Value.DaysToScan; d++)
                {
                    var parseDate = today.AddDays(d);
                    _logger.LogInformation($"Parsing bookings page for {parseDate:dd/MM/yyyy}");

                    try
                    {
                        var calDay = await bpp.GetBookings(parseDate, _appSettings.Value.InstructorName);
                        instructor.CalendarDays.Add(calDay);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed to parse {parseDate:dd/MM/yyyy}.");
                    }

                    Task.Delay(5000).Wait();
                }
            }

            return instructor;
        }
    }
}
