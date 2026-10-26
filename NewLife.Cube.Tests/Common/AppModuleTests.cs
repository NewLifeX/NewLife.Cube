using System;
using NewLife.Cube.Entity;
using NewLife.Cube.Tests.Membership;
using Xunit;

namespace NewLife.Cube.Tests.Common;

/// <summary>
/// BASE-7 应用插件管理测试。覆盖插件登记、按编号/名称读取、启停更新与删除，以及类名长度约束。
/// </summary>
[Collection("TenantAuth")]
public class AppModuleTests
{
    private readonly TenantAuthFixture _fx;

    /// <summary>实例化</summary>
    /// <param name="fixture">多租户认证夹具。复用其 SQLite 库（应用插件表）</param>
    public AppModuleTests(TenantAuthFixture fixture) => _fx = fixture;

    private static String Unique(String prefix) => prefix + Guid.NewGuid().ToString("N")[..8];

    [Fact(DisplayName = "应用插件_登记后可按编号读取并可启停更新")]
    public void Insert_FindAndToggle()
    {
        var name = Unique("plugin");
        var item = new AppModule
        {
            Name = name,
            DisplayName = "测试插件",
            Type = "Module",
            ClassName = "NlMod.M",
            Enable = true,
            Remark = "单元测试",
        };
        item.Insert();
        Assert.True(item.Id > 0);

        var found = AppModule.FindById(item.Id);
        Assert.NotNull(found);
        Assert.Equal(name, found.Name);
        Assert.True(found.Enable);

        found.Enable = false;
        found.Remark = "已停用";
        found.Update();
        AppModule.Meta.Cache.Clear("应用插件测试", true);

        var again = AppModule.FindById(item.Id);
        Assert.False(again.Enable);
        Assert.Equal("已停用", again.Remark);

        Assert.True(again.Delete() > 0);
        Assert.Null(AppModule.FindById(item.Id));
        Assert.Null(AppModule.FindById(0));
    }

    [Fact(DisplayName = "应用插件_类名超长被拒绝")]
    public void Insert_ClassNameTooLong_Throws()
    {
        var item = new AppModule
        {
            Name = Unique("plugin"),
            DisplayName = "超长类名",
            Type = "Module",
            ClassName = new String('A', 60),
            Enable = true,
        };

        Assert.ThrowsAny<Exception>(() => item.Insert());
    }
}
