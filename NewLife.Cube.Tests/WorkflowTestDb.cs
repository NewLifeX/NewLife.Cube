using System;
using XCode.DataAccessLayer;

namespace NewLife.Cube.Tests;

/// <summary>Workflow 测试库：Shared 内存库在进程内复用时，新列可能未反向工程补齐。</summary>
static class WorkflowTestDb
{
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
