using Microsoft.Extensions.DependencyInjection;
using NewLife.Cube.Services.Notifications;
using Xunit;

namespace NewLife.Cube.Tests.Services;

/// <summary>通知依赖注入测试。验证 AddCube 后模块可直接解析 INotifier</summary>
public class NotificationDiTests
{
    [Fact(DisplayName = "通知DI_AddCube_解析统一通知入口")]
    public void AddCube_ResolvesNotifier()
    {
        var services = new ServiceCollection();
        services.AddCube();
        using var provider = services.BuildServiceProvider();

        var notifier = provider.GetRequiredService<INotifier>();
        var bus = provider.GetRequiredService<NotificationBus>();

        Assert.IsType<NotificationService>(notifier);
        Assert.NotNull(bus);
    }
}
