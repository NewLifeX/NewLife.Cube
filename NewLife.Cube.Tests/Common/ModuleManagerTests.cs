using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using NewLife.Cube.Modules;
using Xunit;

namespace NewLife.Cube.Tests.Common;

/// <summary>
/// BASE-3 模块化插件机制测试。覆盖程序集自动发现、显式注册、装配顺序、重名去重、异常隔离、
/// 端点映射、加载结果缓存与默认适配器可用。
/// </summary>
public class ModuleManagerTests
{
    [Fact(DisplayName = "模块插件_自动发现已引用程序集")]
    public void Load_DiscoversModules()
    {
        var manager = new ModuleManager();
        var modules = manager.Load();

        Assert.True(modules.ContainsKey("M"));
        Assert.IsType<NlMod.M>(modules["M"]);
        Assert.True(modules.ContainsKey("OrderA"));
        Assert.True(modules.ContainsKey("Endpoint"));
    }

    [Fact(DisplayName = "模块插件_显式注册被合并且不重复")]
    public void Load_ExplicitRegistration()
    {
        var services = new ServiceCollection();
        services.AddCubeModule<NlMod.ExplicitModule>();

        var manager = new ModuleManager();
        var modules = manager.Load(services);

        Assert.True(modules.ContainsKey("Explicit"));
        Assert.IsType<NlMod.ExplicitModule>(modules["Explicit"]);
        Assert.Single(modules.Keys, e => e.EqualIgnoreCase("Explicit"));
    }

    [Fact(DisplayName = "模块插件_按Order升序装配")]
    public void Add_FollowsOrder()
    {
        NlMod.ModuleLog.Reset();

        var manager = new ModuleManager();
        manager.Add(new ServiceCollection());

        var a = NlMod.ModuleLog.Added.IndexOf("OrderA");
        var b = NlMod.ModuleLog.Added.IndexOf("OrderB");
        Assert.True(a >= 0 && b >= 0);
        Assert.True(b < a, $"OrderB(10) 应早于 OrderA(20)，实际顺序 {NlMod.ModuleLog.Added.Join(",")}");
    }

    [Fact(DisplayName = "模块插件_重名只保留一个")]
    public void Load_DuplicateName_KeepsOne()
    {
        var manager = new ModuleManager();
        var modules = manager.Load();

        Assert.Single(modules.Keys, e => e.EqualIgnoreCase("Dup"));
    }

    [Fact(DisplayName = "模块插件_单模块异常不影响其它模块")]
    public void Add_IsolatesFailure()
    {
        NlMod.ModuleLog.Reset();
        NlMod.M.AddTimes = 0;
        NlMod.Broken.Enabled = true;

        var manager = new ModuleManager();
        manager.Add(new ServiceCollection());

        Assert.Equal(1, NlMod.M.AddTimes);
        Assert.Contains("OrderA", NlMod.ModuleLog.Added);
    }

    [Fact(DisplayName = "模块插件_Use与Map被调用")]
    public void UseAndMap_InvokeModules()
    {
        NlMod.ModuleLog.Reset();
        NlMod.M.UseTimes = 0;

        var manager = new ModuleManager();
        manager.Use(null!, null!);
        manager.Map(null!);

        Assert.Equal(1, NlMod.M.UseTimes);
        Assert.Equal(1, NlMod.ModuleLog.Mapped);
    }

    [Fact(DisplayName = "模块插件_加载结果被缓存")]
    public void Load_Cached()
    {
        var manager = new ModuleManager();
        var first = manager.Load();
        var second = manager.Load();

        Assert.Same(first, second);
        Assert.Same(manager.Modules, first);
    }

    [Fact(DisplayName = "模块插件_适配器自动发现且默认适配器始终可用")]
    public void Adapters_Discovered()
    {
        var manager = new ModuleManager();

        Assert.IsType<NlMod.A>(manager.GetAdapter("A"));
        Assert.IsType<DefaultAdapter>(manager.GetAdapter("Default"));
        Assert.Null(manager.GetAdapter("none"));
        Assert.Null(manager.GetAdapter(null));
    }

    [Fact(DisplayName = "模块插件_扫描结果包含测试模块")]
    public void ScanAllModules_FindsTestModules()
    {
        var map = ModuleManager.ScanAllModules();

        Assert.True(map.ContainsKey("M"));
        Assert.True(map.ContainsKey("OrderA"));
        Assert.True(map.ContainsKey("Endpoint"));
    }
}


