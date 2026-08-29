using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;

namespace E2EMvcTest.Fixtures;

/// <summary>SQLite 数据库查询辅助类，用于测试后验证数据库状态</summary>
public static class DatabaseHelper
{
    #region 连接字符串

    /// <summary>Membership 数据库连接字符串</summary>
    private static String MembershipConnStr =>
        $"Data Source={AppFixture.DataDir}\\Membership.db;Mode=ReadOnly";

    /// <summary>Log 数据库连接字符串</summary>
    private static String LogConnStr =>
        $"Data Source={AppFixture.DataDir}\\Log.db;Mode=ReadOnly";

    #endregion

    #region User 表查询

    /// <summary>统计 User 表中用户名精确匹配的行数</summary>
    /// <param name="username">用户名</param>
    /// <returns>匹配行数</returns>
    public static Int32 CountUsersByName(String username)
    {
        const String sql = "SELECT COUNT(*) FROM User WHERE Name = @name";
        return ExecuteScalar<Int32>(MembershipConnStr, sql, ("@name", username));
    }

    /// <summary>获取 User 表总行数</summary>
    /// <returns>用户总数</returns>
    public static Int32 CountAllUsers()
    {
        const String sql = "SELECT COUNT(*) FROM User";
        return ExecuteScalar<Int32>(MembershipConnStr, sql);
    }

    /// <summary>获取指定用户名的指定字段值</summary>
    /// <param name="username">用户名</param>
    /// <param name="field">字段名（须为 User 表已知字段）</param>
    /// <returns>字段值，不存在时返回 null</returns>
    public static String? GetUserField(String username, String field)
    {
        // 字段名不能参数化，使用白名单校验防止注入
        var allowedFields = new HashSet<String>(StringComparer.OrdinalIgnoreCase)
        {
            "ID", "Name", "DisplayName", "Mail", "Mobile", "Code", "Password",
            "Logins", "LastLogin", "CreateTime", "UpdateTime", "Enable",
        };

        if (!allowedFields.Contains(field))
            throw new ArgumentException($"不允许查询字段: {field}", nameof(field));

        var sql = $"SELECT {field} FROM User WHERE Name = @name LIMIT 1";
        return ExecuteScalar<String?>(MembershipConnStr, sql, ("@name", username));
    }

    /// <summary>获取指定用户的登录次数</summary>
    /// <param name="username">用户名</param>
    /// <returns>Logins 字段值</returns>
    public static Int32 GetUserLogins(String username)
    {
        const String sql = "SELECT IFNULL(Logins, 0) FROM User WHERE Name = @name LIMIT 1";
        return ExecuteScalar<Int32>(MembershipConnStr, sql, ("@name", username));
    }

    /// <summary>获取指定用户最后登录时间字符串</summary>
    /// <param name="username">用户名</param>
    /// <returns>LastLogin 字段值（ISO 字符串），用户不存在时返回 null</returns>
    public static String? GetUserLastLogin(String username)
    {
        const String sql = "SELECT LastLogin FROM User WHERE Name = @name LIMIT 1";
        return ExecuteScalar<String?>(MembershipConnStr, sql, ("@name", username));
    }

    #endregion

    #region UserConnect 表查询

    /// <summary>统计指定用户和提供商的 UserConnect 绑定记录数</summary>
    /// <param name="userId">用户 ID（0 表示不限）</param>
    /// <param name="provider">OAuth 提供商名称，如"NewLife"</param>
    /// <returns>匹配行数</returns>
    public static Int32 CountUserConnect(Int32 userId, String provider)
    {
        if (userId > 0)
        {
            const String sql = "SELECT COUNT(*) FROM UserConnect WHERE UserID = @uid AND Provider = @p";
            return ExecuteScalar<Int32>(MembershipConnStr, sql, ("@uid", userId), ("@p", provider));
        }
        else
        {
            const String sql = "SELECT COUNT(*) FROM UserConnect WHERE Provider = @p";
            return ExecuteScalar<Int32>(MembershipConnStr, sql, ("@p", provider));
        }
    }

