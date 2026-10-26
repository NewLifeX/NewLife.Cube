using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using NewLife.Cube;
using NewLife.Cube.Modules;
using NewLife.Cube.ViewModels;

namespace NlMod;

/// <summary>模块插件机制测试用模块。类型名保持短小，避免超出应用插件表类名长度限制</summary>
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

