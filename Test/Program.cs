using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using NewLife.Log;
using NewLife.School.Entity;
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
                Test2();
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
            var count = Student.Meta.Count;
            Console.WriteLine("{0:n0}", count);

            Student.Meta.Session.Dal.Session.ShowSQL = false;

            // 生成一批需要随机查询的编号
            var ids = Enumerable.Range(0, 1024).Select(e => Rand.Next(count)).ToArray();

            count = 10_000_000;
            var sw = Stopwatch.StartNew();
            for (var i = 0; i < count; i++)
            {
                var id = ids[i % 1024];
                var entity = Student.FindByID(id);
            }
            sw.Stop();

            var ms = sw.Elapsed.TotalMilliseconds;
            Console.WriteLine("查询[{0:n0}]次，耗时{1:n0}ms，速度{2:n0}qps", count, ms, count * 1000L / ms);
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
    }
}