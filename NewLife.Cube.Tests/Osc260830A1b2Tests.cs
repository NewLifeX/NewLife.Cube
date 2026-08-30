using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using NewLife.Cube;
using NewLife.Cube.Automation;
using NewLife.Remoting;
using XCode;
using XCode.DataAccessLayer;
using Xunit;

namespace NewLife.Cube.Tests;

/// <summary>OSC-260830a1b2 测试实体：字段白名单 / startsWith / 复杂度上限 用</summary>
[DisplayName("OSC-260830a1b2 查询实体")]
[BindTable("Osc260830A1b2Item", ConnName = "Cube", DbType = DatabaseType.None)]
public class Osc260830A1b2Item : Entity<Osc260830A1b2Item>
{
    [DisplayName("编号")]
    [DataObjectField(true, true, false, 0)]
    public virtual Int32 Id { get; set; }

    [DisplayName("名称")]
    [DataObjectField(false, false, true, 50)]
    public virtual String Name { get; set; }

    [DisplayName("启用")]
    [DataObjectField(false, false, false, 0)]
    public virtual Boolean Enable { get; set; }
}

/// <summary>OSC-260830a1b2 时间窗测试实体：名称含 Log 且含 CreateTime 字段</summary>
[DisplayName("OSC-260830a1b2 日志实体（时间窗）")]
[BindTable("Osc260830A1b2Log", ConnName = "Cube", DbType = DatabaseType.None)]
public class Osc260830A1b2Log : Entity<Osc260830A1b2Log>
{
    [DisplayName("编号")]
    [DataObjectField(true, true, false, 0)]
    public virtual Int32 Id { get; set; }

    [DisplayName("创建时间")]
    [DataObjectField(false, false, false, 0)]
    public virtual DateTime CreateTime { get; set; }
}

/// <summary>OSC-260830a1b2 查询收口与筛选服务端化：白名单 400 / startsWith / 复杂度上限</summary>
public class Osc260830A1b2Tests
{
    static ViewFilterDto F(String logic, params ViewFilterConditionDto[] conds) => new() { Logic = logic, Conditions = [.. conds] };

    static ViewFilterConditionDto C(String field, String op, Object value = null) => new() { Field = field, Op = op, Value = value };

    static Func<String, Boolean> Allow(params String[] names)
    {
        var set = new HashSet<String>(names, StringComparer.OrdinalIgnoreCase);
        return n => set.Contains(n);
    }

    [Fact(DisplayName = "白名单外字段 → 400，不静默放弃")]
    public void TryBuildWhere_Whitelist_Throws400()
    {
        var fact = EntityFactory.CreateFactory(typeof(Osc260830A1b2Item));
        var allowed = Allow("Name", "Enable");

        // 白名单内可下推
        var exp = AutomationFilter.TryBuildWhere(fact, F("all", C("Name", "eq", "x")), allowed);
        Assert.NotNull(exp);

        // 白名单外（未下发列）→ 400
        var ex = Assert.Throws<ApiException>(() => AutomationFilter.TryBuildWhere(fact, F("all", C("Salary", "eq", 9999)), allowed));
        Assert.Equal(400, ex.Code);
    }

    [Fact(DisplayName = "startsWith/endswith 可下推")]
    public void TryBuildWhere_StartsWith()
    {
        var fact = EntityFactory.CreateFactory(typeof(Osc260830A1b2Item));
        var allowed = Allow("Name");

        var exp = AutomationFilter.TryBuildWhere(fact, F("all", C("Name", "startsWith", "张")), allowed);
        Assert.NotNull(exp);

        var exp2 = AutomationFilter.TryBuildWhere(fact, F("all", C("Name", "endsWith", "三")), allowed);
        Assert.NotNull(exp2);
    }

    [Fact(DisplayName = "Match 内存路径 startswith/endswith 与前端同构")]
    public void Match_StartsWith_EndsWith()
    {
        var row = new Dictionary<String, Object>(StringComparer.OrdinalIgnoreCase) { ["Name"] = "张三" };
        Assert.True(AutomationFilter.Match(row, F("all", C("Name", "startsWith", "张"))));
        Assert.False(AutomationFilter.Match(row, F("all", C("Name", "startsWith", "李"))));
        Assert.True(AutomationFilter.Match(row, F("all", C("Name", "endsWith", "三"))));
        Assert.False(AutomationFilter.Match(row, F("all", C("Name", "endsWith", "五"))));
    }

