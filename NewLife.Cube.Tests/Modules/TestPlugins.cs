using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using NewLife.Cube;
using NewLife.Cube.Modules;
using NewLife.Cube.ViewModels;

namespace NlMod;

/// <summary>模块插件机制测试用模块</summary>
[Module("M")]
public class M : IModule
{
    /// <summary>Add 调用次数</summary>
    public static Int32 AddTimes;

    /// <summary>Use 调用次数</summary>
    public static Int32 UseTimes;

    /// <summary>添加模块</summary>
    /// <param name="services"></param>
    public void Add(IServiceCollection services) => AddTimes++;

    /// <summary>使用模块</summary>
    /// <param name="app"></param>
    /// <param name="env"></param>
    public void Use(IApplicationBuilder app, IWebHostEnvironment env) => UseTimes++;
}

/// <summary>模块插件机制测试用适配器</summary>
[Module("A")]
public class A : IAdapter
{
    /// <summary>编码配置</summary>
    /// <param name="dic"></param>
    /// <param name="fieldCollections"></param>
    /// <returns></returns>
    public Object Encode(Dictionary<String, Object> dic, Dictionary<ViewKinds, FieldCollection> fieldCollections) => dic;

    /// <summary>解码配置</summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public Object Decode(Object obj) => obj;
}

/// <summary>装配顺序记录</summary>
public static class ModuleLog
{
    /// <summary>已添加的模块名称</summary>
    public static List<String> Added { get; } = [];

    /// <summary>已使用的模块名称</summary>
    public static List<String> Used { get; } = [];

    /// <summary>已映射端点的模块数量</summary>
    public static Int32 Mapped;

    /// <summary>重置记录</summary>
    public static void Reset()
    {
        Added.Clear();
        Used.Clear();
        Mapped = 0;
        Broken.Enabled = false;
    }
}

/// <summary>排序测试模块。Order=20，应晚于 OrderB</summary>
[Module("OrderA", Order = 20)]
public class OrderA : IModule
{
    /// <summary>添加模块</summary>
    /// <param name="services"></param>
    public void Add(IServiceCollection services) => ModuleLog.Added.Add("OrderA");

    /// <summary>使用模块</summary>
    /// <param name="app"></param>
    /// <param name="env"></param>
    public void Use(IApplicationBuilder app, IWebHostEnvironment env) => ModuleLog.Used.Add("OrderA");
}

/// <summary>排序测试模块。Order=10，应早于 OrderA</summary>
[Module("OrderB", Order = 10)]
public class OrderB : IModule
{
    /// <summary>添加模块</summary>
    /// <param name="services"></param>
    public void Add(IServiceCollection services) => ModuleLog.Added.Add("OrderB");

    /// <summary>使用模块</summary>
    /// <param name="app"></param>
    /// <param name="env"></param>
    public void Use(IApplicationBuilder app, IWebHostEnvironment env) => ModuleLog.Used.Add("OrderB");
}

/// <summary>异常隔离测试模块。启用后 Add/Use 抛异常</summary>
[Module("Broken")]
public class Broken : IModule
{
    /// <summary>是否启用异常行为</summary>
    public static Boolean Enabled;

    /// <summary>添加模块</summary>
    /// <param name="services"></param>
    public void Add(IServiceCollection services)
    {
        if (Enabled) throw new InvalidOperationException("broken module");
    }

    /// <summary>使用模块</summary>
    /// <param name="app"></param>
    /// <param name="env"></param>
    public void Use(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (Enabled) throw new InvalidOperationException("broken module");
    }
}

/// <summary>端点映射测试模块</summary>
[Module("Endpoint")]
public class Endpoint : IModule, IModuleEndpoint
{
    /// <summary>添加模块</summary>
    /// <param name="services"></param>
    public void Add(IServiceCollection services) { }

    /// <summary>使用模块</summary>
    /// <param name="app"></param>
    /// <param name="env"></param>
    public void Use(IApplicationBuilder app, IWebHostEnvironment env) { }

    /// <summary>映射端点</summary>
    /// <param name="endpoints"></param>
    public void Map(IEndpointRouteBuilder endpoints) => ModuleLog.Mapped++;
}

/// <summary>重名测试模块一</summary>
[Module("Dup")]
public class Dup1 : IModule
{
    /// <summary>添加模块</summary>
    /// <param name="services"></param>
    public void Add(IServiceCollection services) { }

    /// <summary>使用模块</summary>
    /// <param name="app"></param>
    /// <param name="env"></param>
    public void Use(IApplicationBuilder app, IWebHostEnvironment env) { }
}

/// <summary>重名测试模块二</summary>
[Module("Dup")]
public class Dup2 : IModule
{
    /// <summary>添加模块</summary>
    /// <param name="services"></param>
    public void Add(IServiceCollection services) { }

    /// <summary>使用模块</summary>
    /// <param name="app"></param>
    /// <param name="env"></param>
    public void Use(IApplicationBuilder app, IWebHostEnvironment env) { }
}

/// <summary>显式注册测试模块</summary>
[Module("Explicit")]
public class ExplicitModule : IModule
{
    /// <summary>添加模块</summary>
    /// <param name="services"></param>
    public void Add(IServiceCollection services) { }

    /// <summary>使用模块</summary>
    /// <param name="app"></param>
    /// <param name="env"></param>
    public void Use(IApplicationBuilder app, IWebHostEnvironment env) { }
}

/// <summary>管道顺序测试模块。启用后注册中间件与端点，用于验证 Use 在路由之前、Map 在回退之前</summary>
[Module("Pipeline")]
public class PipelineModule : IModule, IModuleEndpoint
{
    /// <summary>是否启用管道行为</summary>
    public static Boolean Enabled;

    /// <summary>添加模块</summary>
    /// <param name="services"></param>
    public void Add(IServiceCollection services) { }

    /// <summary>使用模块</summary>
    /// <param name="app"></param>
    /// <param name="env"></param>
    public void Use(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (!Enabled || app == null) return;

        app.Use(async (context, next) =>
        {
            context.Response.Headers["X-Cube-Module"] = "cube";
            await next();
        });
    }

    /// <summary>映射端点</summary>
    /// <param name="endpoints"></param>
    public void Map(IEndpointRouteBuilder endpoints)
    {
        if (Enabled) endpoints.MapGet("/module/ping", () => "pong");
    }
}

