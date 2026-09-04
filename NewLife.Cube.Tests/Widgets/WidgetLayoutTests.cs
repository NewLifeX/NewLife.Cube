using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NewLife.Cube.Widgets;
using XCode.DataAccessLayer;
using XCode.Membership;
using Xunit;

namespace NewLife.Cube.Tests.Widgets;

/// <summary>工作台布局/分组测试集合。夹具一次性构建 SQLite 库（Membership 连接），测试共享同一数据源。</summary>
[CollectionDefinition("WidgetLayout", DisableParallelization = true)]
public class WidgetLayoutCollection : ICollectionFixture<WidgetLayoutFixture>
{
}

/// <summary>
/// 工作台布局测试夹具：独立 SQLite 连接（Parameter 实体重映射），自动迁移建表。
/// 使用独立连接名绕开 Membership 连接的 DAL 全局缓存（跨测试集合污染根因）。
/// </summary>
public class WidgetLayoutFixture : IDisposable
{
    /// <summary>独立连接名。避免与其它测试集合共享的 Membership 连接缓存冲突</summary>
    public const String ConnName = "WidgetLayout";

    /// <summary>数据库文件</summary>
    public String DbFile { get; }

    public WidgetLayoutFixture()
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "Data");
        Directory.CreateDirectory(dir);
        DbFile = Path.Combine(dir, "WidgetLayoutTests.db");

        // 清理历史库文件（含 WAL/SHM 残留），保证每次运行干净，避免上一轮数据导致 UNIQUE 冲突
        foreach (var f in Directory.GetFiles(dir, "WidgetLayoutTests.db*"))
        {
            try { File.Delete(f); } catch { }
        }

        // 注册独立连接；实体重映射在测试类构造函数按测试线程执行（Meta.ConnName 线程级）
        DAL.AddConnStr(ConnName, $"Data Source={DbFile}", null, "SQLite");
    }

    public void Dispose()
    {
        // 释放连接、移除独立连接，避免影响其它测试集合
        try { DAL.Create(ConnName).Reset(); } catch { }
        DAL.ConnStrs?.TryRemove(ConnName, out _);

        // 清理库文件（含 WAL/SHM 残留），避免下次运行脏数据
        try
        {
            foreach (var f in Directory.GetFiles(Path.GetDirectoryName(DbFile)!, "WidgetLayoutTests.db*"))
            {
                File.Delete(f);
            }
        }
        catch
        {
            // 忽略清理失败
        }
    }
}

/// <summary>纯排序逻辑测试。验证组顺序聚合与用户布局覆盖，不依赖数据库</summary>
public class WidgetOrderTests
{
    private static WidgetAttribute W(String name, String category, Int32 sort, WidgetTypes type = WidgetTypes.Content, Type impl = null) =>
        new(name, name) { Category = category, Sort = sort, WidgetType = type, Type = impl };

    private static readonly IList<String> DefaultGroups = new List<String> { "系统", "个人", "通用" };

    [Fact(DisplayName = "OrderWidgets_无布局_按组顺序聚合且组内排序")]
    public void OrderWidgets_Default_Grouped()
    {
        var widgets = new List<WidgetAttribute>
        {
            W("A", "系统", 20),
            W("B", "系统", 10),
            W("C", "个人", 5),
            W("D", "IoT", 1),
            W("E", "IoT", 2),
            W("F", "通用", 1),
            W("G", "订单", 3),
        };

        var rs = WidgetManager.OrderWidgets(widgets, null, DefaultGroups);

        // 已配置组在前（系统 < 个人 < 通用），组内按 Sort；未配置组（IoT/订单）排最后按名称
        Assert.Equal(new[] { "B", "A", "C", "F", "D", "E", "G" }, rs.Select(e => e.Name).ToArray());
    }

    [Fact(DisplayName = "OrderWidgets_无布局_空分类归入通用组")]
    public void OrderWidgets_Default_NullCategory()
    {
        var widgets = new List<WidgetAttribute>
        {
            W("A", null!, 1),
            W("B", "通用", 2),
        };

        var rs = WidgetManager.OrderWidgets(widgets, null, DefaultGroups);

        // 空分类按"通用"处理，与通用组聚合
        Assert.Equal(new[] { "A", "B" }, rs.Select(e => e.Name).ToArray());
    }

