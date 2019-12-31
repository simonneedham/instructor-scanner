using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InstructorScanner.FunctionApp
{
    public interface ICalendarDaysPersistanceService
    {
        Task<List<CalendarDay>> RetrieveAsync();
        Task StoreAsync(List<CalendarDay> calendarDays);
    }

    public class CalendarDaysPersistanceService : ICalendarDaysPersistanceService
    {
        private const string FILE_NAME = "instructors-and-slots.json";

        private readonly IStorageHelper _storageHelper;

        public CalendarDaysPersistanceService(
            IStorageHelper storageHelper
        )
        {
            _storageHelper = storageHelper;
        }

        public async Task<List<CalendarDay>> RetrieveAsync()
        {
            List<CalendarDay> previousCalendarDays;
            if (await _storageHelper.FileExistsAsync(ContainerNames.InstructorScan, FILE_NAME))
            {
                previousCalendarDays = JsonConvert.DeserializeObject<List<CalendarDay>>(await _storageHelper.ReadFileAsync(ContainerNames.InstructorScan, FILE_NAME));
            }
            else
            {
                previousCalendarDays = new List<CalendarDay>();
            }

            return previousCalendarDays;
        }

        public async Task StoreAsync(List<CalendarDay> calendarDays)
        {
            var newCalendarDaysJson = JsonConvert.SerializeObject(calendarDays);
            await _storageHelper.SaveFileAsync(ContainerNames.InstructorScan, FILE_NAME, newCalendarDaysJson);
        }
    }
}
