using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace InstructorScanner.ConsoleApp
{
    class BookingPageParser : IDisposable
    {
        private readonly AppSettings _appSettings;
        private CookieContainer _cookies;
        private HttpClientHandler _httpClientHandler;
        private HttpClient _httpClient;
        private readonly Uri _rootUrl;


        public BookingPageParser(AppSettings appSettings)
        {
            _appSettings = appSettings;
            _rootUrl = new Uri(appSettings.RootUrl);
        }

        public async Task<CalendarDay> GetBookings(DateTime date, string instructorName)
        {
            if (_httpClient == null)
                await InitHttpClientAsync();

            var bookingPageUrl = new Uri(_rootUrl, $"{_appSettings.BookingPage}?dt={date.ToString("dd/MM/yyyy")}");
            var bookingPageResponse = await _httpClient.GetAsync(bookingPageUrl);
            bookingPageResponse.EnsureSuccessStatusCode();

            var bookingPageContents = await bookingPageResponse.Content.ReadAsStringAsync();

            var bookingPageParser = new HtmlDocument();
            bookingPageParser.LoadHtml(bookingPageContents);

            var tableBookings = bookingPageParser.DocumentNode.SelectSingleNode("//table[@id='tblBookings']");
            if (tableBookings == null) throw new Exception("Could not find table with an id of 'tblBookings'");

            var timesTDNodes = tableBookings.SelectNodes(".//td[@class='TimeHeaderHalf']"); //is this searching children only
            var times = timesTDNodes.Select(n => n.GetDirectInnerText()).ToList();

            //- Find row where tr / td innerText = 'Instructor Name'
            var slots = new List<Slot>();
            var instructorRowNode = tableBookings.SelectSingleNode($".//tr[td='{instructorName}']");
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

            var calendarDay = new CalendarDay
            {
                Date = date,
                Slots = slots
            };

            return calendarDay;
        }

        private string BookingStatus(HtmlNode htmlNode)
        {
            if (htmlNode.HasClass("AvailableCellBase")) return AvailabilityNames.Free;
            if (htmlNode.HasClass("BookedCellBase")) return AvailabilityNames.Booked;
            return AvailabilityNames.NA;
        }

        private async Task InitHttpClientAsync()
        {
            _cookies = new CookieContainer();
            _httpClientHandler = new HttpClientHandler { CookieContainer = _cookies };
            _httpClient = new HttpClient(_httpClientHandler);

            var loginPageUrl = new Uri(_rootUrl, _appSettings.LoginPage);

            var loginPageHtml = await _httpClient.GetStringAsync(loginPageUrl);
            var dictionaryLoginInputs = GetDictionaryLoginInputs(loginPageHtml);

            var bookingPageResponse = await _httpClient.PostAsync(loginPageUrl, new FormUrlEncodedContent(dictionaryLoginInputs));
            bookingPageResponse.EnsureSuccessStatusCode();
        }

        private Dictionary<string, string> GetDictionaryLoginInputs(string loginPageHtml)
        {
            var loginPageParser = new HtmlDocument();
            loginPageParser.LoadHtml(loginPageHtml);

            var inputs = loginPageParser.DocumentNode.SelectNodes("//form[@id='form1']//input");

            var dictionary = new Dictionary<string, string>();
            foreach (var input in inputs)
            {
                dictionary.Add(input.GetAttributeValue("name", string.Empty), input.GetAttributeValue("value", string.Empty));
            }

            dictionary["ctl00$UserID"] = _appSettings.Username;
            dictionary["ctl00$Pwd"] = _appSettings.Password;

            dictionary.TryAdd("__EVENTTARGET", string.Empty);
            dictionary.TryAdd("__EVENTARGUMENT", string.Empty);
            dictionary.TryAdd("ctl00$ImageButton1.x", "0");
            dictionary.TryAdd("ctl00$ImageButton1.y", "0");

            return dictionary;
        }

        public void WriteCookiesToConsole(Uri uri)
        {
            if (_cookies == null) return;

            foreach (var cookie in _cookies.GetCookies(uri).Cast<Cookie>())
            {
                Console.WriteLine($"{cookie.Name}: {cookie.Value}");
            }
        }


        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _cookies = null;
                    if (_httpClientHandler != null) _httpClientHandler.Dispose();
                    if (_httpClient != null) _httpClient.Dispose();

                    _httpClientHandler = null;
                    _httpClient = null;

                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
