using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using SendGrid.Helpers.Mail;
using System.Threading.Tasks;

namespace InstructorScanner.FunctionApp
{
    public class ScheduleInstructorScanStatusCheck
    {
        [FunctionName(nameof(ScheduleInstructorScanStatusCheck))]
        public async Task Run(
            [TimerTrigger("0 08 20 17 11 *")]TimerInfo myTimer,
            ILogger logger,
            [SendGrid(ApiKey = "AppSettings:SendGridApiKey")] IAsyncCollector<SendGridMessage> messageCollector
        )
        {
        }
    }
}
