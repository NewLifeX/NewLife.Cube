using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube;
using NewLife.Cube.Automation;
using NewLife.Cube.ViewModels;
using NewLife.Remoting;
using NewLife.Web;
using XCode;
using XCode.DataAccessLayer;
using XCode.Model;
using Xunit;

namespace XUnitTest;

/// <summary>P2 测试实体：XCode 数据字段要求 virtual 属性 + DataObjectField 标注，否则不识别/值读写失败</summary>
[DisplayName("OSC260819 P2 实体")]
[BindTable("Osc260819P2Item", ConnName = "Cube", DbType = DatabaseType.None)]
public class Osc260819P2Item : Entity<Osc260819P2Item>
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

/// <summary>P2 测试控制器：暴露 SearchData，无数据权限</summary>
public class Osc260819P2Controller : ReadOnlyEntityController<Osc260819P2Item>
{
    /// <summary>执行 SearchData 并返回分页对象与结果</summary>
    /// <param name="viewFilter">viewFilter 查询参数</param>
    /// <returns></returns>
    public (Pager pager, List<Osc260819P2Item> list) Run(String viewFilter)
    {
        var p = new Pager();
        if (!String.IsNullOrEmpty(viewFilter)) p["viewFilter"] = viewFilter;
        var list = SearchData(p).ToList();
        return (p, list);
    }
}

/// <summary>P2 测试控制器：带数据权限 1=0（override CreateWhere 提供权限表达式，避免依赖 HttpContext Session 初始化）</summary>
public class Osc260819P2PermController : ReadOnlyEntityController<Osc260819P2Item>
{
    /// <summary>数据权限表达式</summary>
    protected override WhereBuilder CreateWhere()
    {
        var builder = new WhereBuilder
        {
            Factory = Factory,
            // WhereBuilder 支持 `字段=常量`（如 TenantId={#TenantId}）；Id=0 代表权限范围
            Expression = "Id=0",
        };
        builder.SetData(new Dictionary<String, Object>(StringComparer.OrdinalIgnoreCase));
        builder.SetData2(new Dictionary<String, Object>(StringComparer.OrdinalIgnoreCase));
        return builder;
    }

    /// <summary>执行 SearchData 并返回分页对象与结果</summary>
    /// <param name="viewFilter">viewFilter 查询参数</param>
    /// <returns></returns>
    public (Pager pager, List<Osc260819P2Item> list) Run(String viewFilter)
    {
        var p = new Pager();
        if (!String.IsNullOrEmpty(viewFilter)) p["viewFilter"] = viewFilter;
        var list = SearchData(p).ToList();
        return (p, list);
    }
}

/// <summary>OSC-260819e483 P2 筛选 AST（复用 AutomationFilter）与 Sort</summary>
public class Osc260819P2Tests
{
    public Osc260819P2Tests()
    {
        DAL.AddConnStr("Cube", "Data Source=Osc260819P2Tests;Mode=Memory;Cache=Shared", null, "SQLite");
    }

    static ViewFilterDto F(String logic, params ViewFilterConditionDto[] conds) => new() { Logic = logic, Conditions = [.. conds] };

    static ViewFilterConditionDto C(String field, String op, Object value = null) => new() { Field = field, Op = op, Value = value };

    [Fact]
    [DisplayName("P2 ParseViewFilter：空→null；超长→400；坏 JSON→400")]
    public void ParseViewFilter_Edges()
    {
        Assert.Null(AutomationFilter.ParseViewFilter(null));
        Assert.Null(AutomationFilter.ParseViewFilter(""));

        var longJson = new String('a', 4097);
        var ex = Assert.Throws<ApiException>(() => AutomationFilter.ParseViewFilter(longJson));
        Assert.Equal(400, ex.Code);

        Assert.Throws<ApiException>(() => AutomationFilter.ParseViewFilter("{bad json"));
    }

    [Fact]
    [DisplayName("P2 TryBuildWhere：notcontains 可下推；未知字段整段 null")]
    public void TryBuildWhere_NotContains()
    {
        var fact = EntityFactory.CreateFactory(typeof(Osc260819P2Item));
        Assert.NotNull(fact);
        Assert.NotNull(fact.Fields);
        Assert.Contains(fact.Fields, f => f.Name == "Name");

        // notcontains 本号补下推（与 Match 对齐）
        var exp = AutomationFilter.TryBuildWhere(fact, F("all", C("Name", "notcontains", "公司")));
        Assert.NotNull(exp);

        // 未知字段 → 整段 null（保持现码行为，不 500）
        Assert.Null(AutomationFilter.TryBuildWhere(fact, F("all", C("Ghost", "eq", 1))));
    }

    [Fact]
    [DisplayName("P2 SearchData 可下推时把 viewFilter 并进 p.State（无权限时 State=Expression）")]
    public void SearchData_PushesViewFilter()
    {
        var c = new Osc260819P2Controller();
        var (p, _) = c.Run("""{"logic":"all","conditions":[{"field":"Name","op":"eq","value":"x"}]}""");
        Assert.NotNull(p.State);
        Assert.IsAssignableFrom<Expression>(p.State);

        // 不传 viewFilter → 不下推，State 仍为 null（无数据权限）
        var (p2, _) = c.Run(null);
        Assert.Null(p2.State);
    }

    [Fact]
    [DisplayName("P2 logic=any 不放大权限：权限表达式并进下推，State=Expression（权限 AND viewExp）")]
    public void SearchData_AnyFilter_DoesNotExpandPermission()
    {
        var c = new Osc260819P2PermController();

        // any：Name=x OR Enable=true；权限 Id=0（合法 WhereBuilder 表达式）必须 AND，State 为合并后的 Expression
        var (p, list) = c.Run("""{"logic":"any","conditions":[{"field":"Name","op":"eq","value":"x"},{"field":"Enable","op":"eq","value":"true"}]}""");
        Assert.NotNull(p.State);
        Assert.IsAssignableFrom<Expression>(p.State);
        Assert.NotNull(list);
    }
}
