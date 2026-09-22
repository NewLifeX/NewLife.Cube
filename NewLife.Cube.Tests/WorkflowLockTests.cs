using System;
using System.ComponentModel;
using System.Linq;
using NewLife.Cube.Entity;
using NewLife.Cube.Workflow;
using NewLife.Cube.Workflow.Entity;
using XCode;
using XCode.DataAccessLayer;
using Xunit;
using WorkflowTask = NewLife.Cube.Workflow.Entity.WorkflowTask;

namespace NewLife.Cube.Tests;

/// <summary>OSC-26090347f1 写锁：full 拒绝普通 Update/Delete；流程通道 Scope 放行节点字段</summary>
[Collection("Osc47f1Menu")]
public class WorkflowLockTests
{
    public WorkflowLockTests()
    {
        DAL.AddConnStr("Cube", "Data Source=Osc47f1LockCube;Mode=Memory;Cache=Shared", null, "SQLite");
        DAL.AddConnStr("Log", "Data Source=Osc47f1LockLog;Mode=Memory;Cache=Shared", null, "SQLite");
        DAL.AddConnStr("Workflow", "Data Source=Osc47f1LockWf;Mode=Memory;Cache=Shared", null, "SQLite");
        WorkflowTestDb.EnsureInstanceSummaryColumn();

        WorkflowTask.FindAll().Delete();
        WorkflowSubject.FindAll().Delete();
        WorkflowInstance.FindAll().Delete();
        WorkflowDefinition.FindAll().Delete();
        WfLockRecord.FindAll().Delete();

        EntityPageRegistry.Register(typeof(WfLockRecord), "Cube/WfLockRecord", "Id");

        // 挂全局写锁拦截器（幂等）。跳过名单保护系统表；其它表在无在途主体时直接放行
        WorkflowHost.Register(false);
    }

    static String Graph(String mode, String users) =>
        "{\"version\":1," +
        "\"nodes\":[" +
        "{\"id\":\"start\",\"type\":\"oa.start\",\"data\":{}}," +
        "{\"id\":\"n1\",\"type\":\"oa.approve\",\"data\":{\"name\":\"审批\",\"mode\":\"" + mode + "\",\"to\":{\"kind\":\"users\",\"users\":[" + users + "]},\"fields\":{\"visible\":[\"*\"],\"writable\":[\"Name\"]},\"timeoutHours\":24,\"timeoutAction\":\"pass\"}}," +
        "{\"id\":\"end\",\"type\":\"oa.end\",\"data\":{}}" +
        "]," +
        "\"edges\":[{\"source\":\"start\",\"target\":\"n1\"},{\"source\":\"n1\",\"target\":\"end\"}]}";

    static WorkflowInstance StartRunning()
    {
        var rec = new WfLockRecord { Name = "原始" };
        rec.Insert();

        var def = new WorkflowDefinition
        {
            TenantId = 0,
            TypePath = "Cube/WfLockRecord",
            Name = "lock" + Guid.NewGuid().ToString("N")[..6],
            Enable = true,
            Published = true,
            Version = 1,
            LockPolicy = WorkflowStatuses.LockFull,
            StartFilter = "{}",
            GraphJson = Graph("or", "101"),
        };
        def.PublishedGraphJson = def.GraphJson;
        def.Insert();

        return WorkflowEngine.Start(def, [$"{rec.Id}"], 9, "发起人", "");
    }

    [Fact(DisplayName = "full 锁：审批中普通 Update 抛 403；流程通道改 Name 放行；结束后解锁")]
    public void FullLock_Blocks_ExceptScope()
    {
        var instance = StartRunning();
        var rec = WfLockRecord.FindAll().FirstOrDefault(e => e.Name == "原始");
        Assert.NotNull(rec);

        // 普通改非 Name 字段 → 拦截
        var rec2 = WfLockRecord.FindByKey(rec.Id);
        rec2.Name = "改1";
        var ex = Assert.ThrowsAny<Exception>(() => rec2.Update());
        Assert.Contains("审批中", ex.GetTrue().Message);

        // 流程通道：仅允许节点可写字段 Name → 放行
        var rec3 = WfLockRecord.FindByKey(rec.Id);
        using (WorkflowWriteScope.Enter(instance.Id, "n1", ["Name"]))
        {
            rec3.Name = "改2";
            rec3.Update();
        }
        Assert.Equal("改2", WfLockRecord.FindByKey(rec.Id).Name);

        // 通过后解锁
        var task = WorkflowTask.FindAll(WorkflowTask._.InstanceId == instance.Id).First();
        WorkflowEngine.Approve(task.Id, 101, "ok");
        var rec4 = WfLockRecord.FindByKey(rec.Id);
        rec4.Name = "终态";
        rec4.Update();
        Assert.Equal("终态", WfLockRecord.FindByKey(rec.Id).Name);
    }
}

/// <summary>写锁测试实体（属性建表，避免污染系统表）</summary>
[Serializable]
[DataObject]
[Description("写锁测试实体")]
[BindTable("WfLockRecord", Description = "写锁测试实体", ConnName = "Cube", DbType = DatabaseType.None)]
public partial class WfLockRecord : Entity<WfLockRecord>
{
    #region 属性
    private Int64 _Id;
    /// <summary>编号</summary>
    [DisplayName("编号")]
    [DataObjectField(true, true, false, 0)]
    [BindColumn("Id", "编号", "")]
    public Int64 Id { get => _Id; set { if (OnPropertyChanging("Id", value)) { _Id = value; OnPropertyChanged("Id"); } } }

    private String? _Name;
    /// <summary>名称</summary>
    [DisplayName("名称")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("Name", "名称", "", Master = true)]
    public String? Name { get => _Name; set { if (OnPropertyChanging("Name", value)) { _Name = value; OnPropertyChanged("Name"); } } }
    #endregion

    #region 获取/设置 字段值
    /// <summary>索引器</summary>
    /// <param name="name">字段名</param>
    /// <returns>值</returns>
    public override Object? this[String name]
    {
        get => name switch
        {
            "Id" => _Id,
            "Name" => _Name,
            _ => base[name]
        };
        set
        {
            switch (name)
            {
                case "Id": _Id = value.ToLong(); break;
                case "Name": _Name = Convert.ToString(value); break;
                default: base[name] = value; break;
            }
        }
    }
    #endregion
}
