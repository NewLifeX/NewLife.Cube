using System.Diagnostics;
using System.Reflection;
using System.Runtime.Versioning;
using NewLife;
using NewLife.Cube.Entity;
using NewLife.Data;
using NewLife.Reflection;
using XCode;
using XCode.Membership;
using XLog = XCode.Membership.Log;
using static XCode.Membership.Log;

namespace NewLife.Cube.Widgets.Workbench;

/// <summary>快捷入口。服务端按是否系统角色过滤 adminOnly 链接。</summary>
[CubeWidget("QuickLink", "快捷入口", Kind = "quickLinks", Cols = 4, Surfaces = "workbench", Icon = "fa-th-large")]
public class QuickLinkWidget : ICubeWidget
{
    static readonly (String Name, String Url, String Icon, Boolean AdminOnly)[] All =
    [
        ("个人中心", "/Admin/User/Info", "fa-user", false),
        ("系统设置", "/Admin/Cube", "fa-cog", true),
        ("用户管理", "/Admin/User", "fa-users", true),
        ("审计日志", "/Admin/Log", "fa-history", true),
        ("在线用户", "/Admin/UserOnline", "fa-user-circle", true),
        ("定时作业", "/Cube/CronJob", "fa-clock-o", true),
    ];

    /// <inheritdoc />
    public Object GetData(WidgetContext ctx)
    {
        var isSys = WorkbenchResolver.IsSystem(ctx?.User);
        var links = All.Where(e => !e.AdminOnly || isSys)
            .Select(e => new { name = e.Name, url = e.Url, icon = e.Icon, adminOnly = e.AdminOnly })
            .ToArray();
        return new { links };
    }
}

/// <summary>个人信息</summary>
[CubeWidget("Profile", "个人信息", Kind = "profile", Cols = 4, Surfaces = "workbench", Icon = "fa-user-circle")]
public class ProfileWidget : ICubeWidget
{
    /// <inheritdoc />
    public Object GetData(WidgetContext ctx)
    {
        var user = ctx?.User;
        var display = user?.DisplayName;
        if (display.IsNullOrEmpty()) display = user?.Name;
        var roleNames = (user as User)?.RoleNames;
        if (roleNames.IsNullOrEmpty()) roleNames = Role.FindByID(user?.RoleID ?? 0)?.Name;
        return new
        {
            name = user?.Name,
            displayName = display,
            roleNames,
            online = user?.Online == true,
            logins = user?.Logins ?? 0,
            lastLogin = user != null && user.LastLogin.Year > 2000 ? user.LastLogin.ToFullString() : "",
            lastLoginIP = user?.LastLoginIP,
            registerTime = user != null && user.RegisterTime.Year > 2000 ? user.RegisterTime.ToFullString() : "",
        };
    }
}

/// <summary>系统信息摘要。无 HTML。</summary>
[CubeWidget("SysInfo", "系统信息", Kind = "kvList", Cols = 4, AdminOnly = true, Surfaces = "workbench", Icon = "fa-server")]
public class SysInfoWidget : ICubeWidget
{
    /// <inheritdoc />
    public Object GetData(WidgetContext ctx)
    {
        var mi = MachineInfo.Current ?? new MachineInfo();
        var asm = Assembly.GetExecutingAssembly();
        var att = asm.GetCustomAttribute<TargetFrameworkAttribute>();
        var ver = att?.FrameworkDisplayName ?? att?.FrameworkName;
        var process = Process.GetCurrentProcess();
        var startTime = DateTime.Now.AddMilliseconds(-Environment.TickCount64);
        var asmCount = AssemblyX.GetAssemblies(null).Count();
        var items = new[]
        {
            new { label = "操作系统", value = $"{mi.OSName} {mi.OSVersion}", href = (String)null },
            new { label = "机器", value = $"{Environment.MachineName} / {Environment.UserName}", href = (String)null },
            new { label = "处理器", value = $"{mi.Processor}，{Environment.ProcessorCount} 核心", href = (String)null },
            new { label = "运行时", value = ver, href = (String)null },
            new { label = "应用", value = $"{process.ProcessName}，程序集 {asmCount} 个", href = (String)null },
            new { label = "启动时间", value = startTime.ToFullString(), href = (String)null },
            new { label = "更多信息", value = "查看完整服务器信息", href = "/Admin/Index" },
        };
        return new { items };
    }
}

/// <summary>登录与在线明细</summary>
[CubeWidget("LoginLog", "登录与在线", Kind = "loginLog", Cols = 4, AdminOnly = true, Surfaces = "workbench", Icon = "fa-users")]
public class LoginLogWidget : ICubeWidget
{
    /// <inheritdoc />
    public Object GetData(WidgetContext ctx)
    {
        var now = DateTime.Now;
        var snow = XLog.Meta.Factory.Snow;
        var start = now.AddHours(-24);
        var logins = XLog.FindAll(_.Action.Contains("登录") & _.ID.Between(start, now, snow), "ID desc", null, 0, 10);
        var onlines = UserOnline.FindAll(null, "ID desc", null, 0, 10);
        return new
        {
            logins = logins.Select(e => new { createTime = e.CreateTime, userName = e.UserName, action = e.Action, createIP = e.CreateIP }).ToArray(),
            onlines = onlines.Select(e => new { name = e.Name, createTime = e.CreateTime, oAuthProvider = e.OAuthProvider }).ToArray(),
        };
    }
}

/// <summary>性能监控当前点（0–100）。前端环形缓冲。</summary>
[CubeWidget("Monitor", "性能监控", Kind = "monitorChart", Cols = 8, AdminOnly = true, Surfaces = "workbench", Icon = "fa-line-chart")]
public class MonitorWidget : ICubeWidget
{
    /// <inheritdoc />
    public Object GetData(WidgetContext ctx)
    {
        var mi = MachineInfo.Current ?? new MachineInfo();
        var cpu = Math.Round(mi.CpuRate * 100, 1);
        var mem = mi.Memory > 0 ? Math.Round((mi.Memory - mi.AvailableMemory) * 100.0 / mi.Memory, 1) : 0;
        return new { time = DateTime.Now.ToString("HH:mm:ss"), cpu, mem };
    }
}

/// <summary>站内信。未读优先，失败返回空列表。</summary>
[CubeWidget("Inbox", "站内信", Kind = "inbox", Cols = 6, Surfaces = "workbench", Icon = "fa-bell")]
public class InboxWidget : ICubeWidget
{
    /// <inheritdoc />
    public Object GetData(WidgetContext ctx)
    {
        var user = ctx?.User;
        if (user == null) return new { unread = 0, items = Array.Empty<Object>() };
        try
        {
            var unreadPage = new PageParameter { PageIndex = 1, PageSize = 8, Sort = "Id", Desc = true, RetrieveTotalCount = true };
            var unreadList = NotificationRecord.Search(0, "InApp", user.ID, null, false, true, DateTime.MinValue, DateTime.MinValue, null, unreadPage);
            var unread = unreadPage.TotalCount;
            var items = unreadList.Select(Map).ToList();
            if (items.Count < 8)
            {
                var restPage = new PageParameter { PageIndex = 1, PageSize = 8 - items.Count, Sort = "Id", Desc = true };
                var rest = NotificationRecord.Search(0, "InApp", user.ID, null, true, true, DateTime.MinValue, DateTime.MinValue, null, restPage);
                items.AddRange(rest.Select(Map));
            }
            return new { unread, items };
        }
        catch
        {
            return new { unread = 0, items = Array.Empty<Object>() };
        }
    }

    static Object Map(NotificationRecord e) => new { id = e.Id, title = e.Title, createTime = e.CreateTime };
}
