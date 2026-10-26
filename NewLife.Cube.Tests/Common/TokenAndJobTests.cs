using System;
using System.Collections.Generic;
using System.Linq;
using NewLife.Cube.Entity;
using NewLife.Cube.Tests.Membership;
using NewLife.Data;
using NewLife.Web;
using XCode.Membership;
using Xunit;

namespace NewLife.Cube.Tests.Common;

/// <summary>
/// SYS 系统管理测试（第三批）。覆盖用户令牌（有效性校验：无效/禁用/过期/账号禁用，首次使用记录IP与次数，列表检索）
/// 与定时作业（静态方法注册、同名复用、非静态方法拒绝、条件检索）。
/// </summary>
[Collection("TenantAuth")]
public class TokenAndJobTests
{
    private readonly TenantAuthFixture _fx;

    /// <summary>实例化</summary>
    /// <param name="fixture">多租户认证夹具。复用其 SQLite 库</param>
    public TokenAndJobTests(TenantAuthFixture fixture) => _fx = fixture;

    #region 辅助
    private static String Unique(String prefix) => prefix + Guid.NewGuid().ToString("N")[..10];

    private UserToken CreateToken(User user, Boolean enable = true, DateTime? expire = null)
    {
        var token = new UserToken
        {
            Token = Unique("tk"),
            UserID = user.ID,
            Enable = enable,
            Expire = expire ?? DateTime.Now.AddDays(7),
            CreateIP = "127.0.0.1",
        };
        token.Insert();

        return token;
    }

    /// <summary>定时作业测试方法（必须静态且带单个 String 参数）</summary>
    private static void TestJob(String argument) { }

    #endregion

    #region SYS-11 用户令牌
    [Fact(DisplayName = "用户令牌_无效禁用过期与账号禁用均被拒绝")]
    public void UserToken_Valid_RejectsInvalidStates()
    {
        var user = _fx.LegacyUser;

        var ex = Assert.Throws<Exception>(() => UserToken.Valid(null, "127.0.0.1"));
        Assert.False(ex.Message.IsNullOrEmpty());

        ex = Assert.Throws<Exception>(() => UserToken.Valid(Unique("missing"), "127.0.0.1"));
        Assert.False(ex.Message.IsNullOrEmpty());

        var disabled = CreateToken(user, false);
        Assert.Throws<Exception>(() => UserToken.Valid(disabled.Token, "127.0.0.1"));

        var expired = CreateToken(user, true, DateTime.Now.AddMinutes(-1));
        Assert.Throws<Exception>(() => UserToken.Valid(expired.Token, "127.0.0.1"));

        // 账号禁用
        var temp = new User
        {
            Name = Unique("tkUser")[..12],
            Password = "x",
            RoleID = user.RoleID,
            Enable = false,
            RegisterTime = DateTime.Now,
        };
        temp.Insert();
        var bound = CreateToken(temp);
        Assert.Throws<Exception>(() => UserToken.Valid(bound.Token, "127.0.0.1"));

        temp.Enable = true;
        temp.Update();
    }

    [Fact(DisplayName = "用户令牌_有效令牌首次使用记录IP与次数")]
    public void UserToken_Valid_RecordsFirstUseAndTimes()
    {
        var user = _fx.LegacyUser;
        var token = CreateToken(user);

        var first = UserToken.Valid(token.Token, "10.1.2.3");

        Assert.Equal(token.ID, first.ID);
        Assert.Equal(1, first.Times);
        Assert.Equal("10.1.2.3", first.FirstIP);
        Assert.True(first.FirstTime.Year >= DateTime.Today.Year);
        Assert.Equal("10.1.2.3", first.LastIP);
        Assert.True((DateTime.Now - first.LastTime).TotalMinutes < 1);

        var second = UserToken.Valid(token.Token, "10.9.9.9");
        Assert.Equal(2, second.Times);
        Assert.Equal("10.1.2.3", second.FirstIP);
        Assert.Equal("10.9.9.9", second.LastIP);
    }

    [Fact(DisplayName = "用户令牌_按令牌用户与启用状态检索")]
    public void UserToken_Search_Filters()
    {
        var user = _fx.LegacyUser;
        var other = _fx.LegacyUser2;
        var mine = CreateToken(user);
        CreateToken(user, false);
        CreateToken(other);

        var byUser = UserToken.FindAllByUserID(user.ID);
        Assert.Contains(byUser, e => e.ID == mine.ID);
        Assert.DoesNotContain(byUser, e => e.UserID == other.ID);

        
        var byToken = UserToken.Search(mine.Token, mine.UserID, null, DateTime.Now.AddDays(-1), DateTime.Now.AddDays(30), new Pager { PageIndex = 1, PageSize = 20 });
        Assert.Single(byToken);
        Assert.Equal(mine.ID, byToken[0].ID);

        var enabled = UserToken.Search(null, mine.UserID, true, DateTime.Now.AddDays(-1), DateTime.Now.AddDays(30), new Pager { PageIndex = 1, PageSize = 20 });
        Assert.Contains(enabled, e => e.ID == mine.ID);
        Assert.All(enabled, e => Assert.True(e.Enable));

        Assert.Null(UserToken.FindByToken(Unique("missing")));
        Assert.Equal(mine.ID, UserToken.FindByToken(mine.Token)?.ID);
    }
    #endregion

    #region SYS-7 定时作业
    [Fact(DisplayName = "定时作业_静态方法注册并按名称复用")]
    public void CronJob_Add_ReusesByName()
    {
        var name = Unique("job")[..12];
        var action = (Action<String>)TestJob;

        var first = CronJob.Add(name, action, "*/5 * * * *");
        Assert.True(first.Id > 0);
        Assert.Equal(name, first.Name);
        Assert.Equal("*/5 * * * *", first.Cron);
        Assert.True(first.Enable);
        Assert.Contains(nameof(TestJob), first.Method);

        // 同名作业再次注册：复用原记录，保留页面修改过的 Cron
        first.Cron = "0 3 * * *";
        first.Update();
        var second = CronJob.Add(name, action, "*/1 * * * *");
        Assert.Equal(first.Id, second.Id);
        Assert.Equal("0 3 * * *", second.Cron);

        Assert.Equal(first.Id, CronJob.FindByName(name)?.Id);
        Assert.Equal(first.Id, CronJob.FindById(first.Id)?.Id);
    }

    [Fact(DisplayName = "定时作业_非静态方法拒绝注册且支持条件检索")]
    public void CronJob_Add_RejectsNonStaticAndSearch()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CronJob.Add(Unique("job"), (Action<String>)((e) => { }), "* * * * *"));

        var name = Unique("job")[..12];
        CronJob.Add(name, (Action<String>)TestJob, "0 1 * * *");

        var page = new PageParameter { PageIndex = 1, PageSize = 10 };
        var list = CronJob.Search(name, DateTime.Now.AddDays(-1), DateTime.Now.AddDays(1), null, page);
        Assert.Contains(list, e => e.Name == name);

        var outside = CronJob.Search(Unique("missing"), DateTime.Now.AddDays(-1), DateTime.Now.AddDays(1), null, new PageParameter { PageIndex = 1, PageSize = 10 });
        Assert.DoesNotContain(outside, e => e.Name == name);

        Assert.Null(CronJob.FindByName(Unique("missing")));
    }
    #endregion
}


