using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using NewLife.Cube.Entity;
using NewLife.Cube.Modules;
using NewLife.Cube.Tests.Membership;
using NewLife.Cube.ViewModels;
using Xunit;

namespace NewLife.Cube.Tests.Common;

/// <summary>
/// BASE-3 模块化插件机制测试。覆盖从应用插件表加载模块/适配器、禁用项跳过、非法类型名容错与加载结果缓存。
/// </summary>
[Collection("TenantAuth")]
public class ModuleManagerTests
{
    private readonly TenantAuthFixture _fx;

    /// <summary>实例化</summary>
    /// <param name="fixture">多租户认证夹具。复用其 SQLite 库（应用插件表）</param>
    public ModuleManagerTests(TenantAuthFixture fixture) => _fx = fixture;


    #region 辅助
    private static String Unique(String prefix) => prefix + Guid.NewGuid().ToString("N")[..8];

    private static AppModule CreateModule(String name, Type type, String kind = "Module", Boolean enable = true)
    {
        var item = new AppModule
        {
            Name = name,
            DisplayName = name,
            Type = kind,
            ClassName = type.FullName,
            FilePath = type.Assembly.Location,
            Enable = enable,
        };
        item.Insert();

        return item;
    }
    #endregion

    [Fact(DisplayName = "模块插件_加载启用模块并可供调用")]
    public void LoadAll_EnabledModule()
    {
        var name = Unique("mod");
        CreateModule(name, typeof(NlMod.M));
        AppModule.Meta.Cache.Clear("插件测试", true);

        var manager = new ModuleManager();
        var modules = manager.LoadAll();

        Assert.True(modules.TryGetValue(name, out var module));
        Assert.IsType<NlMod.M>(module);

        NlMod.M.AddTimes = 0;
        NlMod.M.UseTimes = 0;
        module!.Add(new ServiceCollection());
        module.Use(null, null);
        Assert.Equal(1, NlMod.M.AddTimes);
        Assert.Equal(1, NlMod.M.UseTimes);
    }

    [Fact(DisplayName = "模块插件_禁用与非法类型被跳过")]
    public void LoadAll_SkipsDisabledAndInvalid()
    {
        var disabled = Unique("mod");
        CreateModule(disabled, typeof(NlMod.M), enable: false);

        var invalid = Unique("mod");
        var item = new AppModule
        {
            Name = invalid,
            DisplayName = invalid,
            Type = "Module",
            ClassName = "No.Such.Type.For.Test",
            FilePath = typeof(NlMod.M).Assembly.Location,
            Enable = true,
        };
        item.Insert();
        AppModule.Meta.Cache.Clear("插件测试", true);

        var manager = new ModuleManager();
        var modules = manager.LoadAll();

        Assert.False(modules.ContainsKey(disabled));
        Assert.False(modules.ContainsKey(invalid));
    }

    [Fact(DisplayName = "模块插件_适配器加载并可按名称获取")]
    public void LoadAll_Adapter_GetByName()
    {
        var name = Unique("adp");
        CreateModule(name, typeof(NlMod.A), kind: "Adapter");
        AppModule.Meta.Cache.Clear("插件测试", true);

        var manager = new ModuleManager();
        manager.LoadAll();

        Assert.IsType<NlMod.A>(manager.GetAdapter(name));
        Assert.Null(manager.GetAdapter(Unique("none")));
    }

    [Fact(DisplayName = "模块插件_加载结果被缓存")]
    public void LoadAll_Cached()
    {
        var manager = new ModuleManager();
        var first = manager.LoadAll();
        var second = manager.LoadAll();

        Assert.Same(first, second);
        Assert.Same(manager.Modules, first);
    }
}


