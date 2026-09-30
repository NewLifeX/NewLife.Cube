using System;
using System.Collections.Generic;
using System.Linq;
using NewLife.Cube.Areas.Cube.Controllers;
using NewLife.Remoting;
using XCode;
using XCode.DataAccessLayer;
using XCode.Membership;
using Xunit;

namespace NewLife.Cube.Tests;

/// <summary>OSC-2609307879 地区名称批量查询。</summary>
public class AreaNamesTests
{
    public AreaNamesTests()
    {
        DAL.AddConnStr("Membership", "Data Source=Osc7879Area;Mode=Memory;Cache=Shared", null, "SQLite");
    }

    [Fact(DisplayName = "空 ids 返回空字典，非法字符串忽略，未知 ID 省略")]
    public void Resolve_Empty_Illegal_Unknown()
    {
        Assert.Empty(AreaController.ResolveNames(null));
        Assert.Empty(AreaController.ResolveNames(new List<String>()));
        Assert.Empty(AreaController.ResolveNames(["abc", "  ", "0"]));

        var map = AreaController.ResolveNames(["99999999"]);
        Assert.False(map.ContainsKey("99999999"));
    }

    [Fact(DisplayName = "缓存命中返回 Name，空名称省略")]
    public void Resolve_CacheHit_SkipsEmptyName()
    {
        Remove(78790001);
        Remove(78790002);
        var named = new Area { ID = 78790001, Name = "东城区" };
        named.Insert();
        var blank = new Area { ID = 78790002, Name = "" };
        blank.Insert();

        var map = AreaController.ResolveNames(["78790001", "78790002", "abc"]);
        Assert.Equal("东城区", map["78790001"]);
        Assert.False(map.ContainsKey("78790002"));

        Remove(78790001);
        Remove(78790002);
    }

    static void Remove(Int32 id)
    {
        var old = Area.FindByID(id);
        old?.Delete();
    }

    [Fact(DisplayName = "去重后超过 200 个返回 400，消息含 200")]
    public void Normalize_Over200_Throws()
    {
        var ids = Enumerable.Range(1, 201).Select(i => i.ToString()).ToList();
        ids.Add("  1  ");
        var ex = Assert.Throws<ApiException>(() => AreaController.NormalizeNameIds(ids));
        Assert.Contains("200", ex.Message);
    }
}
