using NewLife;
using NewLife.Serialization;
using XCode.Membership;

namespace NewLife.Cube.Widgets.System;

/// <summary>快捷入口。常用功能链接，管理员专属链接由视图按角色过滤；附带最近访问页面</summary>
[Widget("QuickLink", "快捷入口", Icon = "fa-th-large", Cols = 4, Sort = 80, Category = "通用", WidgetType = WidgetTypes.Content)]
public class QuickLinkWidget : IWidget
{
    #region 属性
    /// <summary>最近访问参数分类。UserID=当前用户，Name=pages，LongValue=JSON 列表</summary>
    public const String RecentCategory = "Visit.Recent";

    /// <summary>最近访问数量上限</summary>
    public const Int32 MaxRecent = 10;
    #endregion

    /// <summary>获取组件数据</summary>
    /// <returns>快捷链接与最近访问列表匿名对象</returns>
    public Object GetData()
    {
        // 最近访问按当前用户读取；未登录（如测试环境）返回空列表
        var user = ManageProvider.User;
        var recent = user == null ? new List<RecentVisit>() : GetRecent(user.ID, MaxRecent);

        return new
        {
            Links = new[]
            {
                new { Name = "个人中心", Url = "/Admin/User/Info", Icon = "fa-user", AdminOnly = false },
                new { Name = "系统设置", Url = "/Admin/Cube", Icon = "fa-cog", AdminOnly = true },
                new { Name = "用户管理", Url = "/Admin/User", Icon = "fa-users", AdminOnly = true },
                new { Name = "审计日志", Url = "/Admin/Log", Icon = "fa-history", AdminOnly = true },
                new { Name = "在线用户", Url = "/Admin/UserOnline", Icon = "fa-user-circle", AdminOnly = true },
                new { Name = "定时作业", Url = "/Cube/CronJob", Icon = "fa-clock-o", AdminOnly = true },
            },
            Recent = recent,
        };
    }

    #region 最近访问
    /// <summary>记录页面访问。读列表→去重置顶→计数→截断上限→写回 Parameter（每用户单行 JSON）</summary>
    /// <param name="userId">用户编号。小于等于0忽略</param>
    /// <param name="name">页面名称</param>
    /// <param name="url">页面地址</param>
    /// <param name="icon">菜单图标，可选</param>
    public static void RecordVisit(Int32 userId, String name, String url, String icon = null)
    {
        if (userId <= 0 || name.IsNullOrEmpty() || url.IsNullOrEmpty()) return;

        var list = GetRecent(userId, Int32.MaxValue).ToList();

        var item = list.FirstOrDefault(e => e.Url == url);
        if (item != null)
        {
            // 已存在：更新名称/图标，置顶并计数
            item.Name = name;
            item.Icon = icon;
            item.Count++;
            list.Remove(item);
            list.Insert(0, item);
        }
        else
        {
            list.Insert(0, new RecentVisit { Name = name, Url = url, Icon = icon, Count = 1 });
        }

        // 截断上限
        if (list.Count > MaxRecent) list.RemoveRange(MaxRecent, list.Count - MaxRecent);

        var p = Parameter.GetOrAdd(userId, RecentCategory, "pages");
        p.LongValue = list.ToJson();
        p.Save();
    }

    /// <summary>获取最近访问列表。列表顺序即最近访问顺序（最新在前），无数据返回空列表</summary>
    /// <param name="userId">用户编号</param>
    /// <param name="max">最大条数。小于等于0返回全部</param>
    /// <returns>最近访问列表</returns>
    public static IList<RecentVisit> GetRecent(Int32 userId, Int32 max = MaxRecent)
    {
        if (userId <= 0) return new List<RecentVisit>();

        var p = Parameter.FindByUserIDAndCategoryAndName(userId, RecentCategory, "pages");
        if (p == null) return new List<RecentVisit>();

        var json = p.Value ?? p.LongValue;
        if (json.IsNullOrEmpty()) return new List<RecentVisit>();

        var list = json.ToJsonEntity<List<RecentVisit>>();
        if (list == null) return new List<RecentVisit>();

        if (max > 0 && list.Count > max) list = list.Take(max).ToList();

        return list;
    }
    #endregion
}