    [Fact(DisplayName = "条件数 >10 或 any 且 >5 → 400")]
    public void TryBuildWhere_Complexity_Throws400()
    {
        var fact = EntityFactory.CreateFactory(typeof(Osc260830A1b2Item));
        var allowed = Allow("Name", "Enable");

        var many = F("all", Enumerable.Range(0, 11).Select(i => C("Name", "eq", i)).ToArray());
        var ex1 = Assert.Throws<ApiException>(() => AutomationFilter.TryBuildWhere(fact, many, allowed));
        Assert.Equal(400, ex1.Code);

        var orphan = F("any", Enumerable.Range(0, 6).Select(i => C("Name", "eq", i)).ToArray());
        var ex2 = Assert.Throws<ApiException>(() => AutomationFilter.TryBuildWhere(fact, orphan, allowed));
        Assert.Equal(400, ex2.Code);
    }

    [Fact(DisplayName = "Match 白名单外字段 → 400；空条件恒真")]
    public void Match_Whitelist_Throws400()
    {
        var entity = new Osc260830A1b2Item { Name = "张三" };
        var allowed = Allow("Name");

        // 空条件：不读字段，恒真
        Assert.True(AutomationFilter.Match(entity, F("all"), allowed));
        // 白名单外（未下发列）→ 400
        Assert.Throws<ApiException>(() => AutomationFilter.Match(entity, F("all", C("Salary", "eq", 9999)), allowed));
    }

    [Fact(DisplayName = "ResolveFilterTimeField：Log 实体返回 CreateTime，非 Log 返回 null")]
    public void ResolveFilterTimeField_Log()
    {
        var log = EntityFactory.CreateFactory(typeof(Osc260830A1b2Log));
        var fi = AutomationFilter.ResolveFilterTimeField(log);
        Assert.NotNull(fi);
        Assert.Equal("CreateTime", fi.Name);

        var fact = EntityFactory.CreateFactory(typeof(Osc260830A1b2Item));
        Assert.Null(AutomationFilter.ResolveFilterTimeField(fact));
    }

    [Fact(DisplayName = "HasTimeCondition：已有 CreateTime 条件则不再注入时间窗")]
    public void HasTimeCondition_Detects()
    {
        Assert.True(AutomationFilter.HasTimeCondition(F("all", C("CreateTime", "gt", "2026-01-01")), "CreateTime"));
        Assert.True(AutomationFilter.HasTimeCondition(F("all", C("CreateTime", "after", "2026-01-01")), "CreateTime"));
        Assert.False(AutomationFilter.HasTimeCondition(F("all", C("Name", "eq", "x")), "CreateTime"));
        Assert.False(AutomationFilter.HasTimeCondition(null, "CreateTime"));
    }

    [Fact(DisplayName = "TryGetTimeWindow：Log 实体无时间条件命中；已有时间条件不命中；FilterWindowDays=0 关闭")]
    public void TryGetTimeWindow_Log()
    {
        var log = EntityFactory.CreateFactory(typeof(Osc260830A1b2Log));
        var old = CubeSetting.Current.FilterWindowDays;
        try
        {
            CubeSetting.Current.FilterWindowDays = 30;
            var hit = AutomationFilter.TryGetTimeWindow(log, null, out var field, out var days);
            Assert.True(hit);
            Assert.NotNull(field);
            Assert.Equal("CreateTime", field.Name);
            Assert.Equal(30, days);

            // 已有时间条件 → 不命中
            Assert.False(AutomationFilter.TryGetTimeWindow(log, F("all", C("CreateTime", "gt", "2026-01-01")), out _, out _));

            // FilterWindowDays=0 关闭
            CubeSetting.Current.FilterWindowDays = 0;
            Assert.False(AutomationFilter.TryGetTimeWindow(log, null, out _, out _));
        }
        finally
        {
            CubeSetting.Current.FilterWindowDays = old;
        }
    }

    [Fact(DisplayName = "BuildTimeWindow：Log 实体返回表达式；非 Log 返回 null")]
    public void BuildTimeWindow_Log()
    {
        var log = EntityFactory.CreateFactory(typeof(Osc260830A1b2Log));
        var old = CubeSetting.Current.FilterWindowDays;
        try
        {
            CubeSetting.Current.FilterWindowDays = 30;
            var exp = AutomationFilter.BuildTimeWindow(log, null, out var days);
            Assert.NotNull(exp);
            Assert.Equal(30, days);

            var fact = EntityFactory.CreateFactory(typeof(Osc260830A1b2Item));
            Assert.Null(AutomationFilter.BuildTimeWindow(fact, null, out _));
        }
        finally
        {
            CubeSetting.Current.FilterWindowDays = old;
        }
    }
}
