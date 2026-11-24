using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NewLife.Caching;
using NewLife.Cube.Entity;
using NewLife.Cube.Services.Notifications;
using NewLife.Cube.Services.Notifications.Channels;
using NewLife.Cube.Tests.Membership;
using NewLife.Messaging;
using XCode.Membership;
using Xunit;

namespace NewLife.Cube.Tests.Services;

/// <summary>消息通知服务测试。覆盖多渠道路由、BizId 共享已读、团队任一人处理、偏好与去重</summary>
[Collection("TenantAuth")]
public class NotificationServiceTests
{
    private readonly TenantAuthFixture _fixture;

    /// <summary>实例化测试</summary>
    /// <param name="fixture">多租户夹具。复用 SQLite 通知记录与系统参数表</param>
    public NotificationServiceTests(TenantAuthFixture fixture) => _fixture = fixture;

    private static NotificationService CreateService() => new(new NotificationRecipientResolver(), new NotificationTemplateRegistry());

    private static NotificationRequest Request(params Int32[] userIds) => new()
    {
        UserIds = userIds,
        Action = "Notify",
        Category = "Business",
        Level = "Info",
        Title = "测试通知",
        Content = "测试内容",
        Channels = ["InApp"],
    };

    private static IList<NotificationRecord> Rows(String bizId) => NotificationRecord.FindAll(NotificationRecord._.BizId == bizId);

    private static void Cleanup(String bizId) => NotificationRecord.Delete(NotificationRecord._.BizId == bizId);

    [Fact(DisplayName = "通知_个人多渠道_每渠道一行且共享业务编号")]
    public void Publish_PersonalMultiChannel_WritesOneRowPerChannel()
    {
        var request = Request(_fixture.BoundUser.Id);
        request.Channels = ["inapp", "mail", "sms"];

        var result = CreateService().Publish(request);

        Assert.False(result.Skipped);
        Assert.Equal(1, result.Recipients);
        Assert.Equal(3, result.Records);
        Assert.Single(result.BizIds);

        var rows = Rows(result.BizId);
        Assert.Equal(3, rows.Count);
        Assert.All(rows, e => Assert.Equal(result.BizId, e.BizId));
        Assert.All(rows, e => Assert.False(e.Shared));
        Assert.Equal(3, rows.Select(e => e.Channel).Distinct().Count());

        var inApp = rows.First(e => e.Channel.EqualIgnoreCase("InApp"));
        Assert.True(inApp.Success);
        Assert.Equal((Int32)NotificationStatus.Success, inApp.Status);
        Assert.False(inApp.Read);
        Assert.True(inApp.ExpireTime > DateTime.Now.AddYears(1), $"expire={inApp.ExpireTime:O}");

        var mail = rows.First(e => e.Channel.EqualIgnoreCase("Mail"));
        Assert.True(mail.Success);
        Assert.Equal((Int32)NotificationStatus.Skipped, mail.Status);
        Assert.Equal("No target.", mail.Result);

        Cleanup(result.BizId);
    }

    [Fact(DisplayName = "通知_团队共享_任一人已读即全组已读并写团队已读日志")]
    public void Publish_TeamAny_OneReadMarksAll()
    {
        var userIds = new[] { _fixture.BoundUser.Id, _fixture.LegacyUser.Id, _fixture.AdminUser.Id };
        var request = Request(userIds);
        request.Scope = NotificationScope.Any;

        var result = CreateService().Publish(request);

        Assert.Equal(3, result.Records);
        Assert.Single(result.BizIds);

        var rows = Rows(result.BizId);
        Assert.Equal(3, rows.Count);
        Assert.All(rows, e => Assert.True(e.Shared));

        var reader = userIds[0];
        var record = NotificationRecord.MarkReadByBizId(result.BizId, reader, "reader", 0, "AppPush");

        Assert.NotNull(record);

        rows = Rows(result.BizId);
        Assert.All(rows, e => Assert.True(e.Read));
        Assert.All(rows, e => Assert.Equal(reader, e.ReadBy));
        Assert.All(rows, e => Assert.Equal("AppPush", e.ReadChannel));

        var log = XCode.Membership.Log.Find(XCode.Membership.Log._.Category == "Notification"
            & XCode.Membership.Log._.Action == "TeamRead"
            & XCode.Membership.Log._.Ex4 == result.BizId);
        Assert.NotNull(log);

        Cleanup(result.BizId);
    }

