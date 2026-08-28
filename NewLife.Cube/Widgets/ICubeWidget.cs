using NewLife.Cube.Automation;
using XCode.Membership;

namespace NewLife.Cube.Widgets;

/// <summary>SPA 平台 Widget 数据提供方（OSC-2608280e9e）。不得引用 CubeNC Razor IWidget。</summary>
public interface ICubeWidget
{
    /// <summary>获取部件数据</summary>
    Object GetData(WidgetContext ctx);
}

/// <summary>Widget 取数上下文</summary>
public sealed class WidgetContext
{
    /// <summary>当前用户</summary>
    public IUser User { get; init; }

    /// <summary>宿主筛选（洞察槽 viewFilter）</summary>
    public ViewFilterDto HostFilter { get; init; }

    /// <summary>宿主实体路径</summary>
    public String HostTypePath { get; init; }
}
