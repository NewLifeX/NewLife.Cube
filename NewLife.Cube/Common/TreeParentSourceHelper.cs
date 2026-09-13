using System;
using System.Collections.Generic;

namespace NewLife.Cube;

/// <summary>树形实体父级候选构建</summary>
/// <remarks>
/// 供控制器为自引用父级字段（约定 ParentID/ParentId）自动配置数据源，使新增/编辑表单可直接下拉选择，
/// 而不必手填父级编号。层级路径采用「根/…/父」形式，避免同级同名节点难以分辨；
/// 并排除当前节点自身及其全部后代，避免把自己或子孙选为父级而形成环。
/// </remarks>
internal static class TreeParentSourceHelper
{
    /// <summary>父链上溯的最大层数。脏数据成环时用于兜底</summary>
    private const Int32 MaxDepth = 32;

    /// <summary>构建父级候选字典</summary>
    /// <param name="rows">同表全部行（编号、父级编号、显示名）</param>
    /// <param name="excludeId">需排除的节点编号（含其全部后代）。0 表示不排除</param>
    /// <returns>编号到层级路径的映射；无有效行时为空字典</returns>
    internal static Dictionary<Int32, String> Build(IEnumerable<(Int32 Id, Int32 ParentId, String Name)> rows, Int32 excludeId = 0)
    {
        var map = new Dictionary<Int32, (Int32 ParentId, String Name)>();
        foreach (var item in rows)
        {
            if (item.Id <= 0) continue;

            map[item.Id] = (item.ParentId, item.Name ?? item.Id + "");
        }

        var dict = new Dictionary<Int32, String>();
        foreach (var item in map)
        {
            // 排除自身与后代：沿父链上溯，遇到待排除节点即跳过
            if (excludeId > 0 && IsSelfOrDescendant(map, item.Key, excludeId)) continue;

            dict[item.Key] = BuildPath(map, item.Key);
        }

        return dict;
    }

    /// <summary>沿父链上溯拼接层级路径，自环与脏数据仅截断不抛错</summary>
    /// <param name="map">编号到（父级编号、显示名）的映射</param>
    /// <param name="id">起始节点编号</param>
    /// <returns>「根/…/父」形式的路径</returns>
    private static String BuildPath(IDictionary<Int32, (Int32 ParentId, String Name)> map, Int32 id)
    {
        var parts = new List<String>();
        var cur = id;
        var guard = 0;
        while (cur > 0 && map.TryGetValue(cur, out var node) && guard++ < MaxDepth)
        {
            parts.Insert(0, node.Name);

            // 脏数据：父级指向自身，直接截断
            if (node.ParentId == cur) break;

            cur = node.ParentId;
        }

        return String.Join("/", parts);
    }

    /// <summary>判断节点是否为待排除节点自身或其后代</summary>
    /// <param name="map">编号到（父级编号、显示名）的映射</param>
    /// <param name="id">待判断节点编号</param>
    /// <param name="target">待排除节点编号</param>
    /// <returns></returns>
    private static Boolean IsSelfOrDescendant(IDictionary<Int32, (Int32 ParentId, String Name)> map, Int32 id, Int32 target)
    {
        var cur = id;
        var guard = 0;
        while (cur > 0 && guard++ < MaxDepth)
        {
            if (cur == target) return true;
            if (!map.TryGetValue(cur, out var node) || node.ParentId == cur) return false;

            cur = node.ParentId;
        }

        return false;
    }
}
