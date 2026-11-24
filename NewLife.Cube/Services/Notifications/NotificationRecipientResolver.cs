using System;
using System.Collections.Generic;
using System.Linq;
using NewLife.Data;
using XCode;
using XCode.Membership;

namespace NewLife.Cube.Services.Notifications;

/// <summary>通知收件人解析器。负责把用户/角色/部门/全员解析为收件人，并做规模保护</summary>
public class NotificationRecipientResolver
{
    /// <summary>单次最大收件人数量</summary>
    public const Int32 MaxRecipients = 1000;

    /// <summary>解析收件人</summary>
    /// <param name="request">通知请求</param>
    /// <returns>收件人列表</returns>
    public IList<NotificationRecipient> Resolve(NotificationRequest request)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        if (request.ToAdmins && request.All)
            throw new InvalidOperationException("ToAdmins and All cannot both be true.");
        if (request.All && request.Scope == NotificationScope.Any)
            throw new InvalidOperationException("Broadcast notifications cannot share read state. Use scope=Each.");

        var tenantUsers = LoadTenantUsers(request.TenantId);
        var users = LoadUsers(request, tenantUsers);
        var tenantUserIds = tenantUsers == null ? null : new HashSet<Int32>(tenantUsers.Where(e => e.Enable).Select(e => e.UserId));
        var list = new List<NotificationRecipient>();
        var seen = new HashSet<Int32>();

        foreach (var user in users)
        {
            if (user == null || user.Id <= 0 || !user.Enable) continue;
            if (tenantUserIds != null && !tenantUserIds.Contains(user.Id)) continue;
            if (!seen.Add(user.Id)) continue;

            list.Add(new NotificationRecipient
            {
                UserId = user.Id,
                Name = user.DisplayName.IsNullOrEmpty() ? user.Name : user.DisplayName,
                Mail = user.Mail,
                Mobile = user.Mobile,
            });

            if (list.Count > MaxRecipients)
                throw new InvalidOperationException($"Recipients exceed the limit of {MaxRecipients}.");
        }

        return list;
    }

    private static IList<User> LoadUsers(NotificationRequest request, IList<TenantUser> tenantUsers)
    {
        if (request.ToAdmins) return LoadAdminUsers(request, tenantUsers);

        if (request.UserIds is { Length: > 0 })
        {
            var ids = request.UserIds.Where(e => e > 0).Distinct().ToArray();
            return LoadUsersByIds(ids);
        }

        if (request.RoleIds is { Length: > 0 } || request.DepartmentIds is { Length: > 0 })
        {
            // 多租户下角色/部门以租户关系为准，避免误用全局角色
            if (request.TenantId > 0)
            {
                var ids = tenantUsers?
                    .Where(e => e.Enable && MatchRole(e, request.RoleIds) && MatchDepartment(e, request.DepartmentIds))
                    .Select(e => e.UserId)
                    .Distinct()
                    .ToArray() ?? [];

                return LoadUsersByIds(ids);
            }

            var page = new PageParameter { PageSize = MaxRecipients + 1 };
            return User.Search(request.RoleIds, request.DepartmentIds, true, DateTime.MinValue, DateTime.MinValue, null, page);
        }

        if (request.All)
        {
            if (request.TenantId > 0)
            {
                var ids = tenantUsers?.Where(e => e.Enable).Select(e => e.UserId).Distinct().ToArray() ?? [];
                return LoadUsersByIds(ids);
            }

            var page = new PageParameter { PageSize = MaxRecipients + 1 };
            return User.Search(null, null, true, DateTime.MinValue, DateTime.MinValue, null, page);
        }

        return [];
    }

    private static IList<User> LoadAdminUsers(NotificationRequest request, IList<TenantUser> tenantUsers)
    {
        var adminRoleIds = Role.FindAllWithCache().Where(e => e.IsSystem).Select(e => e.Id).ToArray();
        if (adminRoleIds.Length == 0) return [];

        // 多租户下管理员名单以租户成员为准；没有租户成员时宁可不发也不越权
        if (request.TenantId > 0)
        {
            var ids = tenantUsers?
                .Where(e => e.Enable && MatchRole(e, adminRoleIds))
                .Select(e => e.UserId)
                .Distinct()
                .ToArray() ?? [];

            return LoadUsersByIds(ids);
        }

        var page = new PageParameter { PageSize = MaxRecipients + 1 };
        return User.Search(adminRoleIds, null, true, DateTime.MinValue, DateTime.MinValue, null, page);
    }

    private static IList<User> LoadUsersByIds(Int32[] ids)
    {
        if (ids.Length == 0) return [];
        if (ids.Length > MaxRecipients)
            throw new InvalidOperationException($"Recipients exceed the limit of {MaxRecipients}.");

        return User.FindAll(User._.Id.In(ids));
    }

    private static Boolean MatchRole(TenantUser tenant, Int32[] roleIds)
    {
        if (roleIds is not { Length: > 0 }) return true;
        if (roleIds.Contains(tenant.RoleId)) return true;

        var roles = tenant.RoleIds.SplitAsInt();

        return roles.Any(roleIds.Contains);
    }

    private static Boolean MatchDepartment(TenantUser tenant, Int32[] departmentIds)
        => departmentIds is not { Length: > 0 } || departmentIds.Contains(tenant.DepartmentId);

    private static IList<TenantUser> LoadTenantUsers(Int32 tenantId)
    {
        if (tenantId <= 0) return null;

        // 多租户上下文下必须收敛到租户成员；租户没有成员时返回空集合，宁可不发也不越权
        return TenantUser.FindAllByTenantId(tenantId);
    }
}
