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

    #region 私有执行辅助

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
