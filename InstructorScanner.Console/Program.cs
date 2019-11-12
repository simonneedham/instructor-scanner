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
            string bookingPageContents;
            var bookingPageFile = Path.Combine(Directory.GetCurrentDirectory(), "BookingPage.html");

            if (!File.Exists(bookingPageFile))
            {
                Console.WriteLine("Loading from web");

                var configurationRoot = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .Build();

                var appSettings = new AppSettings();
                configurationRoot
                .GetSection("AppSettings")
                .Bind(appSettings);

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

            var bookingPageParser = new HtmlDocument();
            bookingPageParser.LoadHtml(bookingPageContents);

            //- Read Table @class = 'BookingTable floatThead-table' / thead / tr / td for times
            // //div[@class='floatThead-container']/table[@class='BookingTable']/thead/tr
            // //*[@id="BookingPlaceHolder"]/div/table/thead/tr[1]/td

            var tableBookings = bookingPageParser.DocumentNode.SelectSingleNode("//table[@id='tblBookings']");
            if (tableBookings == null) throw new Exception("Could not find table with an id of 'tblBookings'");

            var timesTDNodes = tableBookings.SelectNodes("//td[@class='TimeHeaderHalf']"); //is this searching children only
            var times = timesTDNodes.Select(n => n.GetDirectInnerText()).ToList();

            //- Find Table@id = tblBookings / tbody
            //- Find row where tr / td innerText = 'C Swinhoe-Standen'

            //- Read each TD class
            // - class='UnavailableBase'
            // - class='AvailableCellBase'
            // - class='BookedCellBase'



            Console.ReadLine();
        }

        private static void WriteCookiesToConsole(CookieContainer cookies, Uri uri)
        {
            foreach(var cookie in cookies.GetCookies(uri).Cast<Cookie>())
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
