using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using NewLife.Data;
using NewLife.IO;
using NewLife.Log;
using NewLife.Security;
using NewLife.Serialization;

namespace Test
{
    class Program
    {
        static void Main(String[] args)
        {
            XTrace.UseConsole();

            try
            {
                Test1();
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);
            }

            Console.WriteLine("OK!");
            Console.ReadKey();
        }

        static void Test1()
        {
            var snow = new Snowflake();

            var dt = new DateTime(2000, 1, 1);
            var id = snow.GetId(dt);
            XTrace.WriteLine("{0} {1} {1:X16}", dt, id);

            dt = new DateTime(2020, 1, 1);
            id = snow.GetId(dt);
            XTrace.WriteLine("{0} {1} {1:X16}", dt, id);

            dt = new DateTime(2022, 1, 1);
            id = snow.GetId(dt);
            XTrace.WriteLine("{0} {1} {1:X16}", dt, id);

            dt = new DateTime(2023, 1, 1);
            id = snow.GetId(dt);
            XTrace.WriteLine("{0} {1} {1:X16}", dt, id);

            dt = new DateTime(2022, 11, 11);
            for (int i = 0; i < 365; i++)
            {
                dt = dt.AddDays(1);
                id = snow.GetId(dt);
                XTrace.WriteLine("{0} {1} {1:X16}", dt, id);

                if (id >= 700_00000000_00000000) break;
            }
        }

        static async void Test2()
        {
            var key = "dingbvcq0mz3pidpwtch";
            var secret = "7OTdnimQwf5LJnVp8e0udX1wPxKyCsspLqM2YcBDawvg3BlIkzxIsOs1YhDjiOxj";
            var url = "https://oapi.dingtalk.com/gettoken?appkey={key}&appsecret={secret}";

            url = url.Replace("{key}", key).Replace("{secret}", secret);

            var http = new HttpClient();
            var html = await http.GetStringAsync(url);
            XTrace.WriteLine(html);

            var js = new JsonParser(html).Decode() as IDictionary<String, Object>;
            var token = js["access_token"] as String;
            XTrace.WriteLine("token: {0}", token);

            var url2 = "https://oapi.dingtalk.com/user/listbypage?access_token={token}&department_id=1&offset=0&size=100";
            url2 = url2.Replace("{token}", token);

            var html2 = await http.GetStringAsync(url2);
            XTrace.WriteLine(html2);
        }

        static void Test3()
        {
            using var csv = new CsvFile("Area.csv");
            while (true)
            {
                var line = csv.ReadLine();
                if (line == null) break;

                for (int i = 0; i < line.Length; i++)
                {
                    if (line[i].Length >= 45) XTrace.WriteLine(line[i]);
                }
            }
        }

        static async Task Test4()
        {
            var hc = new HttpClient();

            hc.DefaultRequestHeaders.Add("Connection", "keep-alive");
            hc.DefaultRequestHeaders.Range = new System.Net.Http.Headers.RangeHeaderValue(0, 100);


            var r = await hc.GetAsync("http://localhost:1880/iot/Attachment/DownloadFile?filename=7372469584366874624.bin");

            Console.WriteLine($"状态码: {r.StatusCode}");
            Console.WriteLine($"内容长度: {r.Content.Headers.ContentLength}");
            Console.WriteLine($"Accept-Ranges: {r.Headers.AcceptRanges}");
            Console.WriteLine($"Content-Range: {r.Content.Headers.ContentRange}");

            // 读取响应内容
            var content = await r.Content.ReadAsByteArrayAsync();
            Console.WriteLine($"实际接收字节数: {content.Length}");
        }
    }
}