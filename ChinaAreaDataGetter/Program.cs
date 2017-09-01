using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace ChinaAreaDataGetter
{
    class Program
    {
        static HttpClient httpClient = new HttpClient();
        static string baseUrl = "http://www.stats.gov.cn/tjsj/tjbz/tjyqhdmhcxhfdm/2016/";

        static void Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            string htmlStr = GetHtmlString(httpClient, $"{baseUrl}index.html");
            var dataTemp = GetArea(htmlStr, null, null);
            var data = dataTemp.Select(s => new Area
            {
                Code = s.Code,
                Name = s.Name,
                Parent = s.Parent
            });
            string json = JsonConvert.SerializeObject(data);
            File.WriteAllText("area.json", json);
        }

        private static string GetHtmlString(HttpClient httpClient, string url)
        {
            var html = httpClient.GetByteArrayAsync(url).Result;
            return Encoding.GetEncoding("gb2312").GetString(html);
        }

        private static IList<AreaTemp> GetArea(string html, string path, string parentCode)
        {
            var list = new List<AreaTemp>();
            var reg = new Regex(@"\d{2,}\.html'\s{0,}>\D{2,}?<");
            var match = reg.Match(html);
            while (true)
            {
                if (!match.Success) break;
                string code = match.Value.Split('.')[0].Trim();
                string name = match.Value.Split('>')[1].Split('<')[0].Trim();

                var urlBuilder = new StringBuilder();
                if (path != null) urlBuilder.Append($"{path}/");
                string nextUrl = urlBuilder.Append($"{code}.html").ToString();
                string padedCode = code.PadRight(12, '0');
                list.Add(new AreaTemp
                {
                    Code = padedCode,
                    Name = name,
                    Parent = parentCode,
                    NextUrl = nextUrl
                });
                Console.WriteLine($"{code},{name}");

                try
                {
                    string nextHtml = GetHtmlString(httpClient, $"{baseUrl}{nextUrl}");
                    var pathTemp = path == null ? "" : $"{path}/";
                    var nextArea = GetArea(nextHtml, $"{pathTemp}{code.Substring(code.Length - 2)}", padedCode);
                    list.AddRange(nextArea);
                }
                catch (AggregateException)
                {
                }

                match = match.NextMatch();
            }
            return list;
        }
    }
}
