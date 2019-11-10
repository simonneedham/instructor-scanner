using HtmlAgilityPack;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace InstructorScanner.ConsoleApp
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var loginUrl = @"https://wlacbooking.co.uk/Login.aspx";

            var httpClient = new HttpClient();
            var loginPageHtml = await httpClient.GetStringAsync(loginUrl);
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
