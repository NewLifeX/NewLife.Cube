using NewLife.Cube;
using Xunit;

namespace NewLife.Cube.Tests.Common;

/// <summary>覆盖 <see cref="EntityPageRegistry"/> 的实体→页面注册与链接构造逻辑。</summary>
/// <remarks>
/// 对应 Issue #66：初始化时记录每个实体类型对应的 Controller/菜单 URL，供构建表间链接（跨 Area 外键跳转）使用。
/// 注册表为静态全局，各用例使用独立实体类型，避免相互污染。
/// </remarks>
public class EntityPageRegistryTests
{
    private class E1 { }
    private class E2 { }
    private class E3 { }
    private class E4 { }
    private class E5 { }
    private class E6 { }

    [Fact(DisplayName = "注册后可查询到页面信息")]
    public void Register_Get()
    {
        EntityPageRegistry.Register(typeof(E1), "/Admin/User", "ID");

        var info = EntityPageRegistry.Get(typeof(E1));

        Assert.NotNull(info);
        Assert.Equal("/Admin/User", info!.Url);
        Assert.Equal("ID", info.PrimaryKey);
    }

    [Fact(DisplayName = "同一实体重复注册，后者覆盖前者")]
    public void Register_Overwrite()
    {
        EntityPageRegistry.Register(typeof(E2), "/Admin/User", "ID");
        EntityPageRegistry.Register(typeof(E2), "/Cube/User", "Id");

        var info = EntityPageRegistry.Get(typeof(E2));

        Assert.NotNull(info);
        Assert.Equal("/Cube/User", info!.Url);
        Assert.Equal("Id", info.PrimaryKey);
    }

    [Fact(DisplayName = "主键为空时默认 ID")]
    public void Register_NullPrimaryKey_DefaultId()
    {
        EntityPageRegistry.Register(typeof(E3), "/Admin/Role", null);

        var info = EntityPageRegistry.Get(typeof(E3));

        Assert.Equal("ID", info!.PrimaryKey);
    }

    [Fact(DisplayName = "未注册的实体返回 null")]
    public void Get_NotRegistered_ReturnsNull()
    {
        Assert.Null(EntityPageRegistry.Get(typeof(E4)));
    }

    [Fact(DisplayName = "null 实体类型注册与查询均安全")]
    public void NullType_Safe()
    {
        EntityPageRegistry.Register(null, "/Admin/User", "ID");
        Assert.Null(EntityPageRegistry.Get(null));
    }

    [Fact(DisplayName = "GetLink 按主键拼接完整跳转链接")]
    public void GetLink_BuildsUrl()
    {
        EntityPageRegistry.Register(typeof(E4), "/Admin/Role", "ID");

        var link = EntityPageRegistry.GetLink(typeof(E4), 123);

        Assert.Equal("/Admin/Role?ID=123", link);
    }

    [Fact(DisplayName = "未注册时 GetLink 返回 null")]
    public void GetLink_NotRegistered_ReturnsNull()
    {
        Assert.Null(EntityPageRegistry.GetLink(typeof(E5), 123));
    }

    [Fact(DisplayName = "GetUrlTemplate 生成含占位符的 URL 模板")]
    public void GetUrlTemplate_BuildsTemplate()
    {
        EntityPageRegistry.Register(typeof(E5), "/Admin/Role", "ID");

        var info = EntityPageRegistry.Get(typeof(E5));

        Assert.Equal("/Admin/Role?ID={RoleId}", info!.GetUrlTemplate("RoleId"));
    }

    [Fact(DisplayName = "GetAll 返回已注册快照，含本次注册的实体")]
    public void GetAll_ContainsRegistered()
    {
        EntityPageRegistry.Register(typeof(E6), "/Admin/Tenant", "Id");

        var all = EntityPageRegistry.GetAll();

        Assert.True(all.ContainsKey(typeof(E6)));
        Assert.Equal("/Admin/Tenant", all[typeof(E6)].Url);
        Assert.Equal("Id", all[typeof(E6)].PrimaryKey);
    }
}
