using System.Text.Json.Nodes;
using XCode;
using XCode.Membership;

namespace NewLife.Cube.Membership;

/// <summary>解析通知/审批接收人。kind=users|roles|departments；兼容旧 mode=userId|field</summary>
/// <remarks>
/// 从 OSC-260815 自动化 notify 抽出，供自动化与 Workflow 共用。
/// 开启租户时，角色/部门展开与显式用户均须属于规则租户。
/// </remarks>
/// <example>
/// <code>
/// var ids = RecipientResolver.Resolve(to, tenantId, entity);
/// </code>
/// </example>
public static class RecipientResolver
{
    /// <summary>解析接收人用户编号集合</summary>
    /// <param name="to">to JSON：kind + users/roles/departments，或旧 mode</param>
    /// <param name="tenantId">规则/流程租户。0=不裁剪</param>
    /// <param name="entity">mode=field 时读取字段的实体，可空</param>
    /// <returns>去重后的用户 Id</returns>
    public static HashSet<Int32> Resolve(JsonObject to, Int32 tenantId, IEntity entity = null)
    {
        var ids = new HashSet<Int32>();
        if (to == null) return ids;

        var kind = (to["kind"]?.ToString() ?? "").Trim().ToLowerInvariant();
        if (kind.IsNullOrEmpty())
        {
            // 推断：仅一个非空数组时视为该 kind
            var hasU = ReadIntArray(to["users"]).Any(x => x > 0);
            var hasR = ReadIntArray(to["roles"]).Any(x => x > 0);
            var hasD = ReadIntArray(to["departments"]).Any(x => x > 0);
            var n = (hasU ? 1 : 0) + (hasR ? 1 : 0) + (hasD ? 1 : 0);
            if (n == 1)
                kind = hasU ? "users" : hasR ? "roles" : "departments";
        }

        void AddUsers()
        {
            foreach (var id in ReadIntArray(to["users"]))
            {
                if (id > 0 && UserInRuleTenant(id, tenantId)) ids.Add(id);
            }
        }
        void AddRoles()
        {
            var roleIds = ReadIntArray(to["roles"]).Where(x => x > 0).Distinct().ToArray();
            if (roleIds.Length == 0) return;
            var exp = User._.RoleID.In(roleIds);
            foreach (var rid in roleIds)
            {
                exp |= User._.RoleIds.Contains("," + rid + ",");
            }
            exp &= User._.Enable == true;
            foreach (var u in User.FindAll(exp, null, null, 0, 500))
            {
                if (u.ID > 0 && UserInRuleTenant(u.ID, tenantId)) ids.Add(u.ID);
            }
        }
        void AddDepts()
        {
            var deptIds = ReadIntArray(to["departments"]).Where(x => x > 0).Distinct().ToArray();
            if (deptIds.Length == 0) return;
            var exp = User._.DepartmentID.In(deptIds) & User._.Enable == true;
            foreach (var u in User.FindAll(exp, null, null, 0, 500))
            {
                if (u.ID > 0 && UserInRuleTenant(u.ID, tenantId)) ids.Add(u.ID);
            }
        }

        if (kind is "user" or "users") AddUsers();
        else if (kind is "role" or "roles") AddRoles();
        else if (kind is "department" or "departments" or "dept") AddDepts();
        else
        {
            // 无 kind：兼容旧数据，三者并集
            AddUsers();
            AddRoles();
            AddDepts();
        }

        // 兼容旧版 to.mode
        var mode = to["mode"]?.ToString();
        if (mode.EqualIgnoreCase("userId"))
        {
            var userId = to["userId"]?.GetValue<Int32>() ?? 0;
            if (userId > 0) ids.Add(userId);
        }
        else if (mode.EqualIgnoreCase("field"))
        {
            var field = to["field"]?.ToString();
            if (entity != null && !field.IsNullOrEmpty())
            {
                var userId = entity[field].ToInt();
                if (userId > 0) ids.Add(userId);
            }
        }

        return ids;
    }

    /// <summary>开启租户时，角色/部门展开与显式用户均须属于规则租户</summary>
    /// <param name="userId">用户编号</param>
    /// <param name="tenantId">租户编号</param>
    /// <returns>是否属于该租户</returns>
    static Boolean UserInRuleTenant(Int32 userId, Int32 tenantId)
    {
        if (!CubeSetting.Current.EnableTenant || tenantId <= 0) return true;
        return TenantUser.FindByTenantIdAndUserId(tenantId, userId) != null;
    }

    /// <summary>读取 JSON 整数数组，兼容逗号分隔字符串</summary>
    /// <param name="node">JSON 节点</param>
    /// <returns>整数序列</returns>
    static IEnumerable<Int32> ReadIntArray(JsonNode node)
    {
        var list = new List<Int32>();
        if (node is JsonArray arr)
        {
            foreach (var item in arr)
            {
                if (item == null) continue;
                try { list.Add(item.GetValue<Int32>()); }
                catch
                {
                    if (Int32.TryParse(item.ToString(), out var id)) list.Add(id);
                }
            }
            return list;
        }
        if (node != null)
        {
            var s = node.ToString();
            if (!s.IsNullOrEmpty())
            {
                foreach (var part in s.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (Int32.TryParse(part.Trim().Trim('"'), out var id)) list.Add(id);
                }
            }
        }
        return list;
    }
}
