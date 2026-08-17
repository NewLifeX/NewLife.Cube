using System;
using System.Collections.Generic;
using System.Linq;
using NewLife.Data;
using XCode;
using XCode.Membership;

namespace NewLife.Cube;

/// <summary>租户用户搜索（OSC-260813397e）：先取 TenantUser 再查 User，避免 JOIN 列名歧义</summary>
public static class UserTenantSearch
{
    /// <summary>按租户关系列出用户</summary>
    public static IList<User> Search(
        Int32 tenantId,
        Int32[] roleIds,
        Int32[] departmentIds,
        Int32[] areaIds,
        Boolean? enable,
        DateTime start,
        DateTime end,
        String key,
        PageParameter page)
    {
        var links = TenantUser.FindAll(TenantUser._.TenantId == tenantId);
        var userIds = links.Select(e => e.UserId).Distinct().ToArray();
        if (userIds.Length == 0) return [];

        var exp = User._.ID.In(userIds);
        if (roleIds != null && roleIds.Length > 0)
        {
            var exp2 = new WhereExpression();
            exp2 |= User._.RoleID.In(roleIds);
            foreach (var rid in roleIds)
            {
                exp2 |= User._.RoleIds.Contains("," + rid + ",");
            }
            exp &= exp2;
        }
        if (departmentIds != null && departmentIds.Length > 0) exp &= User._.DepartmentID.In(departmentIds);
        if (areaIds != null && areaIds.Length > 0) exp &= User._.AreaId.In(areaIds);
        if (enable != null) exp &= User._.Enable == enable.Value;
        exp &= User._.LastLogin.Between(start, end);
        if (!key.IsNullOrEmpty())
            exp &= User._.Code.Contains(key) | User._.Name.Contains(key) | User._.DisplayName.Contains(key) | User._.Mobile.Contains(key) | User._.Mail.Contains(key);

        return User.FindAll(exp, page);
    }
}
