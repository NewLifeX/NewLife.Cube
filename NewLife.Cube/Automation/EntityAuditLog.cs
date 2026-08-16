using NewLife.Cube.Entity;
using NewLife.Data;
using NewLife.Reflection;
using XCode;
using XCode.Membership;
using XLog = XCode.Membership.Log;

namespace NewLife.Cube.Automation;

/// <summary>
/// 实体审计日志 Category / Action 别名解析。
/// XCode <see cref="LogProvider"/> 写实体变更时 Category 多为 DisplayName（如「用户」），
/// 前端历史 Tab / 评论则用 typePath（如 Admin/User）；查询时需一并兼容。
/// </summary>
public static class EntityAuditLog
{
    /// <summary>解析 typePath / 显示名 / 类型名等 Category 别名</summary>
    public static String[] ResolveCategories(String categoryOrTypePath)
    {
        var set = new HashSet<String>(StringComparer.OrdinalIgnoreCase);
        if (categoryOrTypePath.IsNullOrEmpty()) return [];
        var raw = categoryOrTypePath.Trim();
        set.Add(raw);
        var path = AutomationPaths.NormalizeTypePath(raw);
        if (!path.IsNullOrEmpty())
        {
            set.Add(path);
            set.Add("/" + path);
            var type = AutomationExecutor.ResolveEntityType(path);
            if (type != null)
            {
                set.Add(type.Name);
                var dn = type.GetDisplayName();
                if (!dn.IsNullOrEmpty()) set.Add(dn);
                var des = type.GetDescription();
                if (!des.IsNullOrEmpty())
                {
                    var shortName = des.Split('。', '.', '，', ',')[0].Trim();
                    if (!shortName.IsNullOrEmpty()) set.Add(shortName);
                }
                try
                {
                    var fact = EntityFactory.CreateFactory(type);
                    var tableDn = fact?.Table?.DataTable?.DisplayName + "";
                    if (!tableDn.IsNullOrEmpty()) set.Add(tableDn);
                    var tableDes = fact?.Table?.Description + "";
                    if (!tableDes.IsNullOrEmpty())
                    {
                        var shortTable = tableDes.Split('。', '.', '，', ',')[0].Trim();
                        if (!shortTable.IsNullOrEmpty()) set.Add(shortTable);
                    }
                }
                catch { /* ignore */ }
            }
            try
            {
                var menu = AutomationAuth.FindMenu(path);
                if (menu != null && !menu.DisplayName.IsNullOrEmpty()) set.Add(menu.DisplayName);
                if (menu != null && !menu.Name.IsNullOrEmpty()) set.Add(menu.Name);
            }
            catch { /* ignore */ }
        }
        return set.Where(e => !e.IsNullOrEmpty()).ToArray();
    }

    /// <summary>前端筛选值 → 实际 Action 别名（中英混用）</summary>
    public static String[] ResolveActions(String action)
    {
        if (action.IsNullOrEmpty()) return [];
        var a = action.Trim();
        return a.ToLowerInvariant() switch
        {
            "insert" or "add" or "添加" or "新增" => ["Insert", "Add", "添加", "新增"],
            "update" or "edit" or "修改" or "编辑" => ["Update", "Edit", "修改", "编辑"],
            "delete" or "删除" => ["Delete", "删除"],
            "automation" or "自动化" => [AutomationFlowLog.ActionName, "自动化"],
            _ => [a],
        };
    }

    /// <summary>按别名搜索审计日志（供历史 Tab / 自动化 Runs）</summary>
    public static IList<XLog> Search(
        String categoryOrTypePath,
        String action,
        Int64 linkId,
        Boolean? success,
        Int32 userId,
        DateTime start,
        DateTime end,
        String key,
        PageParameter page)
    {
        var cats = ResolveCategories(categoryOrTypePath);
        var acts = ResolveActions(action);

        if (cats.Length <= 1 && acts.Length <= 1)
            return XLog.Search(cats.FirstOrDefault(), acts.FirstOrDefault(), linkId, success, userId, start, end, key, page);

        var merged = new List<XLog>();
        var seen = new HashSet<Int64>();
        var pageSize = page?.PageSize > 0 ? page.PageSize : 20;
        var pageIndex = page?.PageIndex > 0 ? page.PageIndex : 1;
        var per = Math.Max(pageSize * pageIndex, pageSize) + pageSize;
        foreach (var cat in cats)
        {
            if (acts.Length <= 1)
            {
                var p = new PageParameter
                {
                    PageIndex = 1,
                    PageSize = per,
                    RetrieveTotalCount = false,
                    OrderBy = "ID Desc",
                };
                foreach (var row in XLog.Search(cat, acts.FirstOrDefault(), linkId, success, userId, start, end, key, p))
                    if (seen.Add(row.ID)) merged.Add(row);
            }
            else
            {
                foreach (var act in acts)
                {
                    var p2 = new PageParameter
                    {
                        PageIndex = 1,
                        PageSize = per,
                        RetrieveTotalCount = false,
                        OrderBy = "ID Desc",
                    };
                    foreach (var row in XLog.Search(cat, act, linkId, success, userId, start, end, key, p2))
                        if (seen.Add(row.ID)) merged.Add(row);
                }
            }
        }

        merged = merged.OrderByDescending(e => e.ID).ToList();
        if (page != null)
        {
            page.TotalCount = merged.Count;
            var skip = (pageIndex - 1) * pageSize;
            if (skip < 0) skip = 0;
            return merged.Skip(skip).Take(pageSize).ToList();
        }
        return merged;
    }
}
