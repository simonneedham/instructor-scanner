using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InstructorScanner.FunctionApp
{
    public interface ICalendarDayListBuilder
    {
        Task<List<CalendarDay>> BuildAsync();
    }

    public class CalendarDayListBuilder : ICalendarDayListBuilder
    {
        private readonly IOptions<AppSettings> _appSettings;
        private readonly ILogger<CalendarDayListBuilder> _logger;

        public CalendarDayListBuilder(
            IOptions<AppSettings> appSettings,
            ILogger<CalendarDayListBuilder> logger
        )
        {
            _appSettings = appSettings;
            _logger = logger;
        }

        public async Task<List<CalendarDay>> BuildAsync()
        {
            var calendarDays = new List<CalendarDay>();

            var today = DateTime.Today;
            using (var bpp = new BookingPageParser(_appSettings, _logger))
            {
                for (var d = _appSettings.Value.StartWindowDays; d < _appSettings.Value.DaysToScan; d++)
                {
                    var parseDate = today.AddDays(d);
                    _logger.LogInformation($"Parsing bookings page for {parseDate:dd/MM/yyyy}");

                    try
                    {
                        var calDay = await bpp.GetBookings(parseDate, _appSettings.Value.Instructors);
                        calendarDays.Add(calDay);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed to parse {parseDate:dd/MM/yyyy}.");
                    }

                    Task.Delay(5000).Wait();
                }
            }

            return calendarDays;
        }
    }
}
