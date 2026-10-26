using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NewLife.Cube.Entity;
using NewLife.Cube.Tests.Membership;
using XCode.Membership;
using Xunit;

namespace NewLife.Cube.Tests.Common;

/// <summary>
/// SYS 系统管理测试。覆盖字典参数（按用户/类别隔离与类型化取值）、审计日志（成功/失败记录落库）、
/// 用户统计（按日唯一、按日查询与区间检索）与地区管理（父子关系与按全名查找）。
/// </summary>
[Collection("TenantAuth")]
public class SystemManagementTests
{
    private readonly TenantAuthFixture _fx;

    /// <summary>实例化</summary>
    /// <param name="fixture">多租户认证夹具。复用其 SQLite 库</param>
    public SystemManagementTests(TenantAuthFixture fixture) => _fx = fixture;

    #region 辅助
    private static String Unique(String prefix) => prefix + "_" + Guid.NewGuid().ToString("N")[..8];

    /// <summary>轮询等待条件成立，用于异步落库（SaveAsync）场景</summary>
    private static T WaitFor<T>(Func<T> read, Func<T, Boolean> ok, Int32 timeoutMs = 6000)
    {
        var end = Environment.TickCount64 + timeoutMs;
        var value = read();
        while (!ok(value) && Environment.TickCount64 < end)
        {
            Thread.Sleep(100);
            value = read();
        }

        return value;
    }
    #endregion

    #region SYS-2 字典参数
    [Fact(DisplayName = "字典参数_同一用户类别名称只创建一次")]
    public void Parameter_GetOrAdd_CreatesOnce()
    {
        var category = Unique("Cat");
        var userId = _fx.LegacyUser.ID;

        var first = Parameter.GetOrAdd(userId, category, "Key1", "值1");
        var second = Parameter.GetOrAdd(userId, category, "Key1", "值2");

        Assert.True(first.ID > 0);
        Assert.Equal(first.ID, second.ID);
        Assert.Equal("值1", second.Value);

        var found = Parameter.FindByUserIDAndCategoryAndName(userId, category, "Key1");
        Assert.NotNull(found);
        Assert.Equal(first.ID, found.ID);
    }

    [Fact(DisplayName = "字典参数_按用户与类别隔离查询")]
    public void Parameter_IsolatedByUserAndCategory()
    {
        var category = Unique("Cat");
        var other = Unique("Cat");
        var userA = _fx.LegacyUser.ID;
        var userB = _fx.LegacyUser2.ID;

        Parameter.GetOrAdd(userA, category, "Shared", "A");
        Parameter.GetOrAdd(userB, category, "Shared", "B");
        Parameter.GetOrAdd(userA, other, "Shared", "C");

        var listA = Parameter.FindAllByUserIDAndCategory(userA, category);
        var listB = Parameter.FindAllByUserIDAndCategory(userB, category);

        Assert.Single(listA);
        Assert.Equal("A", listA[0].Value);
        Assert.Single(listB);
        Assert.Equal("B", listB[0].Value);
        Assert.Null(Parameter.FindByUserIDAndCategoryAndName(userA, category, "Missing"));
    }

    [Fact(DisplayName = "字典参数_类型化存取支持字符串与列表")]
    public void Parameter_SetValue_RoundTripsTypes()
    {
        var param = Parameter.GetOrAdd(_fx.LegacyUser.ID, Unique("Cat"), "Typed", null);

        param.SetValue("纯文本");
        Assert.Equal("纯文本", param.GetValue());

        param.SetValue(new List<String> { "a", "b", "c" });
        var list = param.GetList<String>();
        Assert.Equal(3, list.Count());
        Assert.Equal(["a", "b", "c"], list);

        param.SetValue(null);
        Assert.Null(param.GetValue());
    }
    #endregion

    #region SYS-3 审计日志
    [Fact(DisplayName = "审计日志_成功与失败操作都能落库并可按动作检索")]
    public void AuditLog_WriteAndQuery()
    {
        var action = Unique("SysTest");
        var provider = LogProvider.Provider;

        provider.WriteLog(typeof(User), action, true, "系统管理测试成功", _fx.LegacyUser.ID, _fx.LegacyUser.Name, "127.0.0.1");
        provider.WriteLog(typeof(User), action, false, "系统管理测试失败", _fx.LegacyUser.ID, _fx.LegacyUser.Name, "127.0.0.1");

        var list = WaitFor(
            () => XCode.Membership.Log.FindAll(XCode.Membership.Log._.Action == action),
            e => e.Count >= 2);

        Assert.Equal(2, list.Count);
        Assert.Contains(list, e => e.Success && e.Remark.Contains("成功"));
        Assert.Contains(list, e => !e.Success && e.Remark.Contains("失败"));
        Assert.All(list, e =>
        {
            Assert.Equal(_fx.LegacyUser.ID, e.CreateUserID);
            Assert.Equal(_fx.LegacyUser.Name, e.UserName);
            Assert.Equal("127.0.0.1", e.CreateIP);
            Assert.True(e.CreateTime.Year >= DateTime.Today.Year);
        });
    }
    #endregion

