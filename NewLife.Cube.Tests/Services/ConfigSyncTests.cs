using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NewLife.Caching;
using NewLife.Common;
using NewLife.Configuration;
using NewLife.Cube;
using NewLife.Cube.Services;
using NewLife.Cube.Tests.Membership;
using NewLife.Cube.ViewModels;
using XCode.Configuration;
using XCode.Membership;
using Xunit;

namespace NewLife.Cube.Tests.Services;

/// <summary>
/// 集群配置同步接入测试。覆盖 AddConfigSync 的服务注册、应用标识回退、作用域判断、
/// 跳过字段只读标记，以及无事件总线时宿主服务的启停（退化为定时轮询）。
/// </summary>
public class ConfigSyncTests
{
    /// <summary>测试用配置类型</summary>
    public class SyncTestConfig : Config<SyncTestConfig>
    {
        /// <summary>普通键</summary>
        public String Name { get; set; } = "";

        /// <summary>跳过键</summary>
        public String Secret { get; set; } = "";
    }

    [Fact(DisplayName = "配置同步_注册后解析管理器与宿主服务")]
    public void AddConfigSync_RegistersManagerAndHostedService()
    {
        var sc = new ServiceCollection();
        sc.AddSingleton<ICacheProvider, CacheProvider>();
        sc.AddConfigSync(sync => sync.Add<SyncTestConfig>("SyncTest", ["Secret"]));

        using var sp = sc.BuildServiceProvider();

        var manager = sp.GetService<ConfigSyncManager>();
        Assert.NotNull(manager);
        Assert.Same(manager, sp.GetService<ConfigSyncManager>());

        Assert.True(CubeConfigSyncHelper.IsEnabled(manager, "SyncTest"));
        Assert.True(CubeConfigSyncHelper.IsEnabled(manager, "synctest"));
        Assert.False(CubeConfigSyncHelper.IsEnabled(manager, "Missing"));

        var target = Assert.Single(manager!.Targets);
        Assert.Equal("SyncTest", target.Scope);
        Assert.Contains("Secret", target.Skip, StringComparer.OrdinalIgnoreCase);

        Assert.Single(sp.GetServices<IHostedService>().OfType<CubeConfigSyncHostedService>());
    }

    [Fact(DisplayName = "配置同步_未指定应用标识时取系统名称")]
    public void AddConfigSync_FallsBackToSysConfigName()
    {
        var sc = new ServiceCollection();
        sc.AddConfigSync(sync => sync.Add<SyncTestConfig>("SyncTest"));

        using var sp = sc.BuildServiceProvider();

        var manager = sp.GetRequiredService<ConfigSyncManager>();
        Assert.Equal(SysConfig.Current.Name, manager.AppId);
    }

    [Fact(DisplayName = "配置同步_指定应用标识时保持不变")]
    public void AddConfigSync_KeepsCustomAppId()
    {
        var sc = new ServiceCollection();
        sc.AddConfigSync(sync =>
        {
            sync.AppId = "MyApp";
            sync.Add<SyncTestConfig>("SyncTest");
        });

        using var sp = sc.BuildServiceProvider();

        Assert.Equal("MyApp", sp.GetRequiredService<ConfigSyncManager>().AppId);
    }

    [Fact(DisplayName = "配置同步_跳过字段在配置页标记只读")]
    public void ApplySkipReadOnly_MarksSkipFields()
    {
        var fields = new List<DataField>
        {
            new() { Name = "Name" },
            new() { Name = "Secret" },
            new() { Name = "Sub" },
        };

        // 叶子名与嵌套路径（Sub:Timeout 表示 Sub 字段中某些键跳过）均命中
        CubeConfigSyncHelper.ApplySkipReadOnly(fields, ["Secret", "Sub:Timeout"]);

        Assert.False(fields[0].ReadOnly);
        Assert.True(fields[1].ReadOnly);
        Assert.True(fields[2].ReadOnly);
        Assert.True(CubeConfigSyncHelper.IsSkipped(["Sub:Timeout"], "Sub"));
        Assert.False(CubeConfigSyncHelper.IsSkipped(["Secret"], "Name"));
    }

    [Fact(DisplayName = "配置同步_作用域取文件名去扩展名")]
    public void GetScope_UsesConfigFileName()
    {
        Assert.Equal("XCode", CubeConfigSyncHelper.GetScope(new JsonConfigProvider { FileName = "Config/XCode.config" }));
        Assert.Null(CubeConfigSyncHelper.GetScope(new DbConfigProvider { Category = "Cube" }));
        Assert.Null(CubeConfigSyncHelper.GetScope(null));
    }

