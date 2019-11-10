using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
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

            var httpClient = new HttpClient();
            var loginPageHtml = await httpClient.GetStringAsync(rootUrl);
            var loginPageParser = new HtmlDocument();
            loginPageParser.LoadHtml(loginPageHtml);

            var inputs = loginPageParser.DocumentNode.SelectNodes("//form[@id='form1']//input");

            foreach(var input in inputs)
            {
                Console.WriteLine($"{input.Id}: {input.GetAttributeValue("value", string.Empty)}");
            }

            Console.ReadLine();
        }
    }
}
