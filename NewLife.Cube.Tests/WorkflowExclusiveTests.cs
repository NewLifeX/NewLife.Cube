using System;
using System.Linq;
using System.Threading.Tasks;
using NewLife.Cube.Entity;
using NewLife.Cube.Workflow;
using NewLife.Cube.Workflow.Entity;
using XCode;
using XCode.DataAccessLayer;
using Xunit;
using WorkflowTask = NewLife.Cube.Workflow.Entity.WorkflowTask;

namespace NewLife.Cube.Tests;

/// <summary>OSC-26090347f1 排他：同一业务记录同一时刻最多一条在途（design §3.3/§12）。</summary>
/// <remarks>
/// 并发窗口说明：Start 的 `FindRunning` 排他检查在实例事务之前；OSC-260922201a 起在实例事务内插入
/// WorkflowOccupancy，其 (TypePath,EntityKey) 唯一索引提供 DB 层原子约束，冲突转 409「已有在途审批」。
/// 本测试以 Barrier 并发双提，断言结束后在途仅一条。
/// </remarks>
public class WorkflowExclusiveTests
{
    public WorkflowExclusiveTests()
    {
        DAL.AddConnStr("Cube", "Data Source=Osc47f1ExCube;Mode=Memory;Cache=Shared", null, "SQLite");
        DAL.AddConnStr("Log", "Data Source=Osc47f1ExLog;Mode=Memory;Cache=Shared", null, "SQLite");
        DAL.AddConnStr("Workflow", "Data Source=Osc47f1ExWf;Mode=Memory;Cache=Shared", null, "SQLite");
        WorkflowTestDb.EnsureWorkflowTables();

        WorkflowTask.FindAll().Delete();
        WorkflowSubject.FindAll().Delete();
        WorkflowInstance.FindAll().Delete();
        WorkflowDefinition.FindAll().Delete();

        EntityPageRegistry.Register(typeof(NotificationRecord), "Cube/NotificationRecord", "Id");
    }

    static WorkflowDefinition NewDefinition()
    {
        var graph = "{\"version\":1," +
            "\"nodes\":[" +
            "{\"id\":\"start\",\"type\":\"oa.start\",\"data\":{}}," +
            "{\"id\":\"n1\",\"type\":\"oa.approve\",\"data\":{\"name\":\"审批\",\"mode\":\"or\",\"to\":{\"kind\":\"users\",\"users\":[101]},\"fields\":{\"visible\":[\"*\"],\"writable\":[]}}}," +
            "{\"id\":\"end\",\"type\":\"oa.end\",\"data\":{}}" +
            "]," +
            "\"edges\":[" +
            "{\"source\":\"start\",\"target\":\"n1\"},{\"source\":\"n1\",\"target\":\"end\"}" +
            "]}";
        var def = new WorkflowDefinition
        {
            TenantId = 0,
            TypePath = "Cube/NotificationRecord",
            Name = "排他" + Guid.NewGuid().ToString("N")[..8],
            Enable = true,
            Published = true,
            Version = 1,
            LockPolicy = WorkflowStatuses.LockFull,
            StartFilter = "{}",
            GraphJson = graph,
        };
        def.PublishedGraphJson = def.GraphJson;
        def.Insert();
        return def;
    }

    [Fact(DisplayName = "同一记录二次提交 → 409，全程仅一条在途")]
    public void SameRecord_SecondStart_409()
    {
        var def = NewDefinition();
        var rec = new NotificationRecord { Action = "Notify", Channel = "InApp", UserId = 1, Title = "r", Content = "c", Success = true };
        rec.Insert();
        var key = $"{rec.Id}";

        var instance = WorkflowEngine.Start(def, [key], 9, "发起人", "");
        Assert.Equal(WorkflowStatuses.Running, instance.Status);
        Assert.Single(WorkflowSubject.FindAll(WorkflowSubject._.TypePath == "Cube/NotificationRecord" & WorkflowSubject._.EntityKey == key));

        var ex = Assert.Throws<WorkflowException>(() => WorkflowEngine.Start(def, [key], 9, "发起人", "再来"));
        Assert.Equal(409, ex.Code);
        Assert.Single(WorkflowSubject.FindAll(WorkflowSubject._.TypePath == "Cube/NotificationRecord" & WorkflowSubject._.EntityKey == key));
    }

    [Fact(DisplayName = "并发双提（同记录）：成功至多一条；其余 409/事务回滚不落第二条在途")]
    public async Task Concurrent_DoubleStart_OnlyOneRunning()
    {
        var def = NewDefinition();
        var rec = new NotificationRecord { Action = "Notify", Channel = "InApp", UserId = 1, Title = "c", Content = "c", Success = true };
        rec.Insert();
        var key = $"{rec.Id}";

        // 并发两次提交：断言结束后在途仅一条（顺序竞争允许第二发 409 或落库被回滚）
        var barrier = new System.Threading.Barrier(2);
        var results = new System.Collections.Concurrent.ConcurrentBag<Boolean>();
        var tasks = new[]
        {
            Task.Run(() => RunStart(def, key, barrier, results)),
            Task.Run(() => RunStart(def, key, barrier, results)),
        };
        await Task.WhenAll(tasks);

        var running = WorkflowSubject.FindAll(WorkflowSubject._.TypePath == "Cube/NotificationRecord" & WorkflowSubject._.EntityKey == key)
            .Select(s => WorkflowInstance.FindById(s.InstanceId))
            .Where(i => i != null && i.Status == WorkflowStatuses.Running)
            .ToList();
        Assert.Single(running);

        static void RunStart(WorkflowDefinition d, String k, System.Threading.Barrier b, System.Collections.Concurrent.ConcurrentBag<Boolean> bag)
        {
            b.SignalAndWait();
            try
            {
                WorkflowEngine.Start(d, [k], 9, "发起人", "");
                bag.Add(true);
            }
            catch (WorkflowException ex)
            {
                // 409 排他即预期；其它 400（如并发下先落库后 FindByKey/约束）也视为被引擎拒绝
                if (ex.Code is not (409 or 400)) throw;
            }
            catch (Exception)
            {
                // SQLite 共享内存连接在并发写下的瞬时锁冲突也属被拒路径
            }
        }
    }
}