    #region SYS-12 用户统计
    [Fact(DisplayName = "用户统计_按日唯一并可查询与区间检索")]
    public void UserStat_GetOrAdd_UniquePerDay()
    {
        var date = new DateTime(2026, 3, 15);

        var first = UserStat.GetOrAdd(date);
        var second = UserStat.GetOrAdd(date);

        Assert.NotNull(first);
        Assert.Equal(first.ID, second.ID);
        Assert.Equal(date.Date, first.Date.Date);
        Assert.Equal(1, UserStat.FindAll(UserStat._.Date == date).Count);

        var found = UserStat.FindByDate(date);
        Assert.NotNull(found);
        Assert.Equal(first.ID, found.ID);

        var page = new NewLife.Data.PageParameter { PageIndex = 1, PageSize = 10 };
        var list = UserStat.SearchByDate(date.AddDays(-1), date.AddDays(1), null, page);
        Assert.Contains(list, e => e.ID == first.ID);

        var outside = UserStat.SearchByDate(date.AddDays(5), date.AddDays(6), null, page);
        Assert.DoesNotContain(outside, e => e.ID == first.ID);

        first.Delete();
    }

    [Fact(DisplayName = "用户统计_按日期区间分页检索")]
    public void UserStat_SearchByDate_PagesAndFilters()
    {
        var start = new DateTime(2026, 4, 1);
        var stat = UserStat.GetOrAdd(start);
        Assert.NotNull(stat);

        // 空关键字按区间返回
        var page = new NewLife.Data.PageParameter { PageIndex = 1, PageSize = 5 };
        var list = UserStat.SearchByDate(start, start.AddDays(7), null, page);
        Assert.Contains(list, e => e.ID == stat.ID);

        // 区间之外不返回
        var outside = UserStat.SearchByDate(start.AddDays(30), start.AddDays(31), null, new NewLife.Data.PageParameter { PageIndex = 1, PageSize = 5 });
        Assert.DoesNotContain(outside, e => e.ID == stat.ID);

        stat.Delete();
    }
    #endregion

    #region SYS-13 地区管理
    [Fact(DisplayName = "地区管理_父子关系与全名查找")]
    public void Area_Create_BuildsHierarchy()
    {
        var rootName = Unique("省");
        var childName = Unique("市");
        var leafName = Unique("区");

        // Area.ID 为行政区划代码（非自增），测试用随机编号避免与真实数据冲突
        var root = new Area { ID = NewLife.Security.Rand.Next(900_000_000, 999_999_999), Name = rootName, ParentID = 0, Enable = true, Remark = "测试根节点" };
        root.Insert();
        var child = new Area { ID = NewLife.Security.Rand.Next(900_000_000, 999_999_999), Name = childName, ParentID = root.ID, Enable = true };
        child.Insert();
        var leaf = new Area { ID = NewLife.Security.Rand.Next(900_000_000, 999_999_999), Name = leafName, ParentID = child.ID, Enable = true };
        leaf.Insert();

        Assert.True(root.ID > 0);
        Assert.Equal(root.ID, child.ParentID);
        Assert.Equal(child.ID, leaf.ParentID);

        // 地区层级查询走实体缓存，插入后需清缓存才能命中
        Area.Meta.Cache.Clear("地区管理测试写入", true);

        Assert.Equal(root.ID, Area.FindByID(child.ID)?.ParentID);

        var children = Area.FindAll(Area._.ParentID == root.ID);
        Assert.Contains(children, e => e.ID == child.ID);
        Assert.DoesNotContain(children, e => e.ID == leaf.ID);

        var parents = leaf.GetAllParents();
        Assert.Contains(parents, e => e.ID == child.ID);
        Assert.Contains(parents, e => e.ID == root.ID);

        Assert.Equal(child.ID, Area.Find(Area._.ParentID == root.ID & Area._.Name == childName)?.ID);
        Assert.Null(Area.Find(Area._.ParentID == root.ID & Area._.Name == leafName));
    }
    #endregion
}