    [Fact(DisplayName = "OrderWidgets_有布局_已知按布局序且未覆盖按默认位插入")]
    public void OrderWidgets_WithLayout_KnownFirstThenUnknown()
    {
        var widgets = new List<WidgetAttribute>
        {
            W("A", "系统", 20),
            W("B", "系统", 10),
            W("C", "个人", 5),
            W("D", "IoT", 1),
            W("E", "IoT", 2),
            W("F", "通用", 1),
            W("G", "订单", 3),
        };

        // 用户拖拽后：B 第一、C 第二、F 第三；未覆盖的 A/D/E/G 按默认位插入（A 默认序在 B 后 C 前 → 插到 C 前），不一律追加末尾
        var layout = new Dictionary<String, WidgetLayout>
        {
            ["B"] = new() { Sort = 0 },
            ["C"] = new() { Sort = 1 },
            ["F"] = new() { Sort = 2 },
        };

        var rs = WidgetManager.OrderWidgets(widgets, layout, DefaultGroups);

        Assert.Equal(new[] { "B", "A", "C", "F", "D", "E", "G" }, rs.Select(e => e.Name).ToArray());
    }

    [Fact(DisplayName = "OrderWidgets_有布局_隐藏项不参与排序但保留在布局")]
    public void OrderWidgets_WithLayout_HiddenKeptInLayout()
    {
        var widgets = new List<WidgetAttribute>
        {
            W("A", "系统", 10),
            W("B", "系统", 20),
        };

        var layout = new Dictionary<String, WidgetLayout>
        {
            ["A"] = new() { Sort = 0, Hide = true },
            ["B"] = new() { Sort = 1 },
        };

        // OrderWidgets 只负责排序，隐藏过滤在 GetWidgets；布局中隐藏项仍保留
        var rs = WidgetManager.OrderWidgets(widgets, layout, DefaultGroups);

        Assert.Equal(new[] { "A", "B" }, rs.Select(e => e.Name).ToArray());
        Assert.True(layout["A"].Hide);
    }

    [Fact(DisplayName = "OrderWidgets_组内配置_按配置排序且未列出追加末尾")]
    public void OrderWidgets_WithGroupItems_ConfiguredOrder()
    {
        var widgets = new List<WidgetAttribute>
        {
            W("A", "系统", 10),
            W("B", "系统", 20),
            W("C", "系统", 30),
            W("D", "个人", 5),
        };

        // 管理员配置系统组顺序：C 第一、A 第二；B 未列出 → 追加组内末尾
        var groupItems = new Dictionary<String, IList<String>>
        {
            ["系统"] = new List<String> { "C", "A" },
        };

        var rs = WidgetManager.OrderWidgets(widgets, null, DefaultGroups, groupItems);

        Assert.Equal(new[] { "C", "A", "B", "D" }, rs.Select(e => e.Name).ToArray());
    }

    [Fact(DisplayName = "OrderWidgets_组内配置_仅影响配置组其它组回退Sort")]
    public void OrderWidgets_WithGroupItems_OtherGroupFallsBackToSort()
    {
        var widgets = new List<WidgetAttribute>
        {
            W("A", "系统", 20),
            W("B", "系统", 10),
            W("C", "个人", 5),
            W("D", "个人", 3),
        };

        // 只配置了系统组；个人组无配置 → 按 Sort
        var groupItems = new Dictionary<String, IList<String>>
        {
            ["系统"] = new List<String> { "A", "B" },
        };

        var rs = WidgetManager.OrderWidgets(widgets, null, DefaultGroups, groupItems);

        Assert.Equal(new[] { "A", "B", "D", "C" }, rs.Select(e => e.Name).ToArray());
    }

