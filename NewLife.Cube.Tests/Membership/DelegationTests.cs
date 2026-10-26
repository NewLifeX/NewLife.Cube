using System;
using NewLife.Cube.Entity;
using XCode.Membership;
using Xunit;

namespace NewLife.Cube.Tests.Membership;

/// <summary>
/// PERM-7 委托代理测试。覆盖代理身份切换、次数递减、次数用尽/过期自动停用，
/// 以及禁止代理系统管理员身份的防护规则。
/// </summary>
[Collection("TenantAuth")]
public class DelegationTests
{
    private readonly TenantAuthFixture _fx;

    /// <summary>实例化</summary>
    /// <param name="fixture">多租户认证夹具。复用其 SQLite 库</param>
    public DelegationTests(TenantAuthFixture fixture) => _fx = fixture;

    #region 辅助
    private static String Unique(String prefix) => prefix + Guid.NewGuid().ToString("N")[..8];

    private User CreateUser(Int32 roleId)
    {
        var user = new User
        {
            Name = Unique("del"),
            DisplayName = "委托测试用户",
            Password = "x",
            RoleID = roleId,
            Enable = true,
            RegisterTime = DateTime.Now,
        };
        user.Insert();

        return user;
    }

    private static PrincipalAgent NewDelegation(User principal, User agent, Int32 times, DateTime expire)
    {
        var item = new PrincipalAgent
        {
            PrincipalId = principal.ID,
            AgentId = agent.ID,
            Enable = true,
            Times = times,
            Expire = expire,
            Remark = "委托测试",
        };
        item.Insert();

        return item;
    }
    #endregion

    [Fact(DisplayName = "委托代理_有效委托登录后切换为委托人身份并扣减次数")]
    public void CheckAgent_ValidDelegation_SwitchesIdentity()
    {
        var principal = CreateUser(_fx.LegacyUser.RoleID);
        var agent = CreateUser(_fx.LegacyUser.RoleID);
        var item = NewDelegation(principal, agent, 2, DateTime.Now.AddDays(7));

        var current = _fx.Provider.CheckAgent(agent);

        Assert.NotNull(current);
        Assert.Equal(principal.ID, current.ID);
        Assert.Equal(1, PrincipalAgent.FindById(item.Id).Times);
    }

    [Fact(DisplayName = "委托代理_次数用尽或已过期的委托自动停用")]
    public void CheckAgent_ExhaustedOrExpired_DisablesItem()
    {
        var principal = CreateUser(_fx.LegacyUser.RoleID);
        var agent = CreateUser(_fx.LegacyUser.RoleID);

        var exhausted = NewDelegation(principal, agent, 0, DateTime.Now.AddDays(7));
        var current = _fx.Provider.CheckAgent(agent);

        Assert.Equal(agent.ID, current.ID);
        Assert.False(PrincipalAgent.FindById(exhausted.Id).Enable);

        var expired = NewDelegation(principal, agent, 3, DateTime.Now.AddDays(-1));
        current = _fx.Provider.CheckAgent(agent);

        Assert.Equal(agent.ID, current.ID);
        Assert.False(PrincipalAgent.FindById(expired.Id).Enable);
    }

    [Fact(DisplayName = "委托代理_禁止代理系统管理员身份")]
    public void CheckAgent_SystemAdminPrincipal_IsRejected()
    {
        var agent = CreateUser(_fx.LegacyUser.RoleID);
        var item = NewDelegation(_fx.AdminUser, agent, 3, DateTime.Now.AddDays(7));

        var current = _fx.Provider.CheckAgent(agent);

        Assert.Equal(agent.ID, current.ID);
        var saved = PrincipalAgent.FindById(item.Id);
        Assert.False(saved.Enable);
        Assert.False(saved.Remark.IsNullOrEmpty());
    }
}

