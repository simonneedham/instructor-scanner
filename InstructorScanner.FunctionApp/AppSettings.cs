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
        public string InstructorName { get; set; }
        //public Instructor[] Instructors { get; set; }
        public List<Instructor> Instructors { get; set; }
        public int DaysToScan { get; set; }
        public string StorageConnectionString { get; set; }
        public string SendGridApiKey { get; set; }
        public string EmailAddress { get; set; }
    }
}