    [Fact(DisplayName = "OrderWidgets_无布局_KPI卡恒在内容卡前")]
    public void OrderWidgets_Default_KpiBeforeContent()
    {
        // 内容卡属于已配置组“系统”且 Sort 更小，KPI 卡属于“通用”；KPI 簇优先仍应排前
        var widgets = new List<WidgetAttribute>
        {
            W("Content", "系统", 1, WidgetTypes.Content),
            W("Kpi", "通用", 0, WidgetTypes.Kpi),
        };

        var rs = WidgetManager.OrderWidgets(widgets, null, DefaultGroups);

        Assert.Equal(new[] { "Kpi", "Content" }, rs.Select(e => e.Name).ToArray());
    }

    [Fact(DisplayName = "OrderWidgets_无布局_同簇内外部业务组件在魔方内置前")]
    public void OrderWidgets_Default_ExternalBeforeBuiltin()
    {
        // 外部组件（Type 指向其它程序集）与内置组件同簇时业务优先；KPI 簇仍整体在内容区前
        var widgets = new List<WidgetAttribute>
        {
            W("CubeKpi", "系统", 10, WidgetTypes.Kpi),
            W("BizKpi", "系统", 5, WidgetTypes.Kpi, typeof(WidgetOrderTests)),
            W("BizContent", "通用", 1, WidgetTypes.Content, typeof(WidgetOrderTests)),
            W("CubeContent", "通用", 1, WidgetTypes.Content),
        };

        var rs = WidgetManager.OrderWidgets(widgets, null, DefaultGroups);

        // KPI 簇：BizKpi（外部）→ CubeKpi（内置）；内容区：BizContent（外部）→ CubeContent（内置）
        Assert.Equal(new[] { "BizKpi", "CubeKpi", "BizContent", "CubeContent" }, rs.Select(e => e.Name).ToArray());
    }

    [Fact(DisplayName = "OrderWidgets_外部判定_Type为空视为内置")]
    public void OrderWidgets_External_NullTypeIsBuiltin()
    {
        // 手工构造（Type 为空）默认按内置处理，不因来源判定抛异常
        var widgets = new List<WidgetAttribute>
        {
            W("A", "系统", 10, WidgetTypes.Kpi),
            W("B", "系统", 20, WidgetTypes.Kpi, typeof(WidgetOrderTests)),
        };

        var rs = WidgetManager.OrderWidgets(widgets, null, DefaultGroups);

        Assert.Equal(new[] { "B", "A" }, rs.Select(e => e.Name).ToArray());
    }

    [Fact(DisplayName = "OrderWidgets_有布局_业务新KPI插到已知KPI簇之前")]
    public void OrderWidgets_WithLayout_NewBusinessKpiFloatsUp()
    {
        var widgets = new List<WidgetAttribute>
        {
            W("C1", "系统", 10, WidgetTypes.Kpi),
            W("C2", "系统", 20, WidgetTypes.Kpi),
            W("M", "通用", 1, WidgetTypes.Content),
            W("Biz", "系统", 30, WidgetTypes.Kpi, typeof(WidgetOrderTests)), // 新增业务 KPI
        };

        // 用户已拖 C1/C2/M；Biz 未覆盖 → 外部 KPI 默认排最前，应插到 C1 之前
        var layout = new Dictionary<String, WidgetLayout>
        {
            ["C1"] = new() { Sort = 0 },
            ["C2"] = new() { Sort = 1 },
            ["M"] = new() { Sort = 2 },
        };

        var rs = WidgetManager.OrderWidgets(widgets, layout, DefaultGroups);

        Assert.Equal(new[] { "Biz", "C1", "C2", "M" }, rs.Select(e => e.Name).ToArray());
    }

