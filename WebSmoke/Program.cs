using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NewLife;
using NewLife.Cube.Common;

namespace WebSmoke;

/// <summary>Web 指令集冒烟入口。在目标架构上验证 ASP.NET Core 宿主（Kestrel + 依赖注入 + HTTP 往返）与魔方程序集可用</summary>
/// <remarks>
/// 供 `test.yml` 的指令集矩阵在 QEMU 容器内执行：宿主 x64 SDK 构建（IL 与 RID 无关），容器内用目标架构运行时启动。
/// 非 0 退出码表示冒烟失败。仅覆盖不依赖数据库的宿主路径，数据库集成仍由原生 runner 上的单元测试承担。
/// </remarks>
internal static class Program
{
    private static Int32 _pass;
    private static Int32 _fail;

    private static void Check(String name, Boolean ok, String? detail = null)
    {
        if (ok)
        {
            _pass++;
            Console.WriteLine($"PASS {name}");
        }
        else
        {
            _fail++;
            Console.WriteLine($"FAIL {name} {detail}");
        }
    }

    private static async Task<Int32> Main(String[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Logging.ClearProviders();
        // 端口 0 交给 Kestrel 动态分配，避免与宿主已占用端口冲突
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        await using var app = builder.Build();
        app.MapGet("/ping", () => Results.Text("pong"));

        await app.StartAsync();
        try
        {
            var url = app.Urls.FirstOrDefault();
            Check("kestrel-bound", !url.IsNullOrEmpty(), url);

            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            var text = await http.GetStringAsync($"{url}/ping");
            Check("http-roundtrip", text == "pong", text);

            Check("cube-assembly", typeof(ValidFormatHelper).Assembly.GetName().Name == "NewLife.Cube");
            Check("cube-logic", ValidFormatHelper.IsEmail("smoke@example.com"));
        }
        finally
        {
            await app.StopAsync();
        }

        Console.WriteLine($"TOTAL pass={_pass} fail={_fail}");

        return _fail > 0 ? 1 : 0;
    }
}
