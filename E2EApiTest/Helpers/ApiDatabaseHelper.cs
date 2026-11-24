using System;
using System.Collections.Generic;
using System.Threading;
using E2EApiTest.Fixtures;
using Microsoft.Data.Sqlite;

namespace E2EApiTest.Helpers;

/// <summary>SQLite 数据库查询辅助类，用于测试后验证数据落库与清理数据</summary>
public static class ApiDatabaseHelper
{
    #region 连接

    private static String ConnStr(String db, Boolean readOnly = true) =>
        $"Data Source={System.IO.Path.Combine(ApiAppFixture.DataDir, db)};Mode={(readOnly ? "ReadOnly" : "ReadWrite")};Default Timeout=15";

    /// <summary>Membership 数据库（用户、角色、菜单）</summary>
    private static String MembershipConnStr => ConnStr("Membership.db");

    /// <summary>Cube 数据库（参数、附件、通知）</summary>
    private static String CubeConnStr => ConnStr("Cube.db");

    /// <summary>School 数据库（学生、班级）</summary>
    private static String SchoolConnStr => ConnStr("School.db");

    #endregion

    #region 写入辅助

    /// <summary>执行写入语句，返回受影响行数。SQLite 被应用占用时重试</summary>
    private static Int32 ExecuteWrite(String connStr, String sql, params (String Name, Object? Value)[] parameters)
    {
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                using var conn = new SqliteConnection(connStr);
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = sql;
                foreach (var item in parameters)
                    cmd.Parameters.AddWithValue(item.Name, item.Value ?? DBNull.Value);

                return cmd.ExecuteNonQuery();
            }
            catch (SqliteException) when (attempt < 5)
            {
                Thread.Sleep(200);
            }
        }
    }

    /// <summary>执行查询并返回首行首列</summary>
    private static T? ExecuteScalar<T>(String connStr, String sql, params (String Name, Object? Value)[] parameters)
    {
        using var conn = new SqliteConnection(connStr);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        foreach (var item in parameters)
            cmd.Parameters.AddWithValue(item.Name, item.Value ?? DBNull.Value);

        var value = cmd.ExecuteScalar();
        if (value == null || value == DBNull.Value) return default;

        return (T)Convert.ChangeType(value, typeof(T));
    }

    /// <summary>执行查询并返回首行多列</summary>
    private static Dictionary<String, Object?> ExecuteRow(String connStr, String sql, params (String Name, Object? Value)[] parameters)
    {
        var dic = new Dictionary<String, Object?>(StringComparer.OrdinalIgnoreCase);
        using var conn = new SqliteConnection(connStr);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        foreach (var item in parameters)
            cmd.Parameters.AddWithValue(item.Name, item.Value ?? DBNull.Value);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return dic;

        for (var i = 0; i < reader.FieldCount; i++)
            dic[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);

        return dic;
    }

    #endregion

    #region 学生（School.db）

    /// <summary>统计学生表中名称精确匹配的行数</summary>
    public static Int32 CountStudentsByName(String name) =>
        ExecuteScalar<Int32>(SchoolConnStr, "SELECT COUNT(*) FROM Student WHERE Name = @name", ("@name", name));

    /// <summary>读取学生指定字段（白名单）</summary>
    public static Object? GetStudentField(String name, String field)
    {
        var allowed = new HashSet<String>(StringComparer.OrdinalIgnoreCase)
        {
            "Id", "Name", "Sex", "Age", "Mobile", "Address", "Enable", "Avatar", "Weight", "Amount", "ClassId", "Remark",
        };
        if (!allowed.Contains(field)) throw new ArgumentException($"不允许查询字段: {field}", nameof(field));

        var row = ExecuteRow(SchoolConnStr, $"SELECT {field} FROM Student WHERE Name = @name LIMIT 1", ("@name", name));
        return row.TryGetValue(field, out var value) ? value : null;
    }

    /// <summary>按名称删除学生（测试清理）</summary>
    public static Int32 DeleteStudentsByName(String name) =>
        ExecuteWrite(ConnStr("School.db", false), "DELETE FROM Student WHERE Name = @name", ("@name", name));

    #endregion

    #region 用户与角色（Membership.db）

    /// <summary>统计用户名精确匹配的行数</summary>
    public static Int32 CountUsersByName(String name) =>
        ExecuteScalar<Int32>(MembershipConnStr, "SELECT COUNT(*) FROM User WHERE Name = @name", ("@name", name));

    /// <summary>按名称删除用户（测试清理）</summary>
    public static Int32 DeleteUserByName(String name) =>
        ExecuteWrite(ConnStr("Membership.db", false), "DELETE FROM User WHERE Name = @name", ("@name", name));

    /// <summary>按角色名获取角色编号</summary>
    public static Int32 GetRoleIdByName(String name) =>
        ExecuteScalar<Int32>(MembershipConnStr, "SELECT IFNULL(Id, 0) FROM Role WHERE Name = @name LIMIT 1", ("@name", name));

    /// <summary>按名称删除角色（测试清理）</summary>
    public static Int32 DeleteRoleByName(String name) =>
        ExecuteWrite(ConnStr("Membership.db", false), "DELETE FROM Role WHERE Name = @name", ("@name", name));

    #endregion

    #region 附件（Cube.db）

    /// <summary>查询附件记录的分类与上传人</summary>
    /// <param name="id">附件编号</param>
    public static Dictionary<String, Object?> GetAttachment(Int64 id) =>
        ExecuteRow(CubeConnStr, "SELECT Id, Category, FileName, ContentType, Size, CreateUserId FROM Attachment WHERE Id = @id", ("@id", id));

    #endregion

    #region 分享令牌（Membership.db）

    /// <summary>直接写入分享令牌，模拟“附件分享”生成的临时链接凭证</summary>
    /// <param name="token">令牌值</param>
    /// <param name="userId">所属用户</param>
    /// <param name="url">资源地址，形如 attachment:{附件编号}</param>
    /// <param name="expire">过期时间</param>
    /// <param name="enable">是否启用</param>
    public static Int32 InsertUserToken(String token, Int32 userId, String url, DateTime expire, Boolean enable = true) =>
        ExecuteWrite(ConnStr("Membership.db", false),
            "INSERT INTO UserToken(Token, UserId, Url, Expire, Enable, Times, CreateUserId, CreateTime, UpdateUserId, UpdateTime) VALUES(@token, @userId, @url, @expire, @enable, 0, @userId, @now, @userId, @now)",
            ("@token", token), ("@userId", userId), ("@url", url), ("@expire", expire), ("@enable", enable), ("@now", DateTime.Now));

    /// <summary>按令牌值删除分享令牌（测试清理）</summary>
    public static Int32 DeleteUserToken(String token) =>
        ExecuteWrite(ConnStr("Membership.db", false), "DELETE FROM UserToken WHERE Token = @token", ("@token", token));

    #endregion

    #region 凭据字段（Cube.db）

    /// <summary>读取 OAuth 配置的应用密钥（用于校验只写字段未被清空）</summary>
    /// <param name="id">配置编号</param>
    public static String? GetOAuthSecret(Int32 id) =>
        ExecuteScalar<String>(CubeConnStr, "SELECT Secret FROM OAuthConfig WHERE Id = @id", ("@id", id));

    /// <summary>读取短信配置的应用密钥</summary>
    /// <param name="id">配置编号</param>
    public static String? GetSmsAppSecret(Int32 id) =>
        ExecuteScalar<String>(CubeConnStr, "SELECT AppSecret FROM SmsConfig WHERE Id = @id", ("@id", id));

    #endregion
}
