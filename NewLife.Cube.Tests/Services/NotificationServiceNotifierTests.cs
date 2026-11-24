using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NewLife.Cube.Entity;
using NewLife.Cube.Services.Notifications;
using NewLife.Cube.Tests.Membership;
using XCode.Membership;
using Xunit;

namespace NewLife.Cube.Tests.Services;

/// <summary>模块级通知接口与高级发布测试。验证小请求只依赖 INotifier，高级字段走 NotificationService.Publish</summary>
[Collection("TenantAuth")]
public class NotificationServiceNotifierTests
{
    private readonly TenantAuthFixture _fixture;

    /// <summary>实例化测试</summary>
    /// <param name="fixture">多租户夹具。复用 SQLite 通知记录与用户表</param>
    public NotificationServiceNotifierTests(TenantAuthFixture fixture) => _fixture = fixture;

    private static NotificationService CreateService() => new(new NotificationRecipientResolver(), new NotificationTemplateRegistry());

    private static IList<NotificationRecord> Rows(String bizId) => NotificationRecord.FindAll(NotificationRecord._.BizId == bizId);

    private static void Cleanup(String bizId) => NotificationRecord.Delete(NotificationRecord._.BizId == bizId);

    private static String NewBizId() => "test:notifier:" + Guid.NewGuid().ToString("N");

    [Fact(DisplayName = "模块通知接口_普通请求_写入站内信")]
    public async Task SendAsync_Request_WritesInAppRecord()
    {
        var bizId = NewBizId();
        try
        {
            INotifier notifier = CreateService();
            var result = await notifier.SendAsync(new NotifyRequest
            {
                BizId = bizId,
                UserIds = [_fixture.BoundUser.Id],
                Channels = ["InApp"],
                Title = "统一接口",
                Content = "模块只依赖 INotifier",
            });

            Assert.False(result.Skipped);
            Assert.Equal(1, result.Recipients);
            Assert.Equal(1, result.Records);

            var row = Assert.Single(Rows(bizId));
            Assert.Equal(_fixture.BoundUser.Id, row.UserId);
            Assert.Equal("InApp", row.Channel);
            Assert.Equal("Business", row.Category);
            Assert.Equal("Info", row.Level);
        }
        finally
        {
            Cleanup(bizId);
        }
    }

    [Fact(DisplayName = "模块通知接口_扩展字段_参与模板渲染")]
    public async Task SendAsync_ExtensionItems_RenderTemplate()
    {
        var bizId = NewBizId();
        try
        {
            var request = new NotifyRequest
            {
                BizId = bizId,
                UserIds = [_fixture.BoundUser.Id],
                Channels = ["InApp"],
                Title = "订单 {orderId} 已支付",
                Content = "订单 {orderId} 已支付",
            };
            request.Items["orderId"] = "A100";

            var result = await ((INotifier)CreateService()).SendAsync(request);

            Assert.False(result.Skipped);
            var row = Assert.Single(Rows(bizId));
            Assert.Equal("订单 A100 已支付", row.Title);
        }
        finally
        {
            Cleanup(bizId);
        }
    }

    [Fact(DisplayName = "高级发布_扩展字段_覆盖业务数据同名字段")]
    public void Publish_ExtensionItems_OverridesData()
    {
        var bizId = NewBizId();
        try
        {
            var request = new NotificationRequest
            {
                BizId = bizId,
                UserIds = [_fixture.BoundUser.Id],
                Channels = ["InApp"],
                Title = "{orderId}-{status}",
                Content = "订单 {orderId} 状态 {status}",
                Data = new { orderId = "A100", status = "new" },
            };
            request.Items["status"] = "paid";

            var result = CreateService().Publish(request);

            Assert.False(result.Skipped);
            var row = Assert.Single(Rows(bizId));
            Assert.Equal("A100-paid", row.Title);
        }
        finally
        {
            Cleanup(bizId);
        }
    }

    [Fact(DisplayName = "高级发布_共享已读请求_保留魔方字段")]
    public void Publish_Request_PreservesCubeFields()
    {
        var bizId = NewBizId();
        try
        {
            var request = new NotificationRequest
            {
                BizId = bizId,
                UserIds = [_fixture.BoundUser.Id, _fixture.LegacyUser.Id],
                Scope = NotificationScope.Any,
                Channels = ["InApp"],
                Title = "共享消息",
                Content = "任一已读即全组已读",
            };

            var result = CreateService().Publish(request);

            Assert.False(result.Skipped);
            Assert.Equal(2, result.Recipients);
            var rows = Rows(bizId);
            Assert.Equal(2, rows.Count);
            Assert.All(rows, e => Assert.True(e.Shared));
        }
        finally
        {
            Cleanup(bizId);
        }
    }

    [Fact(DisplayName = "模块通知接口_标题内容为空_抛参数异常")]
    public async Task SendAsync_EmptyContent_Throws()
    {
        var notifier = (INotifier)CreateService();

        await Assert.ThrowsAsync<ArgumentException>(() => notifier.SendAsync(new NotifyRequest
        {
            UserIds = [_fixture.BoundUser.Id],
        }));
    }

    [Fact(DisplayName = "高级发布_发送给管理员_生成个人站内信")]
    public void Publish_ToAdmins_WritesPersonalInApp()
    {
        var bizId = NewBizId();
        var old = TenantContext.Current;
        TenantContext.Current = null!;
        try
        {
            var request = new NotificationRequest
            {
                BizId = bizId,
                ToAdmins = true,
                Scope = NotificationScope.Any,
                Channels = ["InApp"],
                Title = "管理员消息",
                Content = "系统级通知",
            };

            var result = CreateService().Publish(request);

            Assert.False(result.Skipped);
            Assert.True(result.Recipients >= 1);

            var rows = Rows(bizId);
            Assert.Contains(rows, e => e.UserId == _fixture.AdminUser.Id);
            Assert.DoesNotContain(rows, e => e.UserId == 0);
        }
        finally
        {
            TenantContext.Current = old;
            Cleanup(bizId);
        }
    }
}
