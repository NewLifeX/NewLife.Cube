using System;
using System.Collections.Generic;
using System.ComponentModel;
using NewLife;
using NewLife.Cube;
using NewLife.Cube.AI;
using XCode;
using XCode.Configuration;
using XCode.Membership;
using Xunit;

namespace NewLife.Cube.Tests.Common;

/// <summary>页外出口行权助手测试（OSC-2608273d95 G2/G4）</summary>
/// <remarks>
/// 部件、AI 记录上下文等页外出口统一走 <see cref="CubeDataScope"/>，本类覆盖其判定语义与 AI 取数降级行为。
/// 所有用例都显式传入上下文，不依赖 DataScopeContext.Current（避免受宿主系统态影响）。
/// </remarks>
public class CubeDataScopeTests
{
    #region 测试实体
    /// <summary>页外出口测试实体。实现 IDataScope（用户+部门）</summary>
    [BindTable("CubeScopeEntity", "页外出口测试实体", ConnName = "Test")]
    private class CubeScopeEntity : Entity<CubeScopeEntity>, IDataScope, IDataScopeFieldProvider
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

        FieldItem? IDataScopeFieldProvider.GetUserField() => null;
        FieldItem? IDataScopeFieldProvider.GetDepartmentField() => null;
        FieldItem? IDataScopeFieldProvider.GetTenantField() => null;
    }

    /// <summary>无归属接口实体</summary>
    [BindTable("CubePlainEntity", "无接口页外实体", ConnName = "Test")]
    private class CubePlainEntity : Entity<CubePlainEntity>
    {
        private Int32 _Id;
        /// <summary>编号</summary>
        [DisplayName("编号")]
        [DataObjectField(true, true, false, 0)]
        public Int32 Id { get => _Id; set { if (OnPropertyChanging("Id", value)) { _Id = value; OnPropertyChanged("Id"); } } }
    }
    #endregion

    private static DataScopeContext SelfScope(Int32 userId = 1) => new() { UserId = userId, DepartmentId = 10, DataScope = DataScopes.仅本人 };

    private static CubeScopeEntity NewRow(Int32 id = 0, Int32 userId = 0, Int32 deptId = 0) => new() { Id = id, UserId = userId, DepartmentId = deptId };

    #region 助手语义
    [Fact(DisplayName = "合并_两侧均空返回null")]
    public void Merge_BothNull_ReturnsNull() => Assert.Null(CubeDataScope.Merge(null, null));

    [Fact(DisplayName = "合并_单侧为空取另一侧")]
    public void Merge_OneSideNull_ReturnsOther()
    {
        var scope = User._.ID == 1;

        Assert.Same(scope, CubeDataScope.Merge(null, scope));
        Assert.Same(scope, CubeDataScope.Merge(scope, null));
    }

    [Fact(DisplayName = "归属判定_全为0视为未声明")]
    public void HasOwner_AllZero_False()
    {
        Assert.False(CubeDataScope.HasOwner(NewRow()));
        Assert.False(CubeDataScope.HasOwner(new Object()));
    }

    [Fact(DisplayName = "归属判定_用户或部门任一有值即为已声明")]
    public void HasOwner_AnyValue_True()
    {
        Assert.True(CubeDataScope.HasOwner(NewRow(userId: 2)));
        Assert.True(CubeDataScope.HasOwner(NewRow(deptId: 10)));
    }

    [Fact(DisplayName = "可见性_仅本人_本人行可见他人行不可见")]
    public void CanAccess_SelfScope()
    {
        var ctx = SelfScope(1);

        Assert.True(CubeDataScope.CanAccess(NewRow(userId: 1), ctx));
        Assert.False(CubeDataScope.CanAccess(NewRow(userId: 2), ctx));
    }

    [Fact(DisplayName = "可见性_未声明归属按参数放行或拒绝")]
    public void CanAccess_Unassigned_ByParameter()
    {
        var ctx = SelfScope(1);

        Assert.True(CubeDataScope.CanAccess(NewRow(), ctx, true));
        Assert.False(CubeDataScope.CanAccess(NewRow(), ctx, false));
    }

    [Fact(DisplayName = "可见性_系统态与空上下文不介入")]
    public void CanAccess_SystemOrNull_NotIntervened()
    {
        var sys = new DataScopeContext { UserId = 1, DepartmentId = 10, DataScope = DataScopes.全部 };

        Assert.True(CubeDataScope.CanAccess(NewRow(userId: 2), sys, false));
        Assert.True(CubeDataScope.CanAccess(NewRow(userId: 2), new DataScopeContext { UserId = 1, DataScope = DataScopes.全部 }, false));
    }

    [Fact(DisplayName = "可见性_空实体返回false")]
    public void CanAccess_Null_False() => Assert.False(CubeDataScope.CanAccess(null!, SelfScope(1), false));

    [Fact(DisplayName = "归属伪造_他人为真_本人与未声明为假")]
    public void IsForgedOwner_Cases()
    {
        var ctx = SelfScope(1);

        Assert.True(CubeDataScope.IsForgedOwner(NewRow(userId: 2), ctx));
        Assert.False(CubeDataScope.IsForgedOwner(NewRow(userId: 1), ctx));
        Assert.False(CubeDataScope.IsForgedOwner(NewRow(), ctx));
        Assert.False(CubeDataScope.IsForgedOwner(NewRow(userId: 2), new DataScopeContext { DataScope = DataScopes.全部 }));
    }

    [Fact(DisplayName = "过滤_有归属接口实体按范围生成条件")]
    public void GetFilter_ScopedEntity()
    {
        var exp = CubeDataScope.GetFilter(CubeScopeEntity.Meta.Factory, SelfScope(1));

        Assert.NotNull(exp);
        Assert.Contains("UserId", exp.ToString());
    }

    [Fact(DisplayName = "过滤_无接口实体不生成条件")]
    public void GetFilter_PlainEntity()
    {
        Assert.Null(CubeDataScope.GetFilter(CubePlainEntity.Meta.Factory, SelfScope(1)));
    }
    #endregion

    #region AI 记录上下文
    [Fact(DisplayName = "AI记录上下文_越权委托降级为无权访问")]
    public void CubeTools_RecordDenied_FriendlyError()
    {
        var tools = new CubeTools<CubeScopeEntity>(CubeScopeEntity.Meta.Factory, null, 5, p => [], key => throw new InvalidOperationException($"非法访问数据[{key}]"));

        var json = tools.GetDataContext();

        Assert.Contains("无权访问", json);
    }

    [Fact(DisplayName = "AI记录上下文_有权限返回记录值")]
    public void CubeTools_RecordAllowed_ContainsValues()
    {
        var row = NewRow(id: 5, userId: 1, deptId: 10);
        var tools = new CubeTools<CubeScopeEntity>(CubeScopeEntity.Meta.Factory, null, 5, p => [], key => row);

        var json = tools.GetDataContext();

        Assert.Contains("\"id\":5", json);
        Assert.DoesNotContain("无权访问", json);
    }

    [Fact(DisplayName = "AI表单结构_越权时不抛异常且不带已有值")]
    public void CubeTools_FormSchema_DeniedNotThrow()
    {
        var tools = new CubeTools<CubeScopeEntity>(CubeScopeEntity.Meta.Factory, null, 5, p => [], key => throw new InvalidOperationException($"非法访问数据[{key}]"));

        var json = tools.GetFormSchema("edit");

        Assert.Contains("edit", json);
    }
    #endregion
}
