using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Collections;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using NewLife.Cube.ViewModels;
using Xunit;
using AdminCubeController = NewLife.Cube.Areas.Admin.Controllers.CubeController;

namespace NewLife.Cube.Tests.Common;

/// <summary>前端皮肤测试用例集合。运行时切换会改写全局设置，禁止并行</summary>
[CollectionDefinition("CubeSkin", DisableParallelization = true)]
public class CubeSkinCollection { }

/// <summary>
/// 前端皮肤测试。覆盖多皮肤包共存时的默认选择、强制指定、运行时切换（改设置后下一次请求生效）、
/// 静态资源按当前皮肤解析、SPA 回退，以及重复调用 UseCubeSkin 的幂等性。
/// </summary>
[Collection("CubeSkin")]
public class CubeSkinTests
{
    #region 辅助
    private static async Task<(WebApplication App, HttpClient Client)> StartAsync(String? skin = null)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseSetting(WebHostDefaults.ServerUrlsKey, "http://127.0.0.1:0");

        var app = builder.Build();
        app.UseCubeSkin(app.Environment, skin);

        await app.StartAsync();

        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();
        var client = new HttpClient { BaseAddress = new Uri(address) };

        return (app, client);
    }

    private static async Task<String> GetTextAsync(HttpClient client, String path)
    {
        var rs = await client.GetAsync(path);
        Assert.True(rs.IsSuccessStatusCode, $"{path} => {rs.StatusCode}");
        return await rs.Content.ReadAsStringAsync();
    }
    #endregion

    [Fact(DisplayName = "皮肤_按设置选择皮肤并即时切换")]
    public async Task Skin_FollowsSetting_AndSwitchesAtRuntime()
    {
        var old = CubeSetting.Current;
        CubeSetting.Current = new CubeSetting { SpaSkin = "FakeA" };

        var (app, client) = await StartAsync();
        try
        {
            Assert.Contains("FakeA-index", await GetTextAsync(client, "/"));
            Assert.Contains("A-js", await GetTextAsync(client, "/app.js"));

            // 模拟管理后台保存设置：下一次请求立即使用新皮肤，无需重启
            CubeSetting.Current.SpaSkin = "FakeB";

            Assert.Contains("FakeB-index", await GetTextAsync(client, "/"));
            Assert.Contains("B-js", await GetTextAsync(client, "/app.js"));
        }
        finally
        {
            await app.StopAsync();
            await app.DisposeAsync();
            client.Dispose();
            CubeSetting.Current = old;
        }
    }

    [Fact(DisplayName = "皮肤_强制指定优先于配置")]
    public async Task Skin_Forced_WinsOverSetting()
    {
        var old = CubeSetting.Current;
        CubeSetting.Current = new CubeSetting { SpaSkin = "FakeA" };

        var (app, client) = await StartAsync("FakeB");
        try
        {
            Assert.Contains("FakeB-index", await GetTextAsync(client, "/"));
        }
        finally
        {
            await app.StopAsync();
            await app.DisposeAsync();
            client.Dispose();
            CubeSetting.Current = old;
        }
    }

    [Fact(DisplayName = "皮肤_重复调用UseCubeSkin不产生歧义路由")]
    public async Task Skin_DuplicatedCall_IsIdempotent()
    {
        var old = CubeSetting.Current;
        CubeSetting.Current = new CubeSetting { SpaSkin = "FakeA" };

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseSetting(WebHostDefaults.ServerUrlsKey, "http://127.0.0.1:0");

        var app = builder.Build();
        app.UseCubeSkin(app.Environment);
        app.UseCubeSkin(app.Environment, "FakeB");

        await app.StartAsync();
        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();
        using var client = new HttpClient { BaseAddress = new Uri(address) };
        try
        {
            // 第二次调用被忽略，仍使用首次注册的皮肤；若重复注册回退路由，此处会抛路由歧义
            Assert.Contains("FakeA-index", await GetTextAsync(client, "/"));
        }
        finally
        {
            await app.StopAsync();
            await app.DisposeAsync();
            CubeSetting.Current = old;
        }
    }

    [Fact(DisplayName = "皮肤_管理器忽略大小写查找并在配置无效时回退默认")]
    public void SkinManager_Find_AndFallback()
    {
        var old = CubeSetting.Current;
        CubeSetting.Current = new CubeSetting { SpaSkin = "not-exists" };

        try
        {
            var manager = new CubeSkinManager();

            Assert.NotNull(manager.Find("fakea"));
            Assert.NotNull(manager.Find("FAKEB"));

            // 配置了不存在的皮肤时回退到默认皮肤，不抛异常
            var current = manager.Current;
            Assert.NotNull(current);
            Assert.Equal(current!.Name, manager.CurrentName);
        }
        finally
        {
            CubeSetting.Current = old;
        }
    }

    [Fact(DisplayName = "设置页_皮肤字段渲染为下拉并列出已发现皮肤")]
    public void CubeController_SpaSkinField_HasDataSource()
    {
        var old = CubeSetting.Current;
        CubeSetting.Current = new CubeSetting();

        try
        {
            var controller = new AdminCubeController(new CubeSkinManager());
            var method = typeof(AdminCubeController).GetMethod("OnGetFields", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);

            var list = (IList<DataField>)method!.Invoke(controller, [ViewKinds.EditForm])!;
            var df = list.FirstOrDefault(e => e.Name == nameof(CubeSetting.SpaSkin));

            Assert.NotNull(df);
            Assert.Equal("singleSelect", df!.ItemType);
            Assert.NotNull(df.DataSource);

            var dataSource = (IDictionary)df.DataSource(null)!;
            Assert.Contains("FakeA", dataSource.Keys.Cast<String>());
            Assert.Contains("Vue", dataSource.Keys.Cast<String>());
        }
        finally
        {
            CubeSetting.Current = old;
        }
    }

    [Fact(DisplayName = "皮肤_文件提供者按皮肤缓存")]
    public void SkinManager_FileProvider_Cached()
    {
        var manager = new CubeSkinManager();
        var skin = manager.Find("FakeA");
        Assert.NotNull(skin);

        var first = manager.GetFileProvider(skin!);
        var second = manager.GetFileProvider(skin!);

        Assert.NotNull(first);
        Assert.Same(first, second);
    }
}