    [Fact(DisplayName = "配置同步_无事件总线时宿主服务启停正常")]
    public async Task HostedService_StartStopWithoutBus()
    {
        var sc = new ServiceCollection();
        sc.AddConfigSync(sync => sync.Add<SyncTestConfig>("SyncTest"));

        using var sp = sc.BuildServiceProvider();

        var hosted = sp.GetServices<IHostedService>().OfType<CubeConfigSyncHostedService>().Single();
        await hosted.StartAsync(CancellationToken.None);
        await hosted.StopAsync(CancellationToken.None);
    }

    [Fact(DisplayName = "配置同步_有缓存提供者时宿主服务订阅与退订")]
    public async Task HostedService_StartStopWithBus()
    {
        var sc = new ServiceCollection();
        sc.AddSingleton<ICacheProvider, CacheProvider>();
        sc.AddConfigSync(sync => sync.Add<SyncTestConfig>("SyncTest"));

        using var sp = sc.BuildServiceProvider();

        var hosted = sp.GetServices<IHostedService>().OfType<CubeConfigSyncHostedService>().Single();
        await hosted.StartAsync(CancellationToken.None);
        await hosted.StopAsync(CancellationToken.None);
    }
}

/// <summary>
/// 集群配置同步控制器接线测试。用 SQLite 参数表验证启用同步后配置页读写走合并视图与双写，
/// 跳过字段既不写入参数表，也不会被数据库值覆盖。
/// </summary>
[Collection("TenantAuth")]
public class ConfigSyncControllerTests
{
    private const String AppId = "CubeSyncCase";

    private readonly TenantAuthFixture _fx;

    /// <summary>实例化</summary>
    /// <param name="fixture">多租户认证夹具，复用其 SQLite 库</param>
    public ConfigSyncControllerTests(TenantAuthFixture fixture) => _fx = fixture;

    /// <summary>测试用配置类型</summary>
    public class SyncSetting : Config<SyncSetting>
    {
        /// <summary>普通键</summary>
        public String Name { get; set; } = "";

        /// <summary>跳过键</summary>
        public String Secret { get; set; } = "";

        /// <summary>普通键</summary>
        public Int32 Timeout { get; set; }
    }

    private sealed class SyncSettingController : ConfigController<SyncSetting>
    {
        public SyncSetting GetValue() => Value;

        public void SetValue(SyncSetting value) => Value = value;
    }

    [Fact(DisplayName = "配置同步_配置页双写参数表并显示合并视图")]
    public void ConfigController_SaveAndLoadMergedView()
    {
        var file = Path.Combine(AppContext.BaseDirectory, "Config", "SyncSetting.config");
        var oldProvider = Config<SyncSetting>.Provider;
        var oldCurrent = Config<SyncSetting>.Current;
        try
        {
            Config<SyncSetting>.Provider = new JsonConfigProvider { FileName = file };
            Config<SyncSetting>.Current = new SyncSetting { Name = "node1", Secret = "local1", Timeout = 1 };

            var sc = new ServiceCollection();
            sc.AddSingleton<ICacheProvider, CacheProvider>();
            sc.AddConfigSync(sync =>
            {
                sync.AppId = AppId;
                sync.Add<SyncSetting>("SyncSetting", ["Secret"]);
            });
            using var sp = sc.BuildServiceProvider();

            var http = new DefaultHttpContext { RequestServices = sp };
            var controller = new SyncSettingController
            {
                ControllerContext = new ControllerContext { HttpContext = http },
            };

            // 保存：普通键同时写参数表与本地，跳过键只保留本地值
            controller.SetValue(new SyncSetting { Name = "db", Secret = "remote", Timeout = 9 });

            var category = $"{AppId}-SyncSetting";
            var row = Parameter.FindByUserIdAndCategoryAndName(0, category, "Name");
            Assert.NotNull(row);
            Assert.Equal("db", row!.Value);
            Assert.Null(Parameter.FindByUserIdAndCategoryAndName(0, category, "Secret"));
            Assert.Equal("local1", Config<SyncSetting>.Current.Secret);
            Assert.Equal("db", Config<SyncSetting>.Current.Name);

            // 另一个节点本地仍是旧值，读取时用数据库值覆盖普通键，跳过键保持本地
            Config<SyncSetting>.Current = new SyncSetting { Name = "node2", Secret = "local2", Timeout = 2 };

            var view = controller.GetValue();
            Assert.Equal("db", view.Name);
            Assert.Equal(9, view.Timeout);
            Assert.Equal("local2", view.Secret);
        }
        finally
        {
            Config<SyncSetting>.Provider = oldProvider;
            Config<SyncSetting>.Current = oldCurrent;
            if (File.Exists(file)) File.Delete(file);
            if (File.Exists(file + ".applied")) File.Delete(file + ".applied");
        }
    }
}

