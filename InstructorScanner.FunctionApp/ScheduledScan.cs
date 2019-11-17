using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace InstructorScanner.FunctionApp
{
    public static class ScheduledScan
    {
        [FunctionName("ScheduledScan")]
        public static void Run([TimerTrigger("0 5 8,12,16,20 * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
        }
    }
}