    [Fact(DisplayName = "通知_团队各自已读_业务编号不同且互不影响")]
    public void Publish_TeamEach_UsesDistinctBizIds()
    {
        var userIds = new[] { _fixture.LegacyUser.Id, _fixture.LegacyUser2.Id, _fixture.AdminUser.Id };
        var request = Request(userIds);
        request.Scope = NotificationScope.Each;

        var result = CreateService().Publish(request);

        Assert.Equal(3, result.Records);
        Assert.Equal(3, result.BizIds.Count);
        Assert.Equal(3, result.BizIds.Distinct().Count());

        var first = result.BizIds.First(e => e.EndsWith(":" + userIds[0]));
        var record = NotificationRecord.MarkReadByBizId(first, userIds[0], "reader", 0, "InApp");

        Assert.NotNull(record);
        Assert.True(Rows(first).Single().Read);
        Assert.All(result.BizIds.Skip(1), bizId => Assert.All(Rows(bizId), e => Assert.False(e.Read)));

        foreach (var bizId in result.BizIds) Cleanup(bizId);
    }

    [Fact(DisplayName = "通知_全部已读_跳过团队共享消息并记录批量渠道")]
    public void MarkAllRead_SkipsShared()
    {
        var personal = Request(_fixture.BoundUser.Id);
        var personalResult = CreateService().Publish(personal);

        var shared = Request(_fixture.BoundUser.Id, _fixture.LegacyUser.Id);
        shared.Scope = NotificationScope.Any;
        var sharedResult = CreateService().Publish(shared);

        var before = Rows(personalResult.BizId).Single();
        Assert.False(before.Read);
        Assert.False(before.Shared);
        Assert.False(before.Deleted);
        Assert.Equal("InApp", before.Channel);

        var count = NotificationRecord.MarkAllRead(_fixture.BoundUser.Id, "batch", false);

        var personalRow = Rows(personalResult.BizId).Single();
        Assert.True(count >= 1, $"count={count}, read={personalRow.Read}, shared={personalRow.Shared}, user={personalRow.UserId}");
        Assert.True(Rows(personalResult.BizId).Single().Read);
        Assert.Equal("Batch", Rows(personalResult.BizId).Single().ReadChannel);
        Assert.All(Rows(sharedResult.BizId), e => Assert.False(e.Read));

        Cleanup(personalResult.BizId);
        Cleanup(sharedResult.BizId);
    }

    [Fact(DisplayName = "通知_团队共享处理_任一成员处理即全组闭环")]
    public void Handle_TeamAny_ClosesAll()
    {
        var request = Request(_fixture.BoundUser.Id, _fixture.LegacyUser.Id);
        request.Scope = NotificationScope.Any;
        var result = CreateService().Publish(request);

        var record = NotificationRecord.HandleByBizId(result.BizId, _fixture.BoundUser.Id, "handler", 0, "Done");

        Assert.NotNull(record);
        Assert.All(Rows(result.BizId), e =>
        {
            Assert.True(e.Handled);
            Assert.Equal(_fixture.BoundUser.Id, e.HandledBy);
            Assert.Equal("Done", e.HandleResult);
            Assert.True(e.Read);
        });

        var log = XCode.Membership.Log.Find(XCode.Membership.Log._.Category == "Notification"
            & XCode.Membership.Log._.Action == "Handle"
            & XCode.Membership.Log._.Ex4 == result.BizId);
        Assert.NotNull(log);

        Cleanup(result.BizId);
    }

    [Fact(DisplayName = "通知_租户隔离_只投递给租户成员")]
    public void Publish_Tenant_FiltersNonMembers()
    {
        var request = Request(_fixture.BoundUser.Id, _fixture.LegacyUser.Id);
        request.TenantId = _fixture.Tenant1.Id;

        var result = CreateService().Publish(request);

        Assert.Equal(1, result.Recipients);
        Assert.Single(Rows(result.BizId));
        Assert.Equal(_fixture.BoundUser.Id, Rows(result.BizId)[0].UserId);

        Cleanup(result.BizId);
    }

