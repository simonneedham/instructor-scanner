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

            var rootUrl = new Uri(appSettings.RootUrl);
            var loginPageUrl = new Uri(rootUrl, appSettings.LoginPage);
            var bookingPageUrl = new Uri(rootUrl, appSettings.BookingPage);

            var cookies = new CookieContainer();

            using (var httpClientHandler = new HttpClientHandler { CookieContainer = cookies })
            using (var httpClient = new HttpClient(httpClientHandler))
            {

                var loginPageHtml = await httpClient.GetStringAsync(loginPageUrl);
                var dictionaryLoginInputs = GetDictionaryLoginInputs(appSettings, loginPageHtml);
                WriteCookiesToConsole(cookies, loginPageUrl);


                var bookingPageHtml = await httpClient.PostAsync(loginPageUrl, new FormUrlEncodedContent(dictionaryLoginInputs));
                bookingPageHtml.EnsureSuccessStatusCode();
                Console.WriteLine($"StatusCode: {bookingPageHtml.StatusCode}");
                WriteCookiesToConsole(cookies, loginPageUrl);

                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine(await bookingPageHtml.Content.ReadAsStringAsync());

                Console.ReadLine();
            }
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
                //Console.WriteLine($"{input.GetAttributeValue("name", string.Empty)}: {input.GetAttributeValue("value", string.Empty)}");
            }

            dictionary["ctl00$UserID"] = appSettings.Username;
            dictionary["ctl00$Pwd"] = appSettings.Password;

            if(!dictionary.ContainsKey("__EVENTTARGET"))
            {
                dictionary.Add("__EVENTTARGET", string.Empty);
            }

            if (!dictionary.ContainsKey("__EVENTARGUMENT"))
            {
                dictionary.Add("__EVENTARGUMENT", string.Empty);
            }

            dictionary.TryAdd("ctl00$ImageButton1.x", "0");
            dictionary.TryAdd("ctl00$ImageButton1.y", "0");

            return dictionary;
        }
    }
}