    #endregion

    #region OAuthLog 表查询

    /// <summary>统计 OAuthLog 表中指定提供商在最近 N 分钟内的日志行数</summary>
    /// <param name="provider">提供商名称</param>
    /// <param name="withinMinutes">最近几分钟内，0 表示不限时间</param>
    /// <returns>匹配行数</returns>
    public static Int32 CountOAuthLog(String provider, Int32 withinMinutes = 10)
    {
        if (withinMinutes > 0)
        {
            var since = DateTime.UtcNow.AddMinutes(-withinMinutes).ToString("yyyy-MM-dd HH:mm:ss");
            const String sql = "SELECT COUNT(*) FROM OAuthLog WHERE Provider = @p AND CreateTime >= @since";
            return ExecuteScalar<Int32>(MembershipConnStr, sql, ("@p", provider), ("@since", since));
        }
        else
        {
            const String sql = "SELECT COUNT(*) FROM OAuthLog WHERE Provider = @p";
            return ExecuteScalar<Int32>(MembershipConnStr, sql, ("@p", provider));
        }
    }

    #endregion

    #region Tenant / TenantUser 表查询

    /// <summary>按名称获取用户 Id（User 表主键为 ID）</summary>
    /// <param name="username">用户名</param>
    /// <returns>用户 Id，不存在返回 0</returns>
    public static Int32 GetUserIdByName(String username)
    {
        const String sql = "SELECT ID FROM User WHERE Name = @name LIMIT 1";
        return ExecuteScalar<Int32>(MembershipConnStr, sql, ("@name", username));
    }

    /// <summary>按名称获取租户 Id</summary>
    /// <param name="tenantName">租户名称</param>
    /// <returns>租户 Id，不存在返回 0</returns>
    public static Int32 GetTenantIdByName(String tenantName)
    {
        const String sql = "SELECT Id FROM Tenant WHERE Name = @name LIMIT 1";
        return ExecuteScalar<Int32>(MembershipConnStr, sql, ("@name", tenantName));
    }

    /// <summary>统计指定租户与用户的绑定关系行数</summary>
    /// <param name="tenantId">租户 Id</param>
    /// <param name="userId">用户 Id</param>
    /// <returns>绑定行数（0 表示未绑定）</returns>
    public static Int32 CountTenantUser(Int32 tenantId, Int32 userId)
    {
        const String sql = "SELECT COUNT(*) FROM TenantUser WHERE TenantId = @tid AND UserId = @uid";
        return ExecuteScalar<Int32>(MembershipConnStr, sql, ("@tid", tenantId), ("@uid", userId));
    }

    /// <summary>统计指定租户的成员总数</summary>
    /// <param name="tenantId">租户 Id</param>
    /// <returns>成员总数</returns>
    public static Int32 CountTenantUsers(Int32 tenantId)
    {
        const String sql = "SELECT COUNT(*) FROM TenantUser WHERE TenantId = @tid";
        return ExecuteScalar<Int32>(MembershipConnStr, sql, ("@tid", tenantId));
    }

    #endregion

    #region 种子写入（跨 Area 链接测试）

    /// <summary>Membership 数据库写连接字符串（Tenant 等表）</summary>
    private static String MembershipWriteConnStr =>
        $"Data Source={AppFixture.DataDir}\\Membership.db";

    /// <summary>Cube 数据库写连接字符串（OAuthConfig 等表）</summary>
    private static String CubeWriteConnStr =>
        $"Data Source={AppFixture.DataDir}\\Cube.db";

