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
    /// </remarks>
    /// <param name="entity">角色实体</param>
    /// <param name="permission">权限字符串。空表示清空全部权限</param>
    public static void Apply(Role entity, String permission)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));

        // 收集现有权限键，用于后续清理已移除的项
        var oldKeys = entity.Permissions.Keys.ToList();

        // 解析权限字符串 "MenuID#Flags,MenuID#Flags" 并通过 entity.Set() 设置
        if (!String.IsNullOrEmpty(permission))
        {
            var newKeys = new List<Int32>();
            foreach (var part in permission.Split(','))
            {
                var kv = part.Split('#');
                if (kv.Length == 2 &&
                    Int32.TryParse(kv[0], out var menuId) &&
                    Int32.TryParse(kv[1], out var flag) &&
                    flag > 0)
                {
                    entity.Set(menuId, (PermissionFlags)flag);
                    newKeys.Add(menuId);
                }
            }

            // 移除不在新权限中的旧项
            foreach (var key in oldKeys)
            {
                if (!newKeys.Contains(key))
                    entity.Permissions.Remove(key);
            }
        }
        else
        {
            // 权限字符串为空，清空所有权限
            foreach (var key in oldKeys)
            {
                entity.Permissions.Remove(key);
            }
        }
    }
}
