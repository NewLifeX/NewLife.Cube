using System.Linq;
using XCode.Membership;

namespace NewLife.Cube;

/// <summary>角色权限助手。负责权限字符串与权限字典之间的转换，MVC/API 两版本共用</summary>
public static class RolePermissionHelper
{
    /// <summary>应用权限字符串到角色权限字典。以字符串为准重建权限，避免 JSON 请求下把所有权限当成未授权而清空</summary>
    /// <remarks>
    /// 权限字符串格式：MenuID#Flags,MenuID#Flags，例如 "52#1,53#4"。
    /// JSON/API 提交路径没有表单复选框，直接使用该字符串重建权限字典；
    /// 空字符串表示清空全部权限。
    /// 受限模式（restricted=true，租户管理员场景）：只能授予当前用户自己拥有的权限位，
    /// 且不能移除自己未拥有的权限（保留系统管理员授予的其它权限）。
    /// </remarks>
    /// <param name="entity">角色实体</param>
    /// <param name="permission">权限字符串。空表示清空全部权限</param>
    /// <param name="current">当前操作用户。受限模式下用于计算其拥有的权限位</param>
    /// <param name="restricted">是否受限模式。租户管理员操作时置 true，防止越界授权</param>
    public static void Apply(Role entity, String permission, IUser current = null, Boolean restricted = false)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));

        // 旧权限快照：受限模式保留当前用户未拥有的权限（系统管理员授予的其它权限）
        var old = new Dictionary<Int32, PermissionFlags>(entity.Permissions);

        // 解析权限字符串 "MenuID#Flags,MenuID#Flags"，受限模式只授予当前用户自己拥有的权限位
        var dic = new Dictionary<Int32, PermissionFlags>();
        if (!String.IsNullOrEmpty(permission))
        {
            foreach (var part in permission.Split(','))
            {
                var kv = part.Split('#');
                // 注意：All 在 Role.SavePermission 中以 (Int32) 序列化为 -1（PermissionFlags 为 UInt32），
                // 必须放行 -1，否则「全部」权限（-1）会被 flag > 0 误判为非法而丢弃
                if (kv.Length == 2 &&
                    Int32.TryParse(kv[0], out var menuId) &&
                    Int32.TryParse(kv[1], out var flag) &&
                    flag != 0)
                {
                    var flagVal = (PermissionFlags)flag;
                    // 防止越界授权：受限模式只授予当前用户自己拥有的权限位
                    if (restricted) flagVal &= OwnedFlags(current, XCode.Membership.Menu.FindByID(menuId));
                    if (flagVal > PermissionFlags.None) dic[menuId] = flagVal;
                }
            }
        }

        // 以字符串为准重建权限字典。先清空旧值，避免 Set 的 OR 语义叠加残留
        entity.Permissions.Clear();
        foreach (var item in dic)
            entity.Set(item.Key, item.Value);

        // 受限模式：回收自己拥有的旧权限，保留未拥有的权限（如系统管理员授予的其它权限）
        if (restricted)
        {
            foreach (var item in old)
            {
                if (dic.ContainsKey(item.Key)) continue;

                var keep = item.Value & ~OwnedFlags(current, XCode.Membership.Menu.FindByID(item.Key));
                if (keep > PermissionFlags.None) entity.Set(item.Key, keep);
            }
        }
    }

    /// <summary>计算指定用户在某资源上拥有的权限位集合，用于限制租户管理员的授权范围，防止越界授权</summary>
    /// <param name="user">当前操作用户</param>
    /// <param name="menu">资源菜单</param>
    /// <returns>用户拥有的权限位组合</returns>
    private static PermissionFlags OwnedFlags(IUser user, IMenu menu)
    {
        var owned = PermissionFlags.None;
        if (user == null || menu == null) return owned;

        foreach (var pf in menu.Permissions)
        {
            var flag = (PermissionFlags)pf.Key;
            if (user.Has(menu, flag)) owned |= flag;
        }

        return owned;
    }
}
