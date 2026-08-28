using NewLife;
using NewLife.Cube.Entity;
using XCode;
using XCode.Membership;
using XLog = XCode.Membership.Log;
using static XCode.Membership.Log;

namespace NewLife.Cube.Widgets.Workbench;

static class WorkbenchKpi
{
    public static Object Pack(Object value, String trend, String url) => new { value, trend, url };
}

/// <summary>用户总数</summary>
[CubeWidget("UserCount", "用户总数", Kind = "metricCard", Cols = 2, AdminOnly = true, Surfaces = "workbench", Color = "blue", Icon = "fa-users")]
public class UserCountWidget : ICubeWidget
{
    /// <inheritdoc />
    public Object GetData(WidgetContext ctx) =>
        WorkbenchKpi.Pack(User.Meta.Count.ToString("n0"), "注册用户", "/Admin/User");
}

/// <summary>今日登录</summary>
[CubeWidget("TodayLogin", "今日登录", Kind = "metricCard", Cols = 2, AdminOnly = true, Surfaces = "workbench", Color = "green", Icon = "fa-sign-in")]
public class TodayLoginWidget : ICubeWidget
{
    /// <inheritdoc />
    public Object GetData(WidgetContext ctx)
    {
        var now = DateTime.Now;
        var count = UserStat.FindByDate(DateTime.Today)?.Logins ?? 0;
        return WorkbenchKpi.Pack(count.ToString("n0"), "今日登录成功",
            $"/Admin/Log?dtStart={DateTime.Today:yyyy-MM-dd}&dtEnd={now:yyyy-MM-dd HH:mm:ss}&act=登录");
    }
}

/// <summary>在线用户</summary>
[CubeWidget("OnlineCount", "在线用户", Kind = "metricCard", Cols = 2, AdminOnly = true, Surfaces = "workbench", Color = "cyan", Icon = "fa-user-circle-o")]
public class OnlineCountWidget : ICubeWidget
{
    /// <inheritdoc />
    public Object GetData(WidgetContext ctx) =>
        WorkbenchKpi.Pack(UserOnline.FindCount().ToString(), "当前在线", "/Admin/UserOnline");
}

/// <summary>24h 日志量</summary>
[CubeWidget("Log24h", "24h日志", Kind = "metricCard", Cols = 2, AdminOnly = true, Surfaces = "workbench", Color = "grey", Icon = "fa-file-text-o")]
public class Log24hWidget : ICubeWidget
{
    /// <inheritdoc />
    public Object GetData(WidgetContext ctx)
    {
        var now = DateTime.Now;
        var snow = XLog.Meta.Factory.Snow;
        var count = XLog.FindCount(_.ID.Between(now.AddHours(-24), now, snow));
        return WorkbenchKpi.Pack(count.ToString("n0"), "最近24小时",
            $"/Admin/Log?dtStart={now.AddHours(-24):yyyy-MM-dd HH:mm:ss}&dtEnd={now:yyyy-MM-dd HH:mm:ss}");
    }
}

/// <summary>24h 异常</summary>
[CubeWidget("Error24h", "24h异常", Kind = "metricCard", Cols = 2, AdminOnly = true, Surfaces = "workbench", Color = "red", Icon = "fa-exclamation-triangle")]
public class Error24hWidget : ICubeWidget
{
    /// <inheritdoc />
    public Object GetData(WidgetContext ctx)
    {
        var now = DateTime.Now;
        var snow = XLog.Meta.Factory.Snow;
        var count = XLog.FindCount(_.ID.Between(now.AddHours(-24), now, snow) & _.Success == false);
        return WorkbenchKpi.Pack(count.ToString(), "最近24小时异常",
            $"/Admin/Log?dtStart={now.AddHours(-24):yyyy-MM-dd HH:mm:ss}&dtEnd={now:yyyy-MM-dd HH:mm:ss}&success=false");
    }
}

/// <summary>CPU 使用率</summary>
[CubeWidget("CpuRate", "CPU使用率", Kind = "metricCard", Cols = 2, AdminOnly = true, Surfaces = "workbench", Color = "orange", Icon = "fa-tachometer")]
public class CpuRateWidget : ICubeWidget
{
    /// <inheritdoc />
    public Object GetData(WidgetContext ctx)
    {
        var mi = MachineInfo.Current ?? new MachineInfo();
        var total = mi.Memory / 1024 / 1024;
        var used = total - mi.AvailableMemory / 1024 / 1024;
        var memRate = total <= 0 ? 0d : (Double)used / total * 100;
        return WorkbenchKpi.Pack(
            Math.Round(mi.CpuRate * 100, 1).ToString("0.0") + "%",
            "内存 " + Math.Round(memRate, 1).ToString("0.0") + "%",
            "/Admin/Index");
    }
}

/// <summary>我的登录次数</summary>
[CubeWidget("MyLogins", "我的登录", Kind = "metricCard", Cols = 3, Surfaces = "workbench", Color = "green", Icon = "fa-sign-in")]
public class MyLoginsWidget : ICubeWidget
{
    /// <inheritdoc />
    public Object GetData(WidgetContext ctx) =>
        WorkbenchKpi.Pack((ctx?.User?.Logins ?? 0).ToString("n0"), "累计登录次数", "/Admin/User/Info");
}

/// <summary>注册天数</summary>
[CubeWidget("MyDays", "注册天数", Kind = "metricCard", Cols = 3, Surfaces = "workbench", Color = "blue", Icon = "fa-calendar")]
public class MyDaysWidget : ICubeWidget
{
    /// <inheritdoc />
    public Object GetData(WidgetContext ctx)
    {
        var user = ctx?.User;
        var days = user != null && user.RegisterTime.Year > 2000 ? (DateTime.Now - user.RegisterTime).Days : 0;
        return WorkbenchKpi.Pack(days.ToString("n0"), "加入魔方以来", "");
    }
}
