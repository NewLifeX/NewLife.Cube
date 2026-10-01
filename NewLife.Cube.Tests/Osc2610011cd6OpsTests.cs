using System;
using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.Areas.Admin.Controllers;
using Xunit;

namespace NewLife.Cube.Tests;

/// <summary>OSC-2610011cd6：用户在线「强制下线」合成列与 Kick 的 POST 约束</summary>
public class Osc2610011cd6OpsTests
{
    [Fact]
    public void UserOnline_KickField_IsDataAction()
    {
        RuntimeHelpers.RunClassConstructor(typeof(UserOnlineController).TypeHandle);

        var prop = FindListFields(typeof(UserOnlineController));
        Assert.NotNull(prop);
        var fields = prop!.GetValue(null);
        Assert.NotNull(fields);

        Object? kick = null;
        foreach (var item in (IEnumerable)fields!)
        {
            var name = item?.GetType().GetProperty("Name")?.GetValue(item) as String;
            if (name == "Kick")
            {
                kick = item;
                break;
            }
        }

        Assert.NotNull(kick);
        var url = kick!.GetType().GetProperty("Url")?.GetValue(kick) as String;
        var action = kick.GetType().GetProperty("DataAction")?.GetValue(kick) as String;
        Assert.NotNull(url);
        Assert.StartsWith("/Admin/UserOnline/Kick", url);
        Assert.Equal("action", action);
    }

    [Fact]
    public void Kick_IsHttpPost()
    {
        var method = typeof(UserOnlineController).GetMethod("Kick");
        Assert.NotNull(method);
        Assert.NotNull(method!.GetCustomAttribute<HttpPostAttribute>());
    }

    static PropertyInfo? FindListFields(Type type)
    {
        while (type != null)
        {
            var prop = type.GetProperty(
                "ListFields",
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly);
            if (prop != null) return prop;
            type = type.BaseType!;
        }
        return null;
    }
}
