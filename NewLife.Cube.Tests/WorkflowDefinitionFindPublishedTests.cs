using System;
using NewLife.Cube.Workflow;
using NewLife.Cube.Workflow.Entity;
using XCode;
using XCode.DataAccessLayer;
using Xunit;

namespace NewLife.Cube.Tests;

/// <summary>OSC-2609307879 FindPublished 小表走实体缓存，停用或取消发布后不再返回。</summary>
public class WorkflowDefinitionFindPublishedTests
{
    const String TypePath = "Cube/Osc7879Pub";

    public WorkflowDefinitionFindPublishedTests()
    {
        DAL.AddConnStr("Cube", "Data Source=Osc7879PubCube;Mode=Memory;Cache=Shared", null, "SQLite");
        DAL.AddConnStr("Log", "Data Source=Osc7879PubLog;Mode=Memory;Cache=Shared", null, "SQLite");
        DAL.AddConnStr("Workflow", "Data Source=Osc7879PubWf;Mode=Memory;Cache=Shared", null, "SQLite");
        WorkflowTestDb.EnsureWorkflowTables();
        WorkflowDefinition.FindAll().Delete();
    }

    [Fact(DisplayName = "仅已发布且启用且路径租户匹配才返回；取消发布后消失")]
    public void FindPublished_Filters_And_Drops_Unpublished()
    {
        Assert.Empty(WorkflowDefinition.FindPublished(0, ""));
        Assert.Empty(WorkflowDefinition.FindPublished(0, TypePath));

        var hit = NewDef("命中", published: true, enable: true, tenantId: 0, typePath: TypePath);
        NewDef("未发布", published: false, enable: true, tenantId: 0, typePath: TypePath);
        NewDef("未启用", published: true, enable: false, tenantId: 0, typePath: TypePath);
        NewDef("其它路径", published: true, enable: true, tenantId: 0, typePath: "Cube/Other");
        NewDef("其它租户", published: true, enable: true, tenantId: 7, typePath: TypePath);

        var list = WorkflowDefinition.FindPublished(0, TypePath);
        Assert.Single(list);
        Assert.Equal(hit.Id, list[0].Id);

        hit.Published = false;
        hit.Update();
        Assert.Empty(WorkflowDefinition.FindPublished(0, TypePath));
    }

    static WorkflowDefinition NewDef(String name, Boolean published, Boolean enable, Int32 tenantId, String typePath)
    {
        var def = new WorkflowDefinition
        {
            TenantId = tenantId,
            TypePath = typePath,
            Name = name,
            Enable = enable,
            Published = published,
            Version = 1,
            LockPolicy = WorkflowStatuses.LockFull,
            StartFilter = "{}",
            GraphJson = "{}",
        };
        def.PublishedGraphJson = def.GraphJson;
        def.Insert();
        return def;
    }
}
