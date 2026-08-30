using System;
using System.Collections.Generic;
using NewLife.Cube.Widgets;
using Xunit;

namespace NewLife.Cube.Tests.Widgets;

/// <summary>KPI 部件测试（源码已迁移至 API 侧 NewLife.Cube/Widgets）。覆盖扫描、元数据、权限过滤与数据契约（不依赖数据库）</summary>
public class KpiWidgetTests
{
    private readonly WidgetManager _manager = new();

    /// <summary>全部内置 KPI 部件名（系统 6 + 个人 2）</summary>
    private static readonly String[] KpiNames = ["UserCount", "CpuRate", "OnlineCount", "TodayLogin", "Log24h", "Error24h", "MyDays", "MyLogins"];

    [Fact(DisplayName = "扫描发现内置 KPI 部件（迁移至 API 侧后仍在编译）")]
    public void Scan_FindsKpiWidgets()
    {
        var dic = _manager.Scan();

        foreach (var name in KpiNames)
            Assert.True(dic.ContainsKey(name), $"缺少 KPI 部件 {name}");
    }

    [Fact(DisplayName = "KPI 部件元数据完整且为小卡宽度")]
    public void KpiWidgets_Metadata()
    {
        var dic = _manager.Scan();

        foreach (var name in KpiNames)
        {
            var info = dic[name];
            Assert.Equal(WidgetTypes.Kpi, info.WidgetType);
            Assert.False(info.Title.IsNullOrEmpty(), $"{name} 缺少标题");
            Assert.False(info.Icon.IsNullOrEmpty(), $"{name} 缺少图标");
            Assert.False(info.Color.IsNullOrEmpty(), $"{name} 缺少配色");
            // KPI 小卡宽度约束：Cols <= 3
            Assert.True(info.Cols <= 3, $"{name} Cols={info.Cols} 应小于等于3（KPI 小卡）");
        }
    }

    [Fact(DisplayName = "系统 KPI AdminOnly 仅管理员可见，个人 KPI 所有用户可见")]
    public void AdminOnly_Filtering()
    {
        var dic = _manager.Scan();
        var normalRole = new List<String> { "普通用户" };

        // 系统 KPI：管理员专属
        foreach (var name in new[] { "UserCount", "CpuRate", "OnlineCount", "TodayLogin", "Log24h", "Error24h" })
        {
            Assert.True(dic[name].AdminOnly, $"{name} 应为 AdminOnly");
            Assert.False(_manager.IsVisible(dic[name], normalRole, false), $"普通用户不应看到 {name}");
            Assert.True(_manager.IsVisible(dic[name], normalRole, true), $"管理员应看到 {name}");
        }

        // 个人 KPI：所有登录用户可见
        foreach (var name in new[] { "MyDays", "MyLogins" })
        {
            Assert.False(dic[name].AdminOnly, $"{name} 不应为 AdminOnly");
            Assert.True(_manager.IsVisible(dic[name], normalRole, false), $"普通用户应看到 {name}");
        }
    }

    [Fact(DisplayName = "无数据库依赖的 KPI 部件 GetData 返回 Value/Trend/Url 契约")]
    public void KpiWidgets_ReturnDataContract()
    {
        var dic = _manager.Scan();

        // 仅验证不依赖数据库的部件（CpuRate 用 MachineInfo，MyDays/MyLogins 空用户容错），
        // 避免测试环境无 Membership 连接导致 User/XLog 查询异常
        foreach (var name in new[] { "CpuRate", "MyDays", "MyLogins" })
        {
            var data = _manager.GetData(dic[name]);
            Assert.NotNull(data);

            var type = data!.GetType();
            Assert.NotNull(type.GetProperty("Value"));
            Assert.NotNull(type.GetProperty("Trend"));
            Assert.NotNull(type.GetProperty("Url"));
        }
    }

    [Fact(DisplayName = "KPI 部件名去掉 Widget 后缀为默认名称")]
    public void KpiWidgets_DefaultName()
    {
        var dic = _manager.Scan();

        // 特性 Name 与类型名去 Widget 后缀一致（无自定义 Name 时 WidgetManager 兜底）
        foreach (var name in KpiNames)
        {
            var info = dic[name];
            Assert.Equal(name, info.Name);
        }
    }
}
