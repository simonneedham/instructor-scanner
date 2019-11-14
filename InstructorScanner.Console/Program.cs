using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace InstructorScanner.ConsoleApp
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var configurationRoot = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var appSettings = new AppSettings();
            configurationRoot
                .GetSection("AppSettings")
                .Bind(appSettings);

            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .AddConsole();
            });
            var logger = loggerFactory.CreateLogger("InstructorScanner.ConsoleApp");


            var instructor = new Instructor { Name = appSettings.InstructorName, CalendarDays = new List<CalendarDay>() };

            var today = DateTime.Today;
            using (var bpp = new BookingPageParser(appSettings, logger))
            {
                for (var d = 0; d < appSettings.DaysToScan; d++)
                {
                    var parseDate = today.AddDays(d);
                    logger.LogInformation($"Parsing bookings page for {parseDate:dd/MM/yyyy}");

                    try
                    {
                        var calDay = await bpp.GetBookings(parseDate, appSettings.InstructorName);
                        instructor.CalendarDays.Add(calDay);
                        logger.LogInformation($"Found {calDay.Slots.Where(w => w.Availability == AvailabilityNames.Free).ToList().Count } free booking slots.");
                        logger.LogInformation(string.Empty);
                    }
                    catch(Exception ex)
                    {
                        logger.LogError(ex, $"Failed to parse {parseDate:dd/MM/yyyy}.");
                    }

                    Task.Delay(5000).Wait();
                }
            }

            var instructorBookingsJsonPath = Path.Combine(Directory.GetCurrentDirectory(), "instructor-bookings.json");
            using (var file = File.CreateText(instructorBookingsJsonPath))
            {
                var serializer = new JsonSerializer();
                serializer.Serialize(file, instructor);
            }


            Console.ReadLine();
        }


    }
}