    [Fact(DisplayName = "通知_广播共享已读_拒绝发送")]
    public void Publish_BroadcastAny_Throws()
    {
        var request = Request();
        request.All = true;
        request.Scope = NotificationScope.Any;

        var ex = Assert.Throws<InvalidOperationException>(() => CreateService().Publish(request));
        Assert.Contains("scope", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "通知_同一BizId重复发布_窗口内跳过")]
    public void Publish_SameBizIdTwice_Skips()
    {
        var request = Request(_fixture.BoundUser.Id);
        request.BizId = "biz:" + Guid.NewGuid().ToString("N");

        var service = CreateService();
        var first = service.Publish(request);
        var second = service.Publish(request);

        Assert.False(first.Skipped);
        Assert.True(second.Skipped);
        Assert.Equal(0, second.Records);

        Cleanup(first.BizId);
    }

    [Fact(DisplayName = "通知_BizId幂等按租户隔离")]
    public void Publish_SameBizId_IsTenantScoped()
    {
        var bizId = "biz-tenant:" + Guid.NewGuid().ToString("N");
        var service = CreateService();

        var first = Request(_fixture.BoundUser.Id);
        first.BizId = bizId;
        var one = service.Publish(first);

        var second = Request(_fixture.BoundUser.Id);
        second.TenantId = _fixture.Tenant1.Id;
        second.BizId = bizId;
        var two = service.Publish(second);

        Assert.False(one.Skipped);
        Assert.False(two.Skipped);

        Cleanup(one.BizId);
        Cleanup(two.BizId);
    }

    [Fact(DisplayName = "通知_渠道延迟_已读后取消后续渠道")]
    public void Publish_ChannelDelay_ReadCancelsPending()
    {
        var request = Request(_fixture.BoundUser.Id);
        request.BizId = "cascade:" + Guid.NewGuid().ToString("N");
        request.Channels = ["InApp", "Mail"];
        request.ChannelDelays = [0, 60];
        request.Target = "test@example.com";

        var result = CreateService().Publish(request);
        var rows = Rows(result.BizId);
        var inApp = rows.First(e => e.Channel.EqualIgnoreCase("InApp"));
        var mail = rows.First(e => e.Channel.EqualIgnoreCase("Mail"));

        Assert.True(mail.Status == (Int32)NotificationStatus.Pending);
        Assert.True(mail.NextRetryTime > DateTime.Now.AddSeconds(50));

        NotificationRecord.MarkReadByBizId(result.BizId, _fixture.BoundUser.Id, "reader", 0, "InApp");

        rows = Rows(result.BizId);
        inApp = rows.First(e => e.Channel.EqualIgnoreCase("InApp"));
        mail = rows.First(e => e.Channel.EqualIgnoreCase("Mail"));
        Assert.True(inApp.Read);
        Assert.True(mail.Read);
        Assert.Equal((Int32)NotificationStatus.Skipped, mail.Status);
        Assert.Contains("read", mail.Result, StringComparison.OrdinalIgnoreCase);

        Cleanup(result.BizId);
    }

    [Fact(DisplayName = "通知_延迟未到_调度器不发送")]
    public async System.Threading.Tasks.Task Dispatcher_HonorsChannelDelay()
    {
        var channel = new FakeChannel();
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        services.AddSingleton<INotificationChannel>(channel);
        var dispatcher = new NotificationDispatcher(services.BuildServiceProvider());

        var request = Request(_fixture.BoundUser.Id);
        request.Channels = ["InApp", "TestChannel"];
        request.ChannelDelays = [0, 120];
        request.Target = "https://example.test/hook";

        var result = CreateService().Publish(request);
        await dispatcher.DispatchOnce();

        Assert.False(channel.Called);
        Assert.Equal((Int32)NotificationStatus.Pending, Rows(result.BizId).First(e => e.Channel == "TestChannel").Status);

        Cleanup(result.BizId);
    }

