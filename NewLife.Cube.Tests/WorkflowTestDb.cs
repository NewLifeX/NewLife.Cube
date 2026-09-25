using System;
using NewLife.Cube.Entity;
using NewLife.Cube.Workflow.Entity;
using XCode.DataAccessLayer;

namespace NewLife.Cube.Tests;

/// <summary>Workflow 测试库：Shared 内存库在进程内复用时，新列可能未反向工程补齐。</summary>
static class WorkflowTestDb
{
    /// <summary>幂等确保 Workflow 各表结构（含 WorkflowOccupancy 建表触发）</summary>
    /// <remarks>
    /// XCode 的自动建表只在查询/计数且未命中缓存时触发，Insert 路径不触发；
    /// WorkflowOccupancy 仅由 Start 事务 Insert 首用，且计数缓存会让 Meta.Count 不再触发建表，
    /// 因此这里显式 SetTables 幂等建表，避免 no such table。
    /// </remarks>
    public static void EnsureWorkflowTables()
    {
        EnsureInstanceColumns();
        try
        {
            var dal = DAL.Create("Workflow");
            dal.SetTables(
                (IDataTable)WorkflowInstance.Meta.Table.DataTable.Clone(),
                (IDataTable)WorkflowDefinition.Meta.Table.DataTable.Clone(),
                (IDataTable)WorkflowSubject.Meta.Table.DataTable.Clone(),
                (IDataTable)WorkflowTask.Meta.Table.DataTable.Clone(),
                (IDataTable)WorkflowComment.Meta.Table.DataTable.Clone(),
                (IDataTable)WorkflowOccupancy.Meta.Table.DataTable.Clone());
        }
        catch
        {
            /* 建表失败留给后续用例暴露 */
        }

        // 引擎副作用的“外部表”：通知记录（Log）与写锁记录（Cube）。
        // 这两张表不在 Workflow 库，且 Insert 路径不触发 XCode 建表检查；
        // 干净输出目录（或 CI）下必须先显式建表，否则报 no such table（历史输出目录曾靠先前运行残留的库文件掩盖）。
        try
        {
            var dal = DAL.Create("Log");
            dal.SetTables((IDataTable)NotificationRecord.Meta.Table.DataTable.Clone());
        }
        catch
        {
            /* 连接未设置时忽略，留给用例暴露 */
        }

        try
        {
            var dal = DAL.Create("Cube");
            dal.SetTables((IDataTable)WfLockRecord.Meta.Table.DataTable.Clone());
        }
        catch
        {
            /* 连接未设置时忽略，留给用例暴露 */
        }
    }

    /// <summary>幂等补齐 WorkflowInstance 新列（表不存在或列已存在则忽略）</summary>
    public static void EnsureInstanceSummaryColumn()
    {
        EnsureInstanceColumns();
    }

    /// <summary>幂等补齐 Title / Summary</summary>
    public static void EnsureInstanceColumns()
    {
        TryAlter("ALTER TABLE WorkflowInstance ADD COLUMN Summary TEXT");
        TryAlter("ALTER TABLE WorkflowInstance ADD COLUMN Title nvarchar(200)");
    }

    static void TryAlter(String sql)
    {
        try
        {
            DAL.Create("Workflow").Execute(sql);
        }
        catch
        {
            /* ignore */
        }
    }
}
