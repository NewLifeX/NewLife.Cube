using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NewLife.Caching;
using NewLife.Cube.Services.Notifications;
using NewLife.Messaging;
using Xunit;

namespace NewLife.Cube.Tests.Services;

/// <summary>通知事件总线测试。覆盖事件发布/接收与无总线降级</summary>
public class NotificationBusTests
{
    private sealed class FakeEventBusFactory : IEventBusFactory
    {
        public EventBus<NotificationEvent> Bus { get; } = new();

        public IEventBus<TEvent>? CreateEventBus<TEvent>(String topic, String clientId = "")
            => typeof(TEvent) == typeof(NotificationEvent) ? (IEventBus<TEvent>)(Object)Bus : null;
    }

    [Fact(DisplayName = "通知总线_发布后收到事件")]
    public async Task Publish_ReceivesEvent()
    {
        var factory = new FakeEventBusFactory();
        var services = new ServiceCollection();
        services.AddSingleton<IEventBusFactory>(factory);
        using var provider = services.BuildServiceProvider();
        using var bus = new NotificationBus(provider, "TestNotification_" + Guid.NewGuid().ToString("N"));

        var tcs = new TaskCompletionSource<NotificationEvent>(TaskCreationOptions.RunContinuationsAsynchronously);
        bus.Created += (s, e) => tcs.TrySetResult(e);
        bus.Start();

        await bus.PublishAsync(new NotificationEvent { Id = 123, UserId = 42 });

        var actual = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(123, actual.Id);
        Assert.Equal(42, actual.UserId);
    }

    [Fact(DisplayName = "通知总线_缓存提供者_发布后收到事件")]
    public async Task Publish_WithCacheProvider_ReceivesEvent()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ICacheProvider, CacheProvider>();
        using var provider = services.BuildServiceProvider();
        using var bus = new NotificationBus(provider, "TestNotification_" + Guid.NewGuid().ToString("N"));

        var tcs = new TaskCompletionSource<NotificationEvent>(TaskCreationOptions.RunContinuationsAsynchronously);
        bus.Created += (s, e) =>
        {
            if (e.Id == 456) tcs.TrySetResult(e);
        };
        bus.Start();

        await bus.PublishAsync(new NotificationEvent { Id = 456, UserId = 7 });

        var actual = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(456, actual.Id);
        Assert.Equal(7, actual.UserId);
    }

    [Fact(DisplayName = "通知总线_投递事件_只触发Dispatch")]
    public async Task Publish_Dispatch_RaisesDispatchOnly()
    {
        var factory = new FakeEventBusFactory();
        var services = new ServiceCollection();
        services.AddSingleton<IEventBusFactory>(factory);
        using var provider = services.BuildServiceProvider();
        using var bus = new NotificationBus(provider, "TestNotification_" + Guid.NewGuid().ToString("N"));

        var created = 0;
        var dispatch = new TaskCompletionSource<NotificationEvent>(TaskCreationOptions.RunContinuationsAsynchronously);
        bus.Created += (s, e) => created++;
        bus.Dispatch += (s, e) => dispatch.TrySetResult(e);
        bus.Start();

        await bus.PublishAsync(new NotificationEvent { Kind = NotificationEventKind.Dispatch, Id = 9 });

        var actual = await dispatch.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(NotificationEventKind.Dispatch, actual.Kind);
        Assert.Equal(9, actual.Id);
        Assert.Equal(0, created);
    }

    [Fact(DisplayName = "通知总线_无工厂_发布不抛异常")]
    public async Task Publish_NoFactory_DoesNotThrow()
    {
        var services = new ServiceCollection();
        using var provider = services.BuildServiceProvider();
        using var bus = new NotificationBus(provider, "TestNotification_" + Guid.NewGuid().ToString("N"));

        await bus.PublishAsync(new NotificationEvent { Id = 1 });
    }
}
