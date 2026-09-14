using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NewLife;
using NewLife.Cube;
using NewLife.Cube.Models;
using XCode;
using XCode.Configuration;
using XCode.Membership;
using Xunit;

namespace NewLife.Cube.Tests.Common;

/// <summary>批量导入行权与归属校验测试（OSC-2608273d95 G1）</summary>
/// <remarks>
/// 导入走批量写库，绕过实体层拦截器与逐行 Valid，因此 ValidImport 是唯一防线：
/// ① 归属不得为他人；② 已声明归属必须在数据范围内；③ 合并类模式不得覆盖不可见行。
/// 本类不执行实际写入，只校验前置判定；FindImportTarget 由测试桩替换以避免数据库访问。
/// </remarks>
public class ImportRowPermissionTests
{
    #region 测试实体
    /// <summary>导入行权测试实体。实现 IDataScope（用户+部门）</summary>
    [BindTable("ImportScopeEntity", "导入行权测试实体", ConnName = "Test")]
    private class ImportScopeEntity : Entity<ImportScopeEntity>, IDataScope, IDataScopeFieldProvider
    {
        private Int32 _Id;
        /// <summary>编号</summary>
        [DisplayName("编号")]
        [DataObjectField(true, true, false, 0)]
        public Int32 Id { get => _Id; set { if (OnPropertyChanging("Id", value)) { _Id = value; OnPropertyChanged("Id"); } } }

        private Int32 _UserId;
        /// <summary>用户</summary>
        [DisplayName("用户")]
        [DataObjectField(false, false, false, 0)]
        public Int32 UserId { get => _UserId; set { if (OnPropertyChanging("UserId", value)) { _UserId = value; OnPropertyChanged("UserId"); } } }

        private Int32 _DepartmentId;
        /// <summary>部门</summary>
        [DisplayName("部门")]
        [DataObjectField(false, false, false, 0)]
        public Int32 DepartmentId { get => _DepartmentId; set { if (OnPropertyChanging("DepartmentId", value)) { _DepartmentId = value; OnPropertyChanged("DepartmentId"); } } }

        /// <summary>索引器重写：按字段名读写私有字段</summary>
        public override Object? this[String name]
        {
            get => name switch
            {
                "Id" => _Id,
                "UserId" => _UserId,
                "DepartmentId" => _DepartmentId,
                _ => base[name],
            };
            set
            {
                switch (name)
                {
                    case "Id": _Id = value.ToInt(); break;
                    case "UserId": _UserId = value.ToInt(); break;
                    case "DepartmentId": _DepartmentId = value.ToInt(); break;
                    default: base[name] = value; break;
                }
            }
        }

        // 字段名即为接口默认名（UserId/DepartmentId），返回 null 交回默认解析
        FieldItem? IDataScopeFieldProvider.GetUserField() => null;
        FieldItem? IDataScopeFieldProvider.GetDepartmentField() => null;
        FieldItem? IDataScopeFieldProvider.GetTenantField() => null;
    }

    /// <summary>无归属接口实体。用于验证跨实体数据集权限校验</summary>
    [BindTable("ImportPlainEntity", "无接口导入测试实体", ConnName = "Test")]
    private class ImportPlainEntity : Entity<ImportPlainEntity>
    {
        private Int32 _Id;
        /// <summary>编号</summary>
        [DisplayName("编号")]
        [DataObjectField(true, true, false, 0)]
        public Int32 Id { get => _Id; set { if (OnPropertyChanging("Id", value)) { _Id = value; OnPropertyChanged("Id"); } } }
    }
    #endregion

    #region 测试控制器
    /// <summary>导入校验测试控制器</summary>
    private class ImportTestController : EntityController<ImportScopeEntity>
    {
        /// <summary>FindImportTarget 桩返回值（模拟「表中已存在行」）</summary>
        public IEntity? Existing { get; set; }

        protected override IEntity FindImportTarget(IEntityFactory factory, IEntity entity) => Existing!;

        public void TestValidImport(IList<IEntity> list, ImportMode mode, Int64 totalRows)
            => ValidImport(Factory, list, new ImportContext { Name = "test", Mode = mode }, totalRows);

        public void TestValidImportEntity(IEntityFactory factory) => ValidImportEntity(factory);

        public IEntity TestFindImportTarget(ImportScopeEntity entity) => FindImportTarget(Factory, entity);
    }
    #endregion

    /// <summary>创建测试控制器并注入数据权限上下文</summary>
    private static ImportTestController CreateController(DataScopeContext? scope)
    {
        var old = PageSetting.Global.OrderByKey;
        try
        {
            // 关闭 OrderByKey，避免构造函数内按主键查询数据库
            PageSetting.Global.OrderByKey = false;

            var http = new DefaultHttpContext();
            if (scope != null) http.Items["DataScopeContext"] = scope;

            var ctrl = new ImportTestController();
            ctrl.ControllerContext = new ControllerContext { HttpContext = http };

            return ctrl;
        }
        finally
        {
            PageSetting.Global.OrderByKey = old;
        }
    }

