using Microsoft.Extensions.Configuration;
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


            var instructor = new Instructor { Name = appSettings.InstructorName, CalendarDays = new List<CalendarDay>() };

            var today = DateTime.Today;
            using (var bpp = new BookingPageParser(appSettings))
            {
                for (var d = 0; d < appSettings.DaysToScan; d++)
                {
                    var parseDate = today.AddDays(d);
                    Console.WriteLine($"Parsing bookings page for {parseDate:dd/MM/yyyy}");

                    try
                    {
                        var calDay = await bpp.GetBookings(parseDate, appSettings.InstructorName);
                        instructor.CalendarDays.Add(calDay);
                        Console.WriteLine($"Found {calDay.Slots.Where(w => w.Availability == AvailabilityNames.Free).ToList().Count } free booking slots.");
                        Console.WriteLine();
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine($"Failed to parse {parseDate:dd/MM/yyyy}. {ex.Message}");
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
