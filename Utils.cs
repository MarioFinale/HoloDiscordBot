using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace HoloDiscordBot
{
    internal class Utils
    {

        public static string userAgent = "Mozilla/5.0 (SMART-TV; Linux; Tizen 2.4.0) AppleWebkit/538.1 (KHTML, like Gecko) SamsungBrowser/1.1 tv Safari/538.1";//Samsung Smart-TV web agent to avoid weird javascript anti-bot challenges

        public static Tuple<string, bool> GETWebResourceAsText(string url)
        {
            Uri resourceUri = new(url);
            string response;

            CookieContainer cookies = new();

            HttpClientHandler handler = new()
            {
                CookieContainer = cookies,
                UseCookies = true
            };
            HttpClient client = new(handler);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
            client.DefaultRequestHeaders.Connection.ParseAdd("keep-alive");
            client.DefaultRequestHeaders.Add("Method", "GET");
            client.Timeout = new TimeSpan(0, 0, 30);

            try
            {
                Task<HttpResponseMessage> message = client.GetAsync(resourceUri);
                HttpResponseMessage res = message.Result;
                System.Net.Http.Headers.HttpResponseHeaders theaders = res.Headers;
                response = res.Content.ReadAsStringAsync().Result;
                cookies.Add(cookies.GetCookies(resourceUri));
            }
            catch (WebException ex)
            {
                Console.WriteLine("Big oof: " + ex.Message);
                return new Tuple<string, bool>(string.Empty, false);
            }

            return new Tuple<string, bool>(response, true);

        }


        public static Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        public static string CapitalizeFirstLetter(string word)
        {
            return char.ToUpperInvariant(word[0]) + word[1..];
        }

    }
}
