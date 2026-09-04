using System;
using System.IO;
using NewLife.Cube.Entity;
using XCode.DataAccessLayer;
using XCode.Membership;

namespace XUnitTest;

/// <summary>SqliteDb 集合共享 SQLite 数据库。让同集合内所有用例指向同一物理文件，避免多个类各自重定向同一逻辑连接（Membership/Log）导致 XCode 表结构缓存错乱（no such table）</summary>
public static class SqliteDb
{
    private static Boolean _inited;

    /// <summary>确保共享 SQLite 连接就绪并完成常用表结构初始化（进程内只执行一次）</summary>
    public static void Ensure()
    {
        if (_inited) return;
        _inited = true;

        var connStr = $"Data Source={_dbFile};provider=sqlite;Migration=On";
        DAL.AddConnStr("Membership", connStr, null, "sqlite");
        DAL.AddConnStr("Log", connStr, null, "sqlite");

        // 触发表结构检查与自动建表，避免首个用例直接 Delete/Insert 时表尚不存在
        var _ = Role.Meta.Count;
        var _2 = UserStat.Meta.Count;
        var _3 = NotificationRecord.Meta.Count;
    }

    private static readonly String _dbFile = Path.Combine(Path.GetTempPath(), $"cubesqlite_{Guid.NewGuid():N}.db");
}