    /// <summary>确保租户存在，不存在则插入，返回租户 Id</summary>
    /// <param name="name">租户名称</param>
    /// <param name="code">租户唯一编码</param>
    /// <returns>租户 Id，失败返回 0</returns>
    public static Int32 EnsureTenant(String name, String code)
    {
        var id = GetTenantIdByName(name);
        if (id > 0) return id;

        const String sql = """
            INSERT INTO Tenant (Code, Name, Type, Enable, Level, ManagerId, MaxUsers, MaxStorage, CreateUserId, UpdateUserId)
            VALUES (@code, @name, 0, 1, 1, 0, 0, 0, 0, 0)
            RETURNING Id;
            """;
        return ExecuteScalarWrite<Int32>(MembershipWriteConnStr, sql, ("@code", code), ("@name", name));
    }

    /// <summary>确保绑定指定租户的 OAuthConfig 记录存在，返回记录 Id</summary>
    /// <param name="name">OAuth 提供者名称（唯一）</param>
    /// <param name="tenantId">关联租户 Id</param>
    /// <returns>OAuthConfig 记录 Id，失败返回 0</returns>
    public static Int32 EnsureOAuthConfigWithTenant(String name, Int32 tenantId)
    {
        const String find = "SELECT ID FROM OAuthConfig WHERE Name = @name LIMIT 1";
        var id = ExecuteScalar<Int32>(CubeWriteConnStr, find, ("@name", name));
        if (id > 0) return id;

        // Sort 置大保证列表排在最前；其余非空列均提供默认值
        const String sql = """
            INSERT INTO OAuthConfig (TenantId, Name, GrantType, Enable, Debug, Visible, AutoRegister, Sort, FetchAvatar, IsDeleted, CreateUserID, UpdateUserID)
            VALUES (@tenantId, @name, 0, 1, 0, 1, 0, 9999, 0, 0, 0, 0)
            RETURNING ID;
            """;
        return ExecuteScalarWrite<Int32>(CubeWriteConnStr, sql, ("@tenantId", tenantId), ("@name", name));
    }

    /// <summary>按名称查询 OAuthConfig 记录的租户 Id</summary>
    /// <param name="name">OAuth 提供者名称</param>
    /// <returns>租户 Id，不存在返回 0</returns>
    public static Int32 GetOAuthConfigTenantId(String name)
    {
        const String sql = "SELECT TenantId FROM OAuthConfig WHERE Name = @name LIMIT 1";
        return ExecuteScalar<Int32>(CubeWriteConnStr, sql, ("@name", name));
    }

    #endregion

    #region 私有执行辅助

    /// <summary>执行写 SQL 并返回首行首列。设置 busy_timeout 避免与应用进程并发写锁冲突</summary>
    /// <param name="connStr">写连接字符串</param>
    /// <param name="sql">写 SQL</param>
    /// <param name="parameters">参数</param>
    /// <returns>首行首列值</returns>
    private static T ExecuteScalarWrite<T>(String connStr, String sql, params (String Name, Object? Value)[] parameters)
    {
        using var conn = new SqliteConnection(connStr);
        conn.Open();

        // 单独设置 busy_timeout，避免与应用进程并发写锁时立即失败
        using (var pragma = conn.CreateCommand())
        {
            pragma.CommandText = "PRAGMA busy_timeout = 10000;";
            pragma.ExecuteNonQuery();
        }

        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;

        foreach (var (name, value) in parameters)
            cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);

        var result = cmd.ExecuteScalar();
        if (result == null || result == DBNull.Value)
            return default!;

        return (T)Convert.ChangeType(result, Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T));
    }

    private static T ExecuteScalar<T>(String connStr, String sql, params (String Name, Object? Value)[] parameters)
    {
        using var conn = new SqliteConnection(connStr);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;

        foreach (var (name, value) in parameters)
            cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);

        var result = cmd.ExecuteScalar();
        if (result == null || result == DBNull.Value)
        {
            if (typeof(T).IsValueType)
                return default!;
            return default!;
        }

        return (T)Convert.ChangeType(result, Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T));
    }

    #endregion
}
