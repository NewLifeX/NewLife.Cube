using System;
using System.Collections.Generic;
using NewLife.Cube;
using NewLife.Cube.ViewModels;
using NewLife.Data;
using XCode;
using Xunit;

namespace NewLife.Cube.Tests.ViewModels;

/// <summary>覆盖 <see cref="ListField.GetUrl"/> 的跨 Area 外键跳转链接构造逻辑。</summary>
/// <remarks>
/// 对应 Issue #66：当列表字段为 Map 外键且未显式配置 Url 时，从 <see cref="EntityPageRegistry"/>
/// 读取目标实体对应的菜单地址（含 Area 前缀），实现跨 Area 跳转。
/// 各用例使用独立实体类型，避免污染静态注册表。
/// </remarks>
public class ListFieldGetUrlTests
{
    private class RoleEntity { }
    private class DeptEntity { }

    /// <summary>轻量 IModel 实现，避免依赖数据库实体</summary>
    private class TestModel : IModel
    {
        private readonly Dictionary<String, Object?> _dic = new(StringComparer.OrdinalIgnoreCase);

        public Object? this[String name]
        {
            get => _dic.TryGetValue(name, out var value) ? value : null;
            set => _dic[name] = value;
        }
    }

    [Fact(DisplayName = "已注册实体：生成含 Area 前缀的跨 Area 链接")]
    public void RegisteredEntity_ReturnsAreaUrl()
    {
        // 模拟菜单扫描时注册：Role 实体对应 /Admin/Role，主键 ID
        EntityPageRegistry.Register(typeof(RoleEntity), "/Admin/Role", "ID");

        var lf = new ListField
        {
            MapProvider = new MapProvider { EntityType = typeof(RoleEntity), Key = "ID" },
            MapField = "RoleId",
        };

        var model = new TestModel { ["RoleId"] = 5 };

        var url = lf.GetUrl(model);

        Assert.Equal("/Admin/Role?ID=5", url);
    }

    [Fact(DisplayName = "未注册实体：回退到不含 Area 前缀的旧路径")]
    public void UnregisteredEntity_FallbackToOldPath()
    {
        // 不注册 DeptEntity，验证回退行为
        var lf = new ListField
        {
            MapProvider = new MapProvider { EntityType = typeof(DeptEntity), Key = "ID" },
            MapField = "DeptId",
        };

        var model = new TestModel { ["DeptId"] = 3 };

        var url = lf.GetUrl(model);

        Assert.Equal("/DeptEntity?ID=3", url);
    }

    [Fact(DisplayName = "显式配置 Url 时优先于注册表")]
    public void ExplicitUrl_TakesPrecedence()
    {
        EntityPageRegistry.Register(typeof(RoleEntity), "/Admin/Role", "ID");

        var lf = new ListField
        {
            Url = "/Custom/Path?x={Id}",
            MapProvider = new MapProvider { EntityType = typeof(RoleEntity), Key = "ID" },
            MapField = "RoleId",
        };

        var model = new TestModel { ["Id"] = 7, ["RoleId"] = 5 };

        var url = lf.GetUrl(model);

        Assert.Equal("/Custom/Path?x=7", url);
    }

    [Fact(DisplayName = "无 Url 且无 MapProvider：返回 null")]
    public void NoUrl_NoMapProvider_ReturnsNull()
    {
        var lf = new ListField();

        Assert.Null(lf.GetUrl(new TestModel()));
    }

    [Fact(DisplayName = "有 MapProvider 但 MapField 为空：返回 null")]
    public void MapFieldEmpty_ReturnsNull()
    {
        var lf = new ListField
        {
            MapProvider = new MapProvider { EntityType = typeof(RoleEntity), Key = "ID" },
        };

        Assert.Null(lf.GetUrl(new TestModel()));
    }

    [Fact(DisplayName = "嵌套属性占位符与多值替换")]
    public void Replace_MultiplePlaceholders()
    {
        var lf = new ListField
        {
            Url = "/Detail?userId={User.Id}&name={Name}",
        };

        var model = new TestModel { ["Name"] = "admin" };
        model["User"] = new TestModel { ["Id"] = 9 };

        var url = lf.GetUrl(model);

        Assert.Equal("/Detail?userId=9&name=admin", url);
    }
}
