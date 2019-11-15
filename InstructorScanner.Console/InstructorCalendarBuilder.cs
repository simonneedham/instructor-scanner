using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InstructorScanner.ConsoleApp
{
    class InstructorCalendarBuilder
    {
        private readonly AppSettings _appSettings;
        private readonly ILogger _logger;

        public InstructorCalendarBuilder(
            AppSettings appSettings,
            ILogger logger
        )
        {
            _appSettings = appSettings;
            _logger = logger;
        }

        public async Task<InstructorCalendar> BuildInstructorAsync(string instructorName)
        {
            var instructor = new InstructorCalendar { Name = _appSettings.InstructorName, CalendarDays = new List<CalendarDay>() };

            var today = DateTime.Today;
            using (var bpp = new BookingPageParser(_appSettings, _logger))
            {
                for (var d = 0; d < _appSettings.DaysToScan; d++)
                {
                    var parseDate = today.AddDays(d);
                    _logger.LogInformation($"Parsing bookings page for {parseDate:dd/MM/yyyy}");

                    try
                    {
                        var calDay = await bpp.GetBookings(parseDate, _appSettings.InstructorName);
                        instructor.CalendarDays.Add(calDay);
                        _logger.LogInformation($"Found {calDay.Slots.Where(w => w.Availability == AvailabilityNames.Free).ToList().Count } free booking slots.");
                        _logger.LogInformation(string.Empty);
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
