using System.Text.Json.Nodes;
using NewLife.Cube.Entity;
using XCode.Membership;

namespace NewLife.Cube.Widgets;

/// <summary>工作台整份解析：用户 HomeJson → 主角色 Parameter → 系统种子</summary>
public static class WorkbenchResolver
{
    /// <summary>解析结果</summary>
    public sealed class Result
    {
        /// <summary>user / role / system</summary>
        public String Source { get; init; }

        /// <summary>主角色 Id</summary>
        public Int32 RoleId { get; init; }

        /// <summary>已归一化 JSON</summary>
        public String ConfigJson { get; init; }
    }

    /// <summary>按当前用户解析工作台配置。解析失败当未配置，不抛。</summary>
    public static Result Resolve(IUser user)
    {
        if (user == null) throw new ArgumentNullException(nameof(user));
        var roleId = user.RoleID;

        var profile = UserProfile.FindByUserId(user.ID);
        if (TryUse(profile?.HomeJson, user, out var userJson))
            return new Result { Source = "user", RoleId = roleId, ConfigJson = userJson };

        var roleRaw = WorkbenchRoleStore.Get(roleId);
        if (TryUse(roleRaw, user, out var roleJson))
            return new Result { Source = "role", RoleId = roleId, ConfigJson = roleJson };

        var seed = IsSystem(user) ? WorkbenchSeeds.Admin : WorkbenchSeeds.Member;
        if (!DashboardJson.TryNormalize(seed, user, false, DashboardJson.SurfaceWorkbench, out var sysJson, out _) || sysJson.IsNullOrEmpty())
            sysJson = seed;
        return new Result { Source = "system", RoleId = roleId, ConfigJson = sysJson };
    }

    /// <summary>JSON 是否构成有效个人/角色域（含空 widgets 数组）</summary>
    public static Boolean IsConfigured(String json)
    {
        if (json.IsNullOrWhiteSpace()) return false;
        try
        {
            var o = JsonNode.Parse(json) as JsonObject;
            if (o == null) return false;
            if (o["version"]?.GetValue<Int32>() is not 1) return false;
            return o["widgets"] is JsonArray;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>当前用户是否系统角色（Roles 或主角色 IsSystem）</summary>
    public static Boolean IsSystem(IUser user)
    {
        if (user == null) return false;
        if (user.Roles != null && user.Roles.Any(r => r.IsSystem)) return true;
        return Role.FindByID(user.RoleID)?.IsSystem == true;
    }

    static Boolean TryUse(String json, IUser user, out String normalized)
    {
        normalized = null;
        if (!IsConfigured(json)) return false;
        if (!DashboardJson.TryNormalize(json, user, false, DashboardJson.SurfaceWorkbench, out normalized, out _))
            return false;
        return !normalized.IsNullOrEmpty();
    }
}
