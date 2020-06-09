using System.Collections.Generic;

namespace InstructorScanner.FunctionApp
{
    public class AppSettings
    {
        public string RootUrl { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string LoginPage { get; set; }
        public string BookingPage { get; set; }
        public List<Instructor> Instructors { get; set; }
        public int DaysToScan { get; set; }
        public string StorageConnectionString { get; set; }
        public string SendGridApiKey { get; set; }
        public string ToEmailAddress { get; set; }
        public string FromEmailAddress { get; set; }
        public string WebRootUrl { get; set; }
        public int StartWindowDays { get; set; }
        public string CosmosDbDatabaseName { get; set; }
        public string CosmosDbAccountKey { get; set; }
        public string CosmosDbAccountEndPoint { get; set; }
    }
}
