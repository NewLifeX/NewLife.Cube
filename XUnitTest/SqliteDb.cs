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
    /// <remarks>双检锁 + 建表完成后才置位：防止并行用例在表建成前就 Delete/Insert 触发 no such table</remarks>
    public static void Ensure()
    {
        if (_inited) return;
        lock (_lock)
        {
            if (_inited) return;

            var connStr = $"Data Source={_dbFile};provider=sqlite;Migration=On";
            DAL.AddConnStr("Membership", connStr, null, "sqlite");
            DAL.AddConnStr("Log", connStr, null, "sqlite");

            // 主动按表建结构：部分实体（如 User）可能在本初始化之前已被访问过，
            // EntitySession 的“已检查”标记换库后不会复位，不再触发建表；直接 SetTables 绕过该缓存，避免 no such table
            var dal = DAL.Create("Membership");
            dal.SetTables(
                (IDataTable)Role.Meta.Table.DataTable.Clone(),
                (IDataTable)User.Meta.Table.DataTable.Clone(),
                (IDataTable)UserStat.Meta.Table.DataTable.Clone(),
                (IDataTable)UserToken.Meta.Table.DataTable.Clone(),
                (IDataTable)UserConnect.Meta.Table.DataTable.Clone(),
                (IDataTable)Parameter.Meta.Table.DataTable.Clone(),
                (IDataTable)TenantUser.Meta.Table.DataTable.Clone());

            var dal2 = DAL.Create("Log");
            dal2.SetTables(
                (IDataTable)NotificationRecord.Meta.Table.DataTable.Clone(),
                (IDataTable)UserOnline.Meta.Table.DataTable.Clone(),
                (IDataTable)VerifyCodeRecord.Meta.Table.DataTable.Clone());

            // 触发表结构检查与自动建表，避免首个用例直接 Delete/Insert 时表尚不存在
            var _ = Role.Meta.Count;
            var _2 = UserStat.Meta.Count;
            var _3 = NotificationRecord.Meta.Count;

            // 全部表结构就绪后才开放给并行用例
            _inited = true;
        }
    }

    private static readonly Object _lock = new();
    private static readonly String _dbFile = Path.Combine(Path.GetTempPath(), $"cubesqlite_{Guid.NewGuid():N}.db");
}