    [Fact(DisplayName = "通知_已读后调度器不再发送后续渠道")]
    public async System.Threading.Tasks.Task Dispatcher_ReadCancelsPending()
    {
        var channel = new FakeChannel();
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        services.AddSingleton<INotificationChannel>(channel);
        var dispatcher = new NotificationDispatcher(services.BuildServiceProvider());

        var request = Request(_fixture.BoundUser.Id);
        request.BizId = "cascade-dispatch:" + Guid.NewGuid().ToString("N");
        request.Channels = ["InApp", "TestChannel"];
        request.ChannelDelays = [0, 0];
        request.Target = "https://example.test/hook";

        var result = CreateService().Publish(request);
        NotificationRecord.MarkReadByBizId(result.BizId, _fixture.BoundUser.Id, "reader", 0, "InApp");

        await dispatcher.DispatchOnce();

        var row = Rows(result.BizId).First(e => e.Channel == "TestChannel");
        Assert.False(channel.Called);
        Assert.Equal((Int32)NotificationStatus.Skipped, row.Status);

        Cleanup(result.BizId);
    }

    [Fact(DisplayName = "通知_模板渲染_站内信与短信使用不同摘要")]
    public void Publish_Template_RendersAndUsesSmsSummary()
    {
        var code = "tpl:" + Guid.NewGuid().ToString("N")[..8];
        var templates = new NotificationTemplateRegistry();
        templates.Save(0, new NotificationTemplate
        {
            Code = code,
            Title = "任务{name}",
            Content = "任务{name}已经完成，请查看",
            SmsContent = "{name}完成",
            Channels = "InApp,Sms",
        });

        var request = Request(_fixture.BoundUser.Id);
        request.TemplateCode = code;
        request.Data = new { name = "部署" };
        request.Channels = null;
        request.Title = null;
        request.Content = null;

        var result = CreateService().Publish(request);

        var rows = Rows(result.BizId);
        var inApp = rows.First(e => e.Channel.EqualIgnoreCase("InApp"));
        Assert.Equal("任务部署", inApp.Title);
        Assert.Equal("任务部署已经完成，请查看", inApp.Content);

        var sms = rows.First(e => e.Channel.EqualIgnoreCase("Sms"));
        Assert.Equal("部署完成", sms.Content);
        Assert.Contains("部署完成", sms.Data);

        NotificationRecord.Delete(NotificationRecord._.BizId == result.BizId);
        templates.Delete(0, code);
    }

    [Fact(DisplayName = "通知_用户偏好_只投递允许的渠道")]
    public void Publish_UserPreference_FiltersChannels()
    {
        var category = "Pref" + Guid.NewGuid().ToString("N")[..6];
        var service = CreateService();
        service.SavePreferences(_fixture.BoundUser.Id, category, "InApp");

        var request = Request(_fixture.BoundUser.Id);
        request.Category = category;
        request.Channels = ["InApp", "Mail"];

        var result = service.Publish(request);
        var rows = Rows(result.BizId);

        Assert.Single(rows);
        Assert.Equal("InApp", rows[0].Channel);

        Cleanup(result.BizId);
        Parameter.Delete(Parameter._.UserId == _fixture.BoundUser.Id
            & Parameter._.Category == "Notify"
            & Parameter._.Name == category);
    }

    [Fact(DisplayName = "通知_外部渠道调度_发送成功并记录结果")]
    public async System.Threading.Tasks.Task Dispatcher_SendsExternalChannel()
    {
        var channel = new FakeChannel();
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        services.AddSingleton<INotificationChannel>(channel);
        var dispatcher = new NotificationDispatcher(services.BuildServiceProvider());

        var request = Request(_fixture.BoundUser.Id);
        request.Channels = ["TestChannel"];
        request.Target = "https://example.test/hook";
        var result = CreateService().Publish(request);

        var row = Rows(result.BizId).Single();
        Assert.Equal((Int32)NotificationStatus.Pending, row.Status);

        await dispatcher.DispatchOnce();

        row = Rows(result.BizId).Single();
        Assert.True(channel.Called);
        Assert.Equal("测试通知", channel.LastRecord.Title);
        Assert.Equal("测试内容", channel.LastRecord.Content);
        Assert.Equal("https://example.test/hook", channel.LastRecord.Target);
        Assert.True(row.Success);
        Assert.Equal((Int32)NotificationStatus.Success, row.Status);
        Assert.True(row.Attempts >= 1);
        Assert.True(row.SentTime > DateTime.Now.AddMinutes(-1));

        Cleanup(result.BizId);
    }

