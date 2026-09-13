using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using NewLife.Cube;
using NewLife.Cube.Areas.Admin.Controllers;
using NewLife.Cube.ViewModels;
using XCode.Membership;
using Xunit;

namespace NewLife.Cube.Tests.Common;

/// <summary>树形实体父级下拉候选测试</summary>
/// <remarks>
/// 症状：部门等树形实体的「父级」在表单里是普通数字输入，必须手填父级编号。
/// 修复：基类为自引用父级字段（ParentID）自动配置 DataSource（ID→层级路径），
/// 并排除当前节点及其后代防止成环；菜单等已有自定义数据源的控制器不被覆盖。
/// </remarks>
public class TreeParentSourceTests
{
    static readonly (Int32, Int32, String)[] Rows =
    [
        (1, 0, "总公司"),
        (2, 1, "上海分公司"),
        (3, 2, "市场部"),
        (4, 2, "行政部"),
        (5, 1, "北京分公司"),
        (6, 0, "独立公司"),
    ];

    [Fact(DisplayName = "树父级_层级路径拼接")]
    public void Build_PathByAncestors()
    {
        var dict = TreeParentSourceHelper.Build(Rows.Select(e => (e.Item1, e.Item2, e.Item3)));

        Assert.Equal(6, dict.Count);
        Assert.Equal("总公司", dict[1]);
        Assert.Equal("总公司/上海分公司", dict[2]);
        Assert.Equal("总公司/上海分公司/市场部", dict[3]);
        Assert.Equal("总公司/上海分公司/行政部", dict[4]);
        Assert.Equal("独立公司", dict[6]);
    }

    [Fact(DisplayName = "树父级_编辑时排除自身与全部后代")]
    public void Build_ExcludesSelfAndDescendants()
    {
        var dict = TreeParentSourceHelper.Build(Rows.Select(e => (e.Item1, e.Item2, e.Item3)), excludeId: 2);

        // 2 自身与其后代 3/4 均被排除，其余保留
        Assert.Equal([1, 5, 6], dict.Keys.OrderBy(e => e));
    }

    [Fact(DisplayName = "树父级_新增时全量可选")]
    public void Build_NoExclude_KeepsAll()
    {
        var dict = TreeParentSourceHelper.Build(Rows.Select(e => (e.Item1, e.Item2, e.Item3)), excludeId: 0);

        Assert.Equal(6, dict.Count);
    }

    [Fact(DisplayName = "树父级_脏数据不抛错_父级不存在只显示自身")]
    public void Build_DanglingParent_KeepsSelf()
    {
        var dict = TreeParentSourceHelper.Build([(1, 99, "孤儿节点"), (0, 0, "无效行")]);

        Assert.Single(dict);
        Assert.Equal("孤儿节点", dict[1]);
    }

    [Fact(DisplayName = "树父级_自环脏数据不死循环")]
    public void Build_SelfCycle_DoesNotHang()
    {
        var dict = TreeParentSourceHelper.Build([(7, 7, "自环"), (8, 7, "子节点")]);

        Assert.Equal("自环", dict[7]);
        Assert.Equal("自环/子节点", dict[8]);
    }

    /// <summary>调用基类受保护的 OnGetFields（不实例化控制器构造函数）</summary>
    private static FieldCollection GetFields<TController>(ViewKinds kind) where TController : class
    {
        var ctrl = (TController)RuntimeHelpers.GetUninitializedObject(typeof(TController));
        var method = typeof(ReadOnlyEntityController<Department>).GetMethod("OnGetFields", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("未找到 OnGetFields 方法");

        return (FieldCollection)method.Invoke(ctrl, [kind, null])!;
    }

    [Theory(DisplayName = "树形实体_表单父级字段自动配候选")]
    [InlineData(ViewKinds.AddForm)]
    [InlineData(ViewKinds.EditForm)]
    public void TreeEntity_ParentField_HasDataSource(ViewKinds kind)
    {
        var fields = GetFields<DepartmentController>(kind);
        var df = fields.GetField("ParentID");

        Assert.NotNull(df);
        Assert.NotNull(df!.DataSource);
    }

    [Fact(DisplayName = "非树实体_不受影响")]
    public void NonTreeEntity_NoParentWiring()
    {
        // 角色实体没有 ParentID，字段集合不应被注入父级候选
        var ctrl = (RoleController)RuntimeHelpers.GetUninitializedObject(typeof(RoleController));
        var method = typeof(ReadOnlyEntityController<Role>).GetMethod("OnGetFields", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var fields = (FieldCollection)method.Invoke(ctrl, [ViewKinds.AddForm, null])!;

        Assert.Null(fields.GetField("ParentID"));
    }
}
