using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
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
            var jsonSerializer = new JsonSerializer();
            var instructorBookingsJsonPath = Path.Combine(Directory.GetCurrentDirectory(), "instructor-bookings.json");

            InstructorCalendar oldInstructorCalendar;
            if(File.Exists(instructorBookingsJsonPath))
            {
                oldInstructorCalendar = JsonConvert.DeserializeObject<InstructorCalendar>(await File.ReadAllTextAsync(instructorBookingsJsonPath));                    
            }
            else
            {
                oldInstructorCalendar = new InstructorCalendar
                {
                    CalendarDays = new List<CalendarDay>(),
                    Name = appSettings.InstructorName
                };
            }

            var icb = new InstructorCalendarBuilder(appSettings, logger);
            var newInstructorCalendar = await icb.BuildInstructorAsync(appSettings.InstructorName);

            var calendarChanges = InstructorCalendarComparer.Compare(oldInstructorCalendar, newInstructorCalendar);

            
            using (var file = File.CreateText(instructorBookingsJsonPath))
            {
                jsonSerializer.Serialize(file, newInstructorCalendar);
            }

            foreach(var msg in calendarChanges)
            {
                logger.LogInformation(msg);
            }


            Console.ReadLine();
        }


    }
}
