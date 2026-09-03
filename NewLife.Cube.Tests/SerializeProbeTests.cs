using System;
using System.Text.Json;
using NewLife.Cube.Workflow;
using NewLife.Serialization;
using XCode;
using XCode.DataAccessLayer;
using Xunit;

namespace NewLife.Cube.Tests;

/// <summary>探针：验证 SetItem 额外键是否会被 SystemJson.Apply 序列化平铺输出（决定行级 __wf* 注入方式）</summary>
public class SerializeProbeTests
{
    [Fact(DisplayName = "探针 SetItem 平铺")]
    public void Probe_SetItem_Flat()
    {
        DAL.AddConnStr("Cube", "Data Source=Osc47f1Probe;Mode=Memory;Cache=Shared", null, "SQLite");
        var entity = new WfLockRecord { Name = "x" };
        entity.Insert();
        entity.SetItem("__wfStatus", "running");
        entity.SetItem("__wfInstanceId", 42L);

        var options = new JsonSerializerOptions();
        SystemJson.Apply(options, true);
        var json = JsonSerializer.Serialize(entity, options);
        Assert.True(json.Contains("__wfStatus"), json);
        Assert.True(json.Contains("running"), json);
    }
}
