using System;
using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using NewLife.Cube.Areas.Admin.Controllers;
using NewLife.Cube.Workflow;
using Xunit;

namespace NewLife.Cube.Tests;

/// <summary>OSC-261001909b：租户成员合成列与流程通知 Target 拼装</summary>
public class Osc261001909bTests
{
    [Fact]
    public void Tenant_MembersField_IsNavLinkWithoutDataAction()
    {
        RuntimeHelpers.RunClassConstructor(typeof(TenantController).TypeHandle);

        var prop = FindListFields(typeof(TenantController));
        Assert.NotNull(prop);
        var fields = prop!.GetValue(null);
        Assert.NotNull(fields);

        Object? members = null;
        foreach (var item in (IEnumerable)fields!)
        {
            var name = item?.GetType().GetProperty("Name")?.GetValue(item) as String;
            if (name == "Members")
            {
                members = item;
                break;
            }
        }

        Assert.NotNull(members);
        var url = members!.GetType().GetProperty("Url")?.GetValue(members) as String;
        var action = members.GetType().GetProperty("DataAction")?.GetValue(members) as String;
        var display = members.GetType().GetProperty("DisplayName")?.GetValue(members) as String;
        Assert.Equal("成员", display);
        Assert.Equal("/Admin/TenantUser?tenantId={Id}", url);
        Assert.True(String.IsNullOrEmpty(action));
    }

    [Fact]
    public void BuildNotifyTarget_RequiresBothParts()
    {
        Assert.Null(WorkflowEngine.BuildNotifyTarget(null, "9"));
        Assert.Null(WorkflowEngine.BuildNotifyTarget("Admin/User", null));
        Assert.Null(WorkflowEngine.BuildNotifyTarget("", "9"));
        Assert.Null(WorkflowEngine.BuildNotifyTarget("Admin/User", ""));
        Assert.Equal("Admin/User#9", WorkflowEngine.BuildNotifyTarget("Admin/User", "9"));
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