    private static DataScopeContext SelfScope(Int32 userId = 1) => new() { UserId = userId, DepartmentId = 10, DataScope = DataScopes.仅本人 };

    private static DataScopeContext DeptScope(Int32 userId = 1, params Int32[] depts) => new()
    {
        UserId = userId,
        DepartmentId = 10,
        DataScope = DataScopes.本部门,
        AccessibleDepartmentIds = depts.Length > 0 ? depts : [10],
    };

    private static ImportScopeEntity NewRow(Int32 id = 0, Int32 userId = 0, Int32 deptId = 0) => new() { Id = id, UserId = userId, DepartmentId = deptId };

    [Fact(DisplayName = "导入_新增_未声明归属放行")]
    public void ValidImport_Insert_Unassigned_Allowed()
    {
        var ctrl = CreateController(SelfScope());

        ctrl.TestValidImport([NewRow()], ImportMode.Insert, 0);
    }

    [Fact(DisplayName = "导入_新增_他人归属拒绝")]
    public void ValidImport_Insert_ForgedOwner_Rejected()
    {
        var ctrl = CreateController(SelfScope());

        var ex = Assert.Throws<NoPermissionException>(() => ctrl.TestValidImport([NewRow(userId: 2)], ImportMode.Insert, 0));
        Assert.Contains("归属为他人", ex.Message);
    }

    [Fact(DisplayName = "导入_新增_本人与他部门范围内放行")]
    public void ValidImport_Insert_Owned_Allowed()
    {
        var ctrl = CreateController(DeptScope(1, 10, 11));

        ctrl.TestValidImport([NewRow(userId: 1, deptId: 10), NewRow(deptId: 11)], ImportMode.Insert, 0);
    }

    [Fact(DisplayName = "导入_部门范围外归属拒绝")]
    public void ValidImport_Insert_OutOfDepartment_Rejected()
    {
        var ctrl = CreateController(DeptScope(1, 10));

        var ex = Assert.Throws<NoPermissionException>(() => ctrl.TestValidImport([NewRow(deptId: 99)], ImportMode.Insert, 0));
        Assert.Contains("不在当前数据范围", ex.Message);
    }

    [Fact(DisplayName = "导入_合并_覆盖不可见行拒绝")]
    public void ValidImport_Merge_InvisibleTarget_Rejected()
    {
        var ctrl = CreateController(DeptScope(1, 10));
        ctrl.Existing = NewRow(id: 5, userId: 2, deptId: 99);

        var ex = Assert.Throws<NoPermissionException>(() => ctrl.TestValidImport([NewRow(id: 5)], ImportMode.Merge, 100));
        Assert.Contains("将覆盖不可见数据", ex.Message);
    }

    [Fact(DisplayName = "导入_合并_可见行放行")]
    public void ValidImport_Merge_VisibleTarget_Allowed()
    {
        var ctrl = CreateController(DeptScope(1, 10));
        ctrl.Existing = NewRow(id: 5, userId: 1, deptId: 10);

        ctrl.TestValidImport([NewRow(id: 5)], ImportMode.Merge, 100);
    }

    [Fact(DisplayName = "导入_Auto模式_空表按新增判定")]
    public void ValidImport_Auto_EmptyTable_TreatedAsInsert()
    {
        var ctrl = CreateController(DeptScope(1, 10));
        // 空表按新增：未声明归属不清库校验，Existing 即使不可见也不参与判定
        ctrl.Existing = NewRow(id: 5, deptId: 99);

        ctrl.TestValidImport([NewRow(id: 5)], ImportMode.Auto, 0);
    }

    [Fact(DisplayName = "导入_系统态_不介入")]
    public void ValidImport_SystemScope_NotIntervened()
    {
        var ctrl = CreateController(new DataScopeContext { UserId = 1, DepartmentId = 10, DataScope = DataScopes.全部 });

        ctrl.TestValidImport([NewRow(userId: 2, deptId: 99)], ImportMode.Insert, 0);
    }

    [Fact(DisplayName = "导入_跨实体数据集_无页面权限拒绝")]
    public void ValidImportEntity_OtherEntity_NoMenu_Rejected()
    {
        var ctrl = CreateController(DeptScope(1, 10));

        // 测试实体未注册页面菜单，按 fail-closed 拒绝（系统角色不受约束）
        var ex = Assert.Throws<NoPermissionException>(() => ctrl.TestValidImportEntity(ImportPlainEntity.Meta.Factory));
        Assert.Contains("无权导入", ex.Message);
    }

    [Fact(DisplayName = "导入_跨实体数据集_当前实体放行")]
    public void ValidImportEntity_SameEntity_Allowed()
    {
        var ctrl = CreateController(DeptScope(1, 10));

        ctrl.TestValidImportEntity(ImportScopeEntity.Meta.Factory);
    }

    [Fact(DisplayName = "导入_主键未声明_不查库")]
    public void FindImportTarget_NoKey_NoQuery()
    {
        var ctrl = CreateController(DeptScope(1, 10));

        // 主键为 0 时不触发按主键查询（避免导入时逐行查库）
        Assert.Null(ctrl.TestFindImportTarget(NewRow()));
    }
}
