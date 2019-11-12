using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
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


            var instructor = new Instructor { Name = appSettings.InstructorName };

            string bookingPageContents;
            var bookingPageFile = Path.Combine(Directory.GetCurrentDirectory(), "BookingPage.html");

            if (!File.Exists(bookingPageFile))
            {
                Console.WriteLine("Loading from web");

                var rootUrl = new Uri(appSettings.RootUrl);
                var loginPageUrl = new Uri(rootUrl, appSettings.LoginPage);
                var bookingPageUrl = new Uri(rootUrl, $"{appSettings.BookingPage}?dt=20/11/2019");

                var cookies = new CookieContainer();

                using (var httpClientHandler = new HttpClientHandler { CookieContainer = cookies })
                using (var httpClient = new HttpClient(httpClientHandler))
                {
                    var loginPageHtml = await httpClient.GetStringAsync(loginPageUrl);
                    var dictionaryLoginInputs = GetDictionaryLoginInputs(appSettings, loginPageHtml);

                    var bookingPageResponse = await httpClient.PostAsync(loginPageUrl, new FormUrlEncodedContent(dictionaryLoginInputs));
                    bookingPageResponse.EnsureSuccessStatusCode();

                    bookingPageResponse = await httpClient.GetAsync(bookingPageUrl);
                    bookingPageResponse.EnsureSuccessStatusCode();

                    bookingPageContents = await bookingPageResponse.Content.ReadAsStringAsync();
                    await File.WriteAllTextAsync(bookingPageFile, bookingPageContents);
                }
            }

            Console.WriteLine("Loading from file");
            bookingPageContents = await File.ReadAllTextAsync(bookingPageFile);

            var calendarDay = new CalendarDay { Date = new DateTime(2019, 11, 20) };

            var bookingPageParser = new HtmlDocument();
            bookingPageParser.LoadHtml(bookingPageContents);

            var tableBookings = bookingPageParser.DocumentNode.SelectSingleNode("//table[@id='tblBookings']");
            if (tableBookings == null) throw new Exception("Could not find table with an id of 'tblBookings'");

            var timesTDNodes = tableBookings.SelectNodes(".//td[@class='TimeHeaderHalf']"); //is this searching children only
            var times = timesTDNodes.Select(n => n.GetDirectInnerText()).ToList();

            //- Find row where tr / td innerText = 'Instructor Name'
            var slots = new List<Slot>();
            var instructorRowNode = tableBookings.SelectSingleNode($".//tr[td='{appSettings.InstructorName}']");
            if (instructorRowNode != null)
            {
                Console.WriteLine("Found instructor row");
                var bookings = instructorRowNode.SelectNodes(".//td").Select(n => BookingStatus(n)).ToList();

                if (times.Count != bookings.Count)
                    throw new Exception("Eeek!! Time slot count doesn't match bookings count!");


                for (var i = 0; i < bookings.Count; i++)
                {
                    slots.Add(new Slot { Availability = bookings[i], Time = times[i] });
                }
            }

            calendarDay.Slots = slots;
            instructor.CalendarDays = new List<CalendarDay> { calendarDay };

            Console.ReadLine();
        }

        private static string BookingStatus(HtmlNode htmlNode)
        {
            if (htmlNode.HasClass("AvailableCellBase")) return AvailabilityNames.Free;
            if (htmlNode.HasClass("BookedCellBase")) return AvailabilityNames.Booked;
            return AvailabilityNames.NA;
        }

        private static void WriteCookiesToConsole(CookieContainer cookies, Uri uri)
        {
            foreach (var cookie in cookies.GetCookies(uri).Cast<Cookie>())
            {
                Console.WriteLine($"{cookie.Name}: {cookie.Value}");
            }
        }

        private static Dictionary<string, string> GetDictionaryLoginInputs(AppSettings appSettings, string loginPageHtml)
        {
            var loginPageParser = new HtmlDocument();
            loginPageParser.LoadHtml(loginPageHtml);

            var inputs = loginPageParser.DocumentNode.SelectNodes("//form[@id='form1']//input");

            var dictionary = new Dictionary<string, string>();
            foreach (var input in inputs)
            {
                dictionary.Add(input.GetAttributeValue("name", string.Empty), input.GetAttributeValue("value", string.Empty));
                // Console.WriteLine($"{input.GetAttributeValue("name", string.Empty)}: {input.GetAttributeValue("value", string.Empty)}");
            }

            dictionary["ctl00$UserID"] = appSettings.Username;
            dictionary["ctl00$Pwd"] = appSettings.Password;

            dictionary.TryAdd("__EVENTTARGET", string.Empty);
            dictionary.TryAdd("__EVENTARGUMENT", string.Empty);
            dictionary.TryAdd("ctl00$ImageButton1.x", "0");
            dictionary.TryAdd("ctl00$ImageButton1.y", "0");

            return dictionary;
        }
    }
}