    [Fact(DisplayName = "通知_Webhook渠道_真实HTTP投递成功")]
    public async Task Dispatcher_Webhook_DeliversOverHttp()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        var app = builder.Build();
        var received = new TaskCompletionSource<String>(TaskCreationOptions.RunContinuationsAsynchronously);
        app.MapPost("/hook", async context =>
        {
            using var reader = new StreamReader(context.Request.Body, Encoding.UTF8);
            received.TrySetResult(await reader.ReadToEndAsync());
            await context.Response.WriteAsync("ok");
        });
        await app.StartAsync();

        NotificationPublishResult? result = null;
        try
        {
            var services = new ServiceCollection();
            services.AddSingleton<INotificationChannel>(new WebhookNotificationChannel(NotificationChannels.Webhook));
            using var provider = services.BuildServiceProvider();
            var dispatcher = new NotificationDispatcher(provider);

            var request = Request(_fixture.BoundUser.Id);
            request.Channels = [NotificationChannels.Webhook];
            request.Target = app.Urls.First() + "/hook";
            result = CreateService().Publish(request);

            await dispatcher.DispatchOnce();

            var body = await received.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Contains("测试通知", body);
            Assert.Contains("测试内容", body);

            var row = Rows(result.BizId).Single();
            Assert.True(row.Success);
            Assert.Equal((Int32)NotificationStatus.Success, row.Status);
        }
        finally
        {
            if (result != null) Cleanup(result.BizId);
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }

    [Fact(DisplayName = "通知_投递锁被占用_调度器跳过")]
    public async Task Dispatcher_LockHeld_Skips()
    {
        var channel = new FakeChannel();
        var cache = new MemoryCache();
        var services = new ServiceCollection();
        services.AddSingleton<ICacheProvider>(new TestCacheProvider { Cache = cache });
        services.AddSingleton<INotificationChannel>(channel);
        using var provider = services.BuildServiceProvider();
        var dispatcher = new NotificationDispatcher(provider);

        var request = Request(_fixture.BoundUser.Id);
        request.Channels = ["TestChannel"];
        request.Target = "https://example.test/hook";
        var result = CreateService().Publish(request);

        try
        {
            using var held = cache.AcquireLock("CubeNotificationDispatch", 0, 120_000, false);
            Assert.NotNull(held);

            var count = await dispatcher.DispatchOnce();

            Assert.Equal(0, count);
            Assert.False(channel.Called);
            Assert.Equal((Int32)NotificationStatus.Pending, Rows(result.BizId).Single().Status);
        }
        finally
        {
            Cleanup(result.BizId);
        }
    }

    [Fact(DisplayName = "通知_外部渠道失败_按退避重试且最多三次")]
    public async Task Dispatcher_Failure_RetriesWithBackoff()
    {
        var channel = new FailingChannel();
        var services = new ServiceCollection();
        services.AddSingleton<INotificationChannel>(channel);
        using var provider = services.BuildServiceProvider();
        var dispatcher = new NotificationDispatcher(provider);

        var request = Request(_fixture.BoundUser.Id);
        request.Channels = ["FailChannel"];
        request.Target = "https://example.test/hook";
        var result = CreateService().Publish(request);

        try
        {
            await dispatcher.DispatchOnce();
            var row = Rows(result.BizId).Single();
            Assert.Equal(1, row.Attempts);
            Assert.Equal((Int32)NotificationStatus.Failed, row.Status);
            Assert.True(row.NextRetryTime > DateTime.Now.AddSeconds(50));

            row.NextRetryTime = DateTime.Now.AddSeconds(-1);
            row.Update();
            await dispatcher.DispatchOnce();
            row = Rows(result.BizId).Single();
            Assert.Equal(2, row.Attempts);
            Assert.True(row.NextRetryTime > DateTime.Now.AddMinutes(4));

            row.NextRetryTime = DateTime.Now.AddSeconds(-1);
            row.Update();
            await dispatcher.DispatchOnce();
            row = Rows(result.BizId).Single();
            Assert.Equal(3, row.Attempts);
            Assert.Equal((Int32)NotificationStatus.Failed, row.Status);
            Assert.True(row.NextRetryTime > DateTime.Now.AddHours(23));

            await dispatcher.DispatchOnce();
            Assert.Equal(3, channel.Calls);
            Assert.Equal(3, row.Attempts);
        }
        finally
        {
            Cleanup(result.BizId);
        }
    }

