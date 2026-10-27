using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using NewLife.Cube.Vue;
using Xunit;

namespace NewLife.Cube.Tests.Common;

/// <summary>
/// SPA-3 回退路由测试。用真实 Kestrel（随机端口）跑 Vue 皮肤的 UseVue，验证前端路由与不存在的静态资源都回退到 index.html，
/// 嵌入式静态文件可正常访问。
/// </summary>
public class SpaFallbackTests
{
    private const String IndexMarker = "<div id=\"app\"></div>";

    private static async Task<(WebApplication App, HttpClient Client)> StartAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseSetting(WebHostDefaults.ServerUrlsKey, "http://127.0.0.1:0");

        var app = builder.Build();
        app.UseVue(app.Environment);

        await app.StartAsync();

        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();
        var client = new HttpClient { BaseAddress = new Uri(address) };

        return (app, client);
    }

    [Fact(DisplayName = "SPA回退_前端路由回退到index.html")]
    public async Task Fallback_FrontendRoute_ServesIndex()
    {
        var (app, client) = await StartAsync();
        try
        {
            var rs = await client.GetAsync("/cube/user/list?page=2");
            var html = await rs.Content.ReadAsStringAsync();

            Assert.True(rs.IsSuccessStatusCode, rs.StatusCode.ToString());
            Assert.Contains(IndexMarker, html);
        }
        finally
        {
            await app.StopAsync();
            await app.DisposeAsync();
            client.Dispose();
        }
    }

    [Fact(DisplayName = "SPA回退_带扩展名的缺失资源不回退")]
    public async Task Fallback_MissingAsset_ReturnsNotFound()
    {
        var (app, client) = await StartAsync();
        try
        {
            // MapFallbackToFile 只匹配"非文件"路径，带扩展名的请求保持 404，避免把 HTML 当脚本返回
            var rs = await client.GetAsync("/assets/not-exists.js");

            Assert.Equal(System.Net.HttpStatusCode.NotFound, rs.StatusCode);
        }
        finally
        {
            await app.StopAsync();
            await app.DisposeAsync();
            client.Dispose();
        }
    }

    [Fact(DisplayName = "SPA回退_嵌入式静态文件可直接访问")]
    public async Task StaticFile_Embedded_IsServed()
    {
        var (app, client) = await StartAsync();
        try
        {
            var rs = await client.GetAsync("/favicon.ico");

            Assert.True(rs.IsSuccessStatusCode, rs.StatusCode.ToString());
            Assert.True((await rs.Content.ReadAsByteArrayAsync()).Length > 0);
        }
        finally
        {
            await app.StopAsync();
            await app.DisposeAsync();
            client.Dispose();
        }
    }
}
