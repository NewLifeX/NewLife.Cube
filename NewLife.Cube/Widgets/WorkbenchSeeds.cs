using System.Text.Json.Nodes;

namespace NewLife.Cube.Widgets;

/// <summary>系统默认工作台种子（不入库）。id 稳定为 seed-{Name}。</summary>
public static class WorkbenchSeeds
{
    static readonly Lazy<String> AdminLazy = new(BuildAdmin);
    static readonly Lazy<String> MemberLazy = new(BuildMember);

    /// <summary>系统角色默认墙</summary>
    public static String Admin => AdminLazy.Value;

    /// <summary>普通用户默认墙</summary>
    public static String Member => MemberLazy.Value;

    static String BuildAdmin()
    {
        var widgets = new JsonArray();
        var order = 0;
        foreach (var name in new[] { "UserCount", "TodayLogin", "OnlineCount", "Log24h", "Error24h", "CpuRate" })
            widgets.Add(Named(name, 2, order++, h: 1));
        // Monitor 与 QuickLink 同高；内容卡半行配对 w=6（卡内整行排版）
        widgets.Add(Named("Monitor", 8, order++, h: 3, kind: "monitorChart", title: "性能监控", icon: "fa-line-chart"));
        widgets.Add(Named("QuickLink", 4, order++, h: 3, kind: "quickLinks", title: "快捷入口", icon: "fa-th-large"));
        widgets.Add(Named("Inbox", 6, order++, h: 2, kind: "inbox", title: "站内信", icon: "fa-bell"));
        widgets.Add(Named("SysInfo", 6, order++, h: 2, kind: "kvList", title: "系统信息", icon: "fa-server"));
        widgets.Add(Named("LoginLog", 6, order++, h: 2, kind: "loginLog", title: "登录与在线", icon: "fa-users"));
        widgets.Add(Named("Profile", 6, order++, h: 2, kind: "profile", title: "个人信息", icon: "fa-user-circle"));
        return Root(widgets);
    }

    static String BuildMember()
    {
        var widgets = new JsonArray();
        var order = 0;
        widgets.Add(Named("MyLogins", 3, order++, h: 1, color: "green", icon: "fa-sign-in", title: "我的登录"));
        widgets.Add(Named("MyDays", 3, order++, h: 1, color: "blue", icon: "fa-calendar", title: "注册天数"));
        widgets.Add(Named("Inbox", 6, order++, h: 2, kind: "inbox", title: "站内信", icon: "fa-bell"));
        widgets.Add(Named("Profile", 6, order++, h: 2, kind: "profile", title: "个人信息", icon: "fa-user-circle"));
        widgets.Add(Named("QuickLink", 12, order++, h: 3, kind: "quickLinks", title: "快捷入口", icon: "fa-th-large"));
        return Root(widgets);
    }

    static String Root(JsonArray widgets)
    {
        var root = new JsonObject { ["version"] = 1, ["widgets"] = widgets };
        return root.ToJsonString();
    }

    static readonly Dictionary<String, (String Title, String Kind, String Color, String Icon)> Meta = new(StringComparer.OrdinalIgnoreCase)
    {
        ["UserCount"] = ("用户总数", "metricCard", "blue", "fa-users"),
        ["TodayLogin"] = ("今日登录", "metricCard", "green", "fa-sign-in"),
        ["OnlineCount"] = ("在线用户", "metricCard", "cyan", "fa-user-circle-o"),
        ["Log24h"] = ("24h日志", "metricCard", "grey", "fa-file-text-o"),
        ["Error24h"] = ("24h异常", "metricCard", "red", "fa-exclamation-triangle"),
        ["CpuRate"] = ("CPU使用率", "metricCard", "orange", "fa-tachometer"),
        ["MyLogins"] = ("我的登录", "metricCard", "green", "fa-sign-in"),
        ["MyDays"] = ("注册天数", "metricCard", "blue", "fa-calendar"),
        ["QuickLink"] = ("快捷入口", "quickLinks", "", "fa-th-large"),
        ["Profile"] = ("个人信息", "profile", "", "fa-user-circle"),
        ["SysInfo"] = ("系统信息", "kvList", "", "fa-server"),
        ["LoginLog"] = ("登录与在线", "loginLog", "", "fa-users"),
        ["Monitor"] = ("性能监控", "monitorChart", "", "fa-line-chart"),
        ["Inbox"] = ("站内信", "inbox", "", "fa-bell"),
    };

    static JsonObject Named(String name, Int32 w, Int32 order, Int32 h = 1, String kind = null, String title = null, String icon = null, String color = null)
    {
        Meta.TryGetValue(name, out var m);
        var k = !kind.IsNullOrEmpty() ? kind : m.Kind;
        var t = !title.IsNullOrEmpty() ? title : m.Title;
        var ic = !icon.IsNullOrEmpty() ? icon : m.Icon;
        var c = !color.IsNullOrEmpty() ? color : m.Color;
        var style = new JsonObject();
        if (!ic.IsNullOrEmpty()) style["icon"] = ic;
        if (!c.IsNullOrEmpty()) style["color"] = c;
        return new JsonObject
        {
            ["id"] = "seed-" + name,
            ["kind"] = k.IsNullOrEmpty() ? "metricCard" : k,
            ["title"] = t.IsNullOrEmpty() ? name : t,
            ["layout"] = new JsonObject { ["w"] = w, ["h"] = h, ["order"] = order },
            ["source"] = new JsonObject { ["provider"] = "named", ["widgetName"] = name },
            ["query"] = new JsonObject(),
            ["style"] = style,
        };
    }
}