    [Fact(DisplayName = "OrderWidgets_有布局_魔方新KPI插到自然位且用户拖序不变")]
    public void OrderWidgets_WithLayout_NewBuiltinKpiNaturalSlot()
    {
        var widgets = new List<WidgetAttribute>
        {
            W("C1", "系统", 10, WidgetTypes.Kpi),
            W("C2", "系统", 20, WidgetTypes.Kpi),
            W("C3", "系统", 30, WidgetTypes.Kpi), // 新增魔方 KPI
            W("C4", "系统", 50, WidgetTypes.Kpi),
            W("M", "通用", 1, WidgetTypes.Content),
        };

        // 用户拖拽过：C1 第一、C4 第二、C2 第三、M 第四（把 C4 提到 C2 前）
        var layout = new Dictionary<String, WidgetLayout>
        {
            ["C1"] = new() { Sort = 0 },
            ["C4"] = new() { Sort = 1 },
            ["C2"] = new() { Sort = 2 },
            ["M"] = new() { Sort = 3 },
        };

        var rs = WidgetManager.OrderWidgets(widgets, layout, DefaultGroups);

        // C3（Sort=30）默认位在 C1(10) 与 C4(50) 之间 → 插到 C4 前；已拖的 C2 保持在 C4 之后不动
        Assert.Equal(new[] { "C1", "C3", "C4", "C2", "M" }, rs.Select(e => e.Name).ToArray());
    }
}

/// <summary>工作台布局存储集成测试（Parameter 表，SQLite）。覆盖布局保存/读取、旧数据回退、分组顺序、隐藏过滤</summary>
[Collection("WidgetLayout")]
public class WidgetLayoutTests : IDisposable
{
    private readonly WidgetManager _manager = new();
    private readonly String _oldConn;

    /// <summary>每个测试实例在自己的执行线程上重映射 Parameter 到独立连接。
    /// Meta.ConnName 是线程级配置，fixture 线程设置不作用于测试线程，故须在测试构造函数中设置</summary>
    public WidgetLayoutTests()
    {
        _oldConn = Parameter.Meta.ConnName;
        Parameter.Meta.ConnName = WidgetLayoutFixture.ConnName;
    }

    public void Dispose()
    {
        // 恢复实体连接映射，避免影响其它测试集合
        Parameter.Meta.ConnName = _oldConn;
    }

    [Fact(DisplayName = "SaveLayout_GetLayout_排序隐藏往返一致")]
    public void SaveLayout_Roundtrip()
    {
        const Int32 userId = 101;

        var layout = new Dictionary<String, WidgetLayout>
        {
            ["Profile"] = new() { Sort = 3 },
            ["QuickLink"] = new() { Sort = 1, Hide = true },
        };
        _manager.SaveLayout(userId, layout);

        var rs = _manager.GetLayout(userId);
        Assert.Equal(2, rs.Count);
        Assert.Equal(3, rs["Profile"].Sort);
        Assert.False(rs["Profile"].Hide);
        Assert.Equal(1, rs["QuickLink"].Sort);
        Assert.True(rs["QuickLink"].Hide);
    }

    [Fact(DisplayName = "GetLayout_无布局行_回退兼容旧Order逐行数据")]
    public void GetLayout_LegacyOrderFallback()
    {
        const Int32 userId = 102;

        // 写入旧格式逐行排序（分类 Widget.Order）
        new Parameter { UserID = userId, Category = WidgetManager.OrderCategory, Name = "Profile", Value = "5" }.Save();
        new Parameter { UserID = userId, Category = WidgetManager.OrderCategory, Name = "QuickLink", Value = "2" }.Save();

        var rs = _manager.GetLayout(userId);
        Assert.Equal(2, rs.Count);
        Assert.Equal(5, rs["Profile"].Sort);
        Assert.Equal(2, rs["QuickLink"].Sort);
        Assert.False(rs["QuickLink"].Hide);
    }

    [Fact(DisplayName = "GetGroupOrder_未配置_内置约定")]
    public void GetGroupOrder_Default()
    {
        // 清理全局配置，验证内置约定
        var p = Parameter.FindByUserIDAndCategoryAndName(0, WidgetManager.GroupCategory, "Order");
        p?.Delete();

        var rs = _manager.GetGroupOrder();
        Assert.Equal(new[] { "系统", "个人", "通用" }, rs);
    }

    [Fact(DisplayName = "SetGroupOrder_GetGroupOrder_往返一致")]
    public void SetGroupOrder_Roundtrip()
    {
        _manager.SetGroupOrder(new[] { "系统", "IoT", "个人", "订单" });

        var rs = _manager.GetGroupOrder();
        Assert.Equal(new[] { "系统", "IoT", "个人", "订单" }, rs);
    }