/// <summary>测试用内存文件提供者</summary>
internal sealed class MemoryFileProvider : IFileProvider
{
    private readonly IDictionary<String, String> _files;

    public MemoryFileProvider(IDictionary<String, String> files) => _files = files;

    public IFileInfo GetFileInfo(String subpath)
    {
        var path = subpath.TrimStart('/');
        return _files.TryGetValue(path, out var text) ? new MemoryFileInfo(path, text) : new NotFoundFileInfo(subpath);
    }

    public IDirectoryContents GetDirectoryContents(String subpath) => NotFoundDirectoryContents.Singleton;

    public IChangeToken Watch(String filter) => NullChangeToken.Singleton;
}

/// <summary>测试用内存文件</summary>
internal sealed class MemoryFileInfo : IFileInfo
{
    private readonly Byte[] _data;

    public MemoryFileInfo(String name, String content)
    {
        Name = name;
        _data = Encoding.UTF8.GetBytes(content);
    }

    public Boolean Exists => true;
    public Int64 Length => _data.Length;
    public String? PhysicalPath => null;
    public String Name { get; }
    public DateTimeOffset LastModified => DateTimeOffset.UtcNow;
    public Boolean IsDirectory => false;
    public Stream CreateReadStream() => new MemoryStream(_data);
}

/// <summary>测试用皮肤A</summary>
internal sealed class FakeSkinA : ICubeSkin
{
    public String Name => "FakeA";
    public String DisplayName => "Fake A";
    public IFileProvider CreateFileProvider() => new MemoryFileProvider(new Dictionary<String, String>
    {
        ["index.html"] = "<html>FakeA-index</html>",
        ["app.js"] = "A-js",
    });
}

/// <summary>测试用皮肤B</summary>
internal sealed class FakeSkinB : ICubeSkin
{
    public String Name => "FakeB";
    public String DisplayName => "Fake B";
    public IFileProvider CreateFileProvider() => new MemoryFileProvider(new Dictionary<String, String>
    {
        ["index.html"] = "<html>FakeB-index</html>",
        ["app.js"] = "B-js",
    });
}
