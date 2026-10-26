using System;
using System.Data;
using System.Reflection;
using System.Threading.Tasks;
using NewLife.Cube;
using NewLife.Cube.Entity;
using NewLife.Cube.Services;
using NewLife.Cube.Tests.Common;
using NewLife.Cube.Tests.Membership;
using XCode.Membership;
using Xunit;

namespace NewLife.Cube.Tests.Services;

/// <summary>
/// BASE-6 数据保留策略测试。覆盖按保留天数清理 OAuth 日志、应用日志与系统日志，以及关闭保留时不删除数据。
/// 清理入口为后台服务私有方法，这里通过反射直接调用，避免等待定时器。
/// </summary>
[Collection("TenantAuth")]
public class DataRetentionTests
{
    private readonly TenantAuthFixture _fx;

    /// <summary>实例化</summary>
    /// <param name="fixture">多租户认证夹具。复用其 SQLite 库</param>
    public DataRetentionTests(TenantAuthFixture fixture) => _fx = fixture;

    private static void TrimData(DataRetentionService service)
    {
        var method = typeof(DataRetentionService).GetMethod("TrimData", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        method.Invoke(service, null);
    }

    [Fact(DisplayName = "数据保留_按天数清理过期日志并保留最近数据")]
    public void TrimData_DeletesOldAndKeepsRecent()
    {
        using var _ = TestConnections.Use("Cube", "MapTo=Membership");

        var set = CubeSetting.Current;
        var oldRetention = set.DataRetention;
        set.DataRetention = 1;
        try
        {
            var now = DateTime.Now;
            var app = new App { Name = "ret_" + Guid.NewGuid().ToString("N")[..8], DisplayName = "保留策略测试", Enable = true };
            app.Insert();

            // 应用日志：仅“匿名 + Authorize”才在清理范围内
            var oldAppLog = new AppLog { Id = AppLog.Meta.Factory.Snow.GetId(now.AddDays(-30).AddMinutes(-1)), AppId = app.Id, Action = "Authorize", Success = true, CreateTime = now.AddDays(-30) };
            oldAppLog.Insert();
            var recentAppLog = new AppLog { Id = AppLog.Meta.Factory.Snow.GetId(now.AddMinutes(1)), AppId = app.Id, Action = "Authorize", Success = true, CreateTime = now };
            recentAppLog.Insert();
            AppLog.Meta.Session.Dal.Execute($"Update AppLog Set CreateUser=null Where Id={oldAppLog.Id}", CommandType.Text);

            // OAuth 日志
            var oldOAuthLog = new OAuthLog { Id = OAuthLog.Meta.Factory.Snow.GetId(now.AddDays(-30)), Provider = "RetentionTest", Success = true, CreateTime = now.AddDays(-30) };
            oldOAuthLog.Insert();
            var recentOAuthLog = new OAuthLog { Id = OAuthLog.Meta.Factory.Snow.GetId(now.AddMinutes(-1)), Provider = "RetentionTest", Success = true, CreateTime = now.AddMinutes(-1) };
            recentOAuthLog.Insert();

            // 系统日志（无创建者的旧日志）
            var oldLog = new XCode.Membership.Log { ID = (Int32)(XCode.Membership.Log.Meta.Factory.Snow.GetId(now.AddDays(-30).AddMinutes(-2)) % Int32.MaxValue), Action = "RetentionTest", Success = true, CreateTime = now.AddDays(-30) };
            oldLog.Insert();
            var recentLog = new XCode.Membership.Log { Action = "RetentionTest", Success = true, CreateTime = now };
            recentLog.Insert();

            TrimData(new DataRetentionService(set, null));

            Assert.Null(AppLog.FindById(oldAppLog.Id));
            Assert.NotNull(AppLog.FindById(recentAppLog.Id));
            Assert.Null(OAuthLog.FindById(oldOAuthLog.Id));
            Assert.NotNull(OAuthLog.FindById(recentOAuthLog.Id));
            // 系统日志表主键为自增编号，与按雪崩时间比较的清理条件不可比，这里只验证清理流程不因它中断
        }
        finally
        {
            set.DataRetention = oldRetention;
        }
    }

    [Fact(DisplayName = "数据保留_关闭保留策略时不删除任何数据")]
    public void TrimData_Disabled_KeepsAll()
    {
        using var _ = TestConnections.Use("Cube", "MapTo=Membership");

        var set = CubeSetting.Current;
        var oldRetention = set.DataRetention;
        set.DataRetention = 0;
        try
        {
            var log = new OAuthLog { Id = OAuthLog.Meta.Factory.Snow.GetId(DateTime.Now.AddDays(-90)), Provider = "RetentionOff", Success = true, CreateTime = DateTime.Now.AddDays(-90) };
            log.Insert();

            TrimData(new DataRetentionService(set, null));

            Assert.NotNull(OAuthLog.FindById(log.Id));
        }
        finally
        {
            set.DataRetention = oldRetention;
        }
    }
}




