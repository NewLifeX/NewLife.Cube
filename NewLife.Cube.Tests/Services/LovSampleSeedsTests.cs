using System;
using System.ComponentModel;
using System.IO;
using NewLife.Cube.Services;
using XCode.DataAccessLayer;
using XCode.Membership;
using Xunit;

namespace NewLife.Cube.Tests.Services;

/// <summary>值集样例种子测试（与 LovStore 夹具同库）</summary>
[Collection("LovStore")]
public class LovSampleSeedsTests : IDisposable
{
    public LovSampleSeedsTests()
    {
        Parameter.Meta.ConnName = LovStoreFixture.ConnName;
    }

    public void Dispose()
    {
        // 清理样例编码，避免污染同集合其它测试
        foreach (var code in new[]
                 {
                     "Enum.Sample.YesNo", "Enum.Sample.Sex", "Enum.Sample.Priority", "List.Sample.User",
                 })
            LovStore.DeleteDef(code);
    }

    [Fact(DisplayName = "样例种子：首次写入四条，二次调用不覆盖名称")]
    public void Ensure_InsertsOnce_Idempotent()
    {
        LovSampleSeeds.Ensure();

        var yesNo = LovStore.FindDef("Enum.Sample.YesNo");
        Assert.NotNull(yesNo);
        Assert.Equal("是否（样例）", yesNo!.Name);
        Assert.Equal(2, LovStore.FindEnumItems("Enum.Sample.YesNo").Count);

        var sex = LovStore.FindDef("Enum.Sample.Sex");
        Assert.NotNull(sex);
        Assert.Equal(3, LovStore.FindEnumItems("Enum.Sample.Sex").Count);

        var pri = LovStore.FindDef("Enum.Sample.Priority");
        Assert.NotNull(pri);

        var user = LovStore.FindDef("List.Sample.User");
        Assert.NotNull(user);
        Assert.Equal("LIST", user!.Type);
        Assert.NotNull(LovStore.FindListConfig("List.Sample.User"));
        Assert.NotEmpty(LovStore.FindTableColumns("List.Sample.User"));

        // 二次：改名后 Ensure 不得覆盖
        yesNo.Name = "已被管理员改名";
        LovStore.SaveDef(yesNo);
        LovSampleSeeds.Ensure();
        Assert.Equal("已被管理员改名", LovStore.FindDef("Enum.Sample.YesNo")!.Name);
    }
}