    [Fact(DisplayName = "通知_外部渠道待投递_发布Dispatch事件")]
    public async Task Publish_PendingExternal_PublishesDispatch()
    {
        var factory = new FakeEventBusFactory();
        var services = new ServiceCollection();
        services.AddSingleton<IEventBusFactory>(factory);
        using var provider = services.BuildServiceProvider();
        using var bus = new NotificationBus(provider, "TestNotification_" + Guid.NewGuid().ToString("N"));

        var tcs = new TaskCompletionSource<NotificationEvent>(TaskCreationOptions.RunContinuationsAsynchronously);
        bus.Dispatch += (s, e) => tcs.TrySetResult(e);
        bus.Start();

        var service = new NotificationService(new NotificationRecipientResolver(), new NotificationTemplateRegistry(), bus);
        var request = Request(_fixture.BoundUser.Id);
        request.Channels = ["TestChannel"];
        request.Target = "https://example.test/hook";
        var result = service.Publish(request);

        try
        {
            var actual = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal(NotificationEventKind.Dispatch, actual.Kind);
        }
        finally
        {
            Cleanup(result.BizId);
        }
    }

    [Fact(DisplayName = "通知_SSE票据_一次性且绑定用户")]
    public void StreamTicket_IsSingleUseAndBoundToUser()
    {
        var tickets = new NotificationStreamTicketService(new TestCacheProvider());
        var ticket = tickets.Issue(_fixture.BoundUser.Id, "127.0.0.1");

        Assert.True(tickets.Consume(ticket, _fixture.BoundUser.Id, "127.0.0.1"));
        Assert.False(tickets.Consume(ticket, _fixture.BoundUser.Id, "127.0.0.1"));
        Assert.False(tickets.Consume(ticket, _fixture.LegacyUser.Id, "127.0.0.1"));
        Assert.False(tickets.Consume(null, _fixture.BoundUser.Id, "127.0.0.1"));
    }

    [Fact(DisplayName = "通知_设备登记_超长令牌用哈希键并可注销")]
    public void RegisterDevice_LongToken()
    {
        var token = new String('a', 120);
        var service = CreateService();

        service.RegisterDevice(_fixture.BoundUser.Id, token, "Android");

        var parameter = Parameter.FindAllByUserIdAndCategory(_fixture.BoundUser.Id, "NotifyDevice")
            .FirstOrDefault(e => e.LongValue == token);
        Assert.NotNull(parameter);
        Assert.Equal("Android", parameter.Value);
        Assert.True(parameter.Name.Length <= 50);

        Assert.Equal(1, service.UnregisterDevice(_fixture.BoundUser.Id, token));
        Assert.Equal(0, service.UnregisterDevice(_fixture.BoundUser.Id, token));
    }

    private class FakeChannel : INotificationChannel
    {
        public Boolean Called => Calls > 0;

        public Int32 Calls { get; private set; }

        public NotificationRecord LastRecord { get; private set; }

        public String Name => "TestChannel";

        public Task<NotificationSendResult> SendAsync(NotificationRecord record, System.Threading.CancellationToken cancellationToken = default)
        {
            Calls++;
            LastRecord = record;

            return Task.FromResult(NotificationSendResult.Ok("sent"));
        }
    }

    private class FailingChannel : INotificationChannel
    {
        public Int32 Calls { get; private set; }

        public String Name => "FailChannel";

        public Task<NotificationSendResult> SendAsync(NotificationRecord record, System.Threading.CancellationToken cancellationToken = default)
        {
            Calls++;

            return Task.FromResult(NotificationSendResult.Fail("simulated failure"));
        }
    }

    private sealed class FakeEventBusFactory : IEventBusFactory
    {
        public EventBus<NotificationEvent> Bus { get; } = new();

        public IEventBus<TEvent>? CreateEventBus<TEvent>(String topic, String clientId = "")
            => typeof(TEvent) == typeof(NotificationEvent) ? (IEventBus<TEvent>)(Object)Bus : null;
    }
}
