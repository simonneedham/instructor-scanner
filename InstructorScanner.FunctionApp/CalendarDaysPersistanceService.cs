using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InstructorScanner.FunctionApp
{
    public interface ICalendarDaysPersistanceService
    {
        Task<List<CalendarDay>> Retrieve();
        Task Store(List<CalendarDay> calendarDays);
    }

    public class CalendarDaysPersistanceService : ICalendarDaysPersistanceService
    {
        private const string CONTAINER_NAME = "instructor-scan";
        private const string FILE_NAME = "instructors-and-slots.json";

        private readonly IOptions<AppSettings> _appSettings;
        private readonly IStorageHelper _storageHelper;

        public CalendarDaysPersistanceService(
            IOptions<AppSettings> appSettings,
            IStorageHelper storageHelper
        )
        {
            _appSettings = appSettings;
            _storageHelper = storageHelper;
        }

        public async Task<List<CalendarDay>> Retrieve()
        {
            List<CalendarDay> previousCalendarDays;
            if (await _storageHelper.FileExistsAsync(CONTAINER_NAME, FILE_NAME))
            {
                previousCalendarDays = JsonConvert.DeserializeObject<List<CalendarDay>>(await _storageHelper.ReadFileAsync(CONTAINER_NAME, FILE_NAME));
            }
            else
            {
                previousCalendarDays = new List<CalendarDay>();
            }

            return previousCalendarDays;
        }

        public async Task Store(List<CalendarDay> calendarDays)
        {
            var newCalendarDaysJson = JsonConvert.SerializeObject(calendarDays);
            await _storageHelper.SaveFileAsync(CONTAINER_NAME, FILE_NAME, newCalendarDaysJson);
        }
    }
}
