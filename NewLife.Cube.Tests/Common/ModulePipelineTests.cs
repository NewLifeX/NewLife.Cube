using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using NewLife.Cube.Modules;
using Xunit;

namespace NewLife.Cube.Tests.Common;

/// <summary>
/// 模块管道测试。按 UseCube 的装配顺序（Use 在路由之前、Map 在 SPA 回退之前）验证模块中间件与端点生效。
/// </summary>
public class ModulePipelineTests
{
    [Fact(DisplayName = "模块管道_中间件生效且端点优先于SPA回退")]
    public async Task Module_UseAndMap_Work()
    {
        NlMod.PipelineModule.Enabled = true;

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseSetting(WebHostDefaults.ServerUrlsKey, "http://127.0.0.1:0");
        var app = builder.Build();

        var manager = new ModuleManager();
        manager.Load();

        // 与 UseCube 相同的位置：中间件挂在路由注册之前
        manager.Use(app, app.Environment);

        app.UseRouter(endpoints =>
        {
            endpoints.MapGet("/api/ping", () => "api");

            // 模块端点位于前端回退之前
            manager.Map(endpoints);

            endpoints.MapFallback(() => "spa");
        });

        await app.StartAsync();

        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();
        using var client = new HttpClient { BaseAddress = new Uri(address) };
        try
        {
            var rs = await client.GetAsync("/module/ping");
            Assert.True(rs.IsSuccessStatusCode, rs.StatusCode.ToString());
            Assert.Equal("pong", await rs.Content.ReadAsStringAsync());
            Assert.Equal("cube", rs.Headers.GetValues("X-Cube-Module").FirstOrDefault());

            // 非模块路径仍然回退到前端入口
            Assert.Equal("spa", await client.GetStringAsync("/cube/user/list"));
        }
        finally
        {
            await app.StopAsync();
            await app.DisposeAsync();
            NlMod.PipelineModule.Enabled = false;
        }
    }
}
