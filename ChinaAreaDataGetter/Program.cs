using AngleSharp;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ChinaAreaDataGetter
{
    class Program
    {
        static Uri baseUrl = new Uri("http://www.stats.gov.cn/tjsj/tjbz/tjyqhdmhcxhfdm/2016/");

        static List<(string name, string code, string parentCode)> list = new List<(string name, string code, string parentCode)>();
        static void Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var config = Configuration.Default.WithDefaultLoader();
            var provinces = GetProvinces(config).ToArray();

            foreach (var (name, url, code) in provinces)
            {
                AddCellsToList(config, url, code);
            }
            Console.WriteLine("爬取完成");
            string json = JsonConvert.SerializeObject(list);
            File.WriteAllText("data.json", json);
        }

        /// <summary>
        /// 递归添加数据到list
        /// </summary>
        /// <param name="config"></param>
        /// <param name="parentUrl"></param>
        /// <param name="parentCode"></param>
        private static void AddCellsToList(IConfiguration config, Uri parentUrl, string parentCode)
        {
            var doc = BrowsingContext.New(config).OpenAsync(parentUrl.ToString()).Result;
            var cells = doc.QuerySelectorAll(".citytr,.countytr,.towntr,.villagetr");
            foreach (var item in cells)
            {
                string name = item.QuerySelectorAll("td").Last().TextContent;
                string code = item.QuerySelector("td a")?.TextContent ?? item.QuerySelector("td").TextContent;
                string url = item.QuerySelector("td a")?.GetAttribute("href");
                Console.WriteLine($"{name} {url} {code}");
                list.Add((name, code, parentCode));
                if (url != null)
                {
                    string mUrl = Regex.Match(parentUrl.ToString(), ".+/").Value;
                    AddCellsToList(config, new Uri(new Uri(mUrl), url), code);
                }
            }
        }

        /// <summary>
        /// 获取到省数据
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        private static IEnumerable<(string name, Uri url, string code)> GetProvinces(IConfiguration config)
        {
            var indexUrl = new Uri(baseUrl, "Index.html");
            var doc = BrowsingContext.New(config).OpenAsync(indexUrl.ToString()).Result;
            var provinces = doc.QuerySelectorAll(".provincetr a");
            foreach (var item in provinces)
            {
                string name = item.TextContent;
                string url = item.GetAttribute("href");
                string code = Regex.Match(url, "[0-9].").Value;
                Console.WriteLine($"{name} {url} {code}");
                list.Add((name, code, ""));
                yield return (name, new Uri(baseUrl, url), code);
            }
        }
    }
}