    [Fact(DisplayName = "GetWidgets_布局重排并隐藏用户卡片")]
    public void GetWidgets_ReorderAndHide()
    {
        const Int32 userId = 103;

        // 基线顺序：全部可见组件（isAdmin=true 忽略 AdminOnly 过滤）
        var names = _manager.GetWidgets(null, true, 0).Select(e => e.Name).ToList();
        Assert.True(names.Count >= 2, "测试环境至少应有 2 个可见组件");

        var a = names[0];
        var b = names[1];

        // 保存布局：b 排第一，a 隐藏
        var layout = new Dictionary<String, WidgetLayout>
        {
            [b] = new() { Sort = 0 },
            [a] = new() { Sort = 1, Hide = true },
        };
        _manager.SaveLayout(userId, layout);

        var rs = _manager.GetWidgets(null, true, userId).Select(e => e.Name).ToList();
        Assert.Equal(b, rs[0]);
        Assert.DoesNotContain(a, rs);
        Assert.Equal(names.Count - 1, rs.Count);

        // 已隐藏列表包含 a，不含 b
        var hidden = _manager.GetHiddenWidgets(userId, null, true).Select(e => e.Name).ToList();
        Assert.Contains(a, hidden);
        Assert.DoesNotContain(b, hidden);
    }

    [Fact(DisplayName = "ResetLayout_清除布局恢复默认")]
    public void ResetLayout_Clears()
    {
        const Int32 userId = 104;

        _manager.SaveLayout(userId, new Dictionary<String, WidgetLayout> { ["Profile"] = new() { Sort = 0 } });
        Assert.NotEmpty(_manager.GetLayout(userId));

        _manager.ResetLayout(userId);
        Assert.Empty(_manager.GetLayout(userId));
    }

    [Fact(DisplayName = "SetGroupItemOrder_GetGroupItemOrder_往返一致")]
    public void SetGroupItemOrder_Roundtrip()
    {
        _manager.SetGroupItemOrder("系统", new[] { "LoginLog", "SysInfo", "UserCount" });

        var rs = _manager.GetGroupItemOrder("系统");
        Assert.Equal(new[] { "LoginLog", "SysInfo", "UserCount" }, rs);
    }

    [Fact(DisplayName = "GetGroupItemOrder_未配置_返回null")]
    public void GetGroupItemOrder_Unconfigured_ReturnsNull()
    {
        Assert.Null(_manager.GetGroupItemOrder("不存在的组"));
    }

    [Fact(DisplayName = "GetWidgets_组内配置覆盖默认Sort顺序")]
    public void GetWidgets_GroupItemOrder_Applied()
    {
        // 清理可能存在的组内配置（同集合其它用例可能写入），保证默认断言自包含
        Parameter.FindByUserIDAndCategoryAndName(0, WidgetManager.GroupCategory, "系统")?.Delete();

        // 系统组默认按 Sort：SysInfo(100) 在 LoginLog(120) 前
        var def = _manager.GetWidgets(null, true, 0)
            .Where(e => e.Category == "系统")
            .Select(e => e.Name)
            .ToList();
        Assert.Contains("SysInfo", def);
        Assert.Contains("LoginLog", def);
        Assert.True(def.IndexOf("SysInfo") < def.IndexOf("LoginLog"), "默认系统组内 SysInfo 应在 LoginLog 前");

        // 配置系统组内顺序：LoginLog 在前
        _manager.SetGroupItemOrder("系统", new[] { "LoginLog", "SysInfo" });

        var rs = _manager.GetWidgets(null, true, 0)
            .Where(e => e.Category == "系统")
            .Select(e => e.Name)
            .ToList();
        Assert.True(rs.IndexOf("LoginLog") < rs.IndexOf("SysInfo"), "配置后 LoginLog 应在 SysInfo 前");

        // 清理全局配置，避免影响其它用例
        Parameter.FindByUserIDAndCategoryAndName(0, WidgetManager.GroupCategory, "系统")?.Delete();
    }
}
