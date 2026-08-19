using System;
using System.ComponentModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube;
using XCode;
using XCode.DataAccessLayer;
using Xunit;

namespace NewLife.Cube.Tests;

/// <summary>OSC-260819e483 P1 测试实体（WebAPI 栈）</summary>
[DisplayName("OSC260819 P1 实体")]
[BindTable("Osc260819P1Item", ConnName = "Cube", DbType = DatabaseType.None)]
public class Osc260819P1Item : Entity<Osc260819P1Item>
{
    [DisplayName("编号")]
    [DataObjectField(true, true, false, 0)]
    public Int32 Id { get; set; }

    [DisplayName("名称")]
    public String? Name { get; set; }
}

/// <summary>P1 测试控制器：暴露 protected 的 EnableFieldValidationRequested</summary>
public class Osc260819P1Controller : EntityController<Osc260819P1Item>
{
    /// <summary>校验头请求状态</summary>
    public Boolean Requested => EnableFieldValidationRequested;
}

/// <summary>OSC-260819e483 P1 契约加固：校验头选择加入 / Stat 安全转换</summary>
public class Osc260819P1Tests
{
    public Osc260819P1Tests()
    {
        DAL.AddConnStr("Cube", "Data Source=Osc260819P1;Mode=Memory;Cache=Shared", null, "SQLite");
        DAL.AddConnStr("Membership", "Data Source=Osc260819P1Membership;Mode=Memory;Cache=Shared", null, "SQLite");
    }

    [Fact(DisplayName = "P1 校验头：无头默认 false；X-Cube-Field-Validation=true/1/yes 时 true；其它值 false")]
    public void ValidationHeader_Toggles()
    {
        var c = new Osc260819P1Controller
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };

        // 无头 → 默认 false（EnableFieldValidation 默认 false，不改变基类行为）
        Assert.False(c.Requested);

        c.Request.Headers["X-Cube-Field-Validation"] = "true";
        Assert.True(c.Requested);

        c.Request.Headers["X-Cube-Field-Validation"] = "1";
        Assert.True(c.Requested);

        c.Request.Headers["X-Cube-Field-Validation"] = "YES";
        Assert.True(c.Requested);

        c.Request.Headers["X-Cube-Field-Validation"] = "no";
        Assert.False(c.Requested);
    }

    [Fact(DisplayName = "P1 Stat 安全转换：p.State 非实体时 as TEntity 为 null 不抛（Index 不得 InvalidCastException）")]
    public void Stat_AsEntity_NoThrow()
    {
        var p = new NewLife.Web.Pager { RetrieveTotalCount = true };

        // 无 RetrieveState：SearchData 把 WhereBuilder 留在 State → as 转换得 null，不抛
        p.State = new Object();
        Assert.Null(p.State as Osc260819P1Item);

        // 有统计实体且类型匹配 → 返回实体
        var entity = new Osc260819P1Item { Name = "x" };
        p.State = entity;
        Assert.Same(entity, p.State as Osc260819P1Item);
    }
}
