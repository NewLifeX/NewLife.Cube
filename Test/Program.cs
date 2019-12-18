using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using NewLife.Log;
using NewLife.School.Entity;
using NewLife.Security;

namespace Test
{
    class Program
    {
        static void Main(String[] args)
        {
            XTrace.UseConsole();

            var set = XCode.Setting.Current;
            if (set.IsNew)
            {
                set.SQLiteDbPath = "..\\Data";
                set.SaveAsync();
            }

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

        static void Test2()
        {
            for (var i = 0; i < 1000; i++)
            {
                var ticks = Environment.TickCount;
                var ts = TimeSpan.FromMilliseconds(ticks);
                Console.WriteLine(ts);

                Thread.Sleep(1000);
            }
        }
    }
}