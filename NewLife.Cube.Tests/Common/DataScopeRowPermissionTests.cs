using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NewLife;
using NewLife.Cube;
using NewLife.Cube.Areas.Admin.Controllers;
using NewLife.Data;
using NewLife.Web;
using XCode;
using XCode.Configuration;
using XCode.Membership;
using Xunit;

namespace NewLife.Cube.Tests.Common;

/// <summary>接口层行权测试（OSC-2608273d95）。验证列表/详情/写入的 DataScope 接线与脱敏上下文来源</summary>
/// <remarks>
/// 路线 A：不改 XCode 仓库、不给实体挂 DataScopeInterceptor，行权由控制器显式构造上下文后调用
/// DataScopeHelper.GetFilter/CanAccess。本类不访问数据库：分页/详情都通过派生控制器桩替换取数。
/// </remarks>
public class DataScopeRowPermissionTests
{
    #region 测试实体
    /// <summary>行权测试实体。实现 IDataScope（用户+部门）供 GetFilter/CanAccess 解析归属</summary>
    [BindTable("DataScopeTestEntity", "行权测试实体", ConnName = "Test")]
    private class ScopeTestEntity : Entity<ScopeTestEntity>, IDataScope, IDataScopeFieldProvider
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

    /// <summary>不实现任何数据权限接口的实体。用于验证 GetFilter 对无接口实体返回 null（不介入）</summary>
    [BindTable("PlainTestEntity", "无接口测试实体", ConnName = "Test")]
    private class PlainTestEntity : Entity<PlainTestEntity>
    {
        private Int32 _Id;
        /// <summary>编号</summary>
        [DisplayName("编号")]
        [DataObjectField(true, true, false, 0)]
        public Int32 Id { get => _Id; set { if (OnPropertyChanging("Id", value)) { _Id = value; OnPropertyChanged("Id"); } } }

        private String _Name = null!;
        /// <summary>名称</summary>
        [DisplayName("名称")]
        [DataObjectField(false, false, false, 50)]
        public String Name { get => _Name; set { if (OnPropertyChanging("Name", value)) { _Name = value; OnPropertyChanged("Name"); } } }

        /// <summary>索引器重写：按字段名读写私有字段</summary>
        public override Object? this[String name]
        {
            get => name switch { "Id" => _Id, "Name" => _Name, _ => base[name] };
            set
            {
                switch (name)
                {
                    case "Id": _Id = value.ToInt(); break;
                    case "Name": _Name = value + ""; break;
                    default: base[name] = value; break;
                }
            }
        }
    }
    #endregion

    #region 测试控制器
    /// <summary>行权测试控制器。Search/Find 桩化，避免访问数据库</summary>
    private class ScopeTestController : ReadOnlyEntityController<ScopeTestEntity>
    {
        /// <summary>SearchData 写入的行权结果（p.State）</summary>
        public Pager? Captured { get; private set; }

        /// <summary>详情行桩数据</summary>
        public ScopeTestEntity? Row { get; set; }

        protected override IEnumerable<ScopeTestEntity> Search(Pager p)
        {
            Captured = p;
            return [];
        }

        protected override ScopeTestEntity Find(Object key) => Row!;

        protected override void WriteLog(String action, Boolean success, String remark) { }

        public Expression? TestCreateWhere() => CreateWhere()?.GetExpression();
        public Expression? TestScopeExpression() => GetDataScopeExpression();
        public DataScopeContext? TestContext() => GetDataScopeContext();
        public Boolean TestCanAccess(ScopeTestEntity e) => CanAccess(e);
        public Boolean TestValidPermission(ScopeTestEntity e, DataObjectMethodType t) => ValidPermission(e, t, true);
        public ScopeTestEntity TestFindData(Object key) => FindData(key);
        public void TestSearchData(Pager p) => SearchData(p).ToList();
        public Func<String, Boolean> TestAllowSearchOrList() => AllowSearchOrList();
    }
    #endregion

    /// <summary>创建测试控制器，并注入指定数据权限上下文（模拟 DataScopeMiddleware 的宿主态 + 真实用户态）</summary>
    private static ScopeTestController CreateController(DataScopeContext? scope)
    {
        var old = PageSetting.Global.OrderByKey;
        try
        {
            // 关闭 OrderByKey，避免构造函数内按主键查询数据库
            PageSetting.Global.OrderByKey = false;

            var http = new DefaultHttpContext();
            if (scope != null) http.Items["DataScopeContext"] = scope;

            var ctrl = new ScopeTestController();
            ctrl.ControllerContext = new ControllerContext { HttpContext = http };

            return ctrl;
        }
        finally
        {
            PageSetting.Global.OrderByKey = old;
        }
    }

    private static DataScopeContext SelfScope(Int32 userId = 1) => new() { UserId = userId, DepartmentId = 10, DataScope = DataScopes.仅本人 };

    [Fact(DisplayName = "列表_仅本人_行权表达式按归属用户列")]
    public void SearchData_SelfScope_FiltersByOwner()
    {
        var ctrl = CreateController(SelfScope());
        var p = new Pager();

        ctrl.TestSearchData(p);

        var exp = Assert.IsAssignableFrom<Expression>(p.State);
        Assert.Contains("UserId", exp.ToString());
    }

    [Fact(DisplayName = "列表_本部门_行权表达式含部门集合")]
    public void SearchData_DepartmentScope_FiltersByDepartments()
    {
        var ctrl = CreateController(new DataScopeContext { UserId = 1, DepartmentId = 10, DataScope = DataScopes.本部门, AccessibleDepartmentIds = [10, 11] });
        var p = new Pager();

        ctrl.TestSearchData(p);

        var exp = Assert.IsAssignableFrom<Expression>(p.State);
        var sql = exp.ToString();
        Assert.Contains("DepartmentId", sql);
        Assert.Contains("10", sql);
    }

    [Fact(DisplayName = "列表_系统态_不追加行权条件")]
    public void SearchData_SystemScope_NoFilter()
    {
        // 宿主系统态（DataScopes.全部）等价于不过滤，与合并后架构一致
        var ctrl = CreateController(new DataScopeContext { UserId = 1, DepartmentId = 10, DataScope = DataScopes.全部 });
        var p = new Pager();

        ctrl.TestSearchData(p);

        Assert.Null(p.State);
        Assert.Null(ctrl.TestScopeExpression());
    }

    [Fact(DisplayName = "列表_无数据权限实体_行权不介入")]
    public void SearchData_NoScopeEntity_NoFilter()
    {
        var ctrl = CreateController(SelfScope());

        // 无接口实体：GetFilter 返回 null（行权不介入），列表不被收窄
        Assert.Null(DataScopeHelper.GetFilter(Entity<PlainTestEntity>.Meta.Factory, SelfScope()));

        // 对照：实现了 IDataScope 的实体确实被行权表达式约束
        var p = new Pager();
        ctrl.TestSearchData(p);
        Assert.NotNull(p.State);
    }

    [Fact(DisplayName = "写入_新增伪造他人归属_拒绝_本人或未声明放行")]
    public void ValidPermission_Insert_ForgedOwner_Rejected()
    {
        var ctrl = CreateController(SelfScope(1));

        // 声明他人归属：拒绝（实体层拦截器休眠，保存链路不兜底归属，不得建出他人名义的数据）
        Assert.False(ctrl.TestValidPermission(new ScopeTestEntity { UserId = 2 }, DataObjectMethodType.Insert));

        // 本人 / 未声明归属（0）：放行
        Assert.True(ctrl.TestValidPermission(new ScopeTestEntity { UserId = 1 }, DataObjectMethodType.Insert));
        Assert.True(ctrl.TestValidPermission(new ScopeTestEntity { UserId = 0 }, DataObjectMethodType.Insert));
    }

    [Fact(DisplayName = "写入_新增_系统态放行任意归属")]
    public void ValidPermission_Insert_SystemScope_Allowed()
    {
        var ctrl = CreateController(new DataScopeContext { UserId = 1, DataScope = DataScopes.全部 });

        Assert.True(ctrl.TestValidPermission(new ScopeTestEntity { UserId = 2 }, DataObjectMethodType.Insert));
    }

    [Fact(DisplayName = "行权上下文_取自请求缓存_不写 Current")]
    public void GetDataScopeContext_DoesNotWriteCurrent()
    {
        var old = DataScopeContext.Current;
        try
        {
            DataScopeContext.Current = new DataScopeContext { UserId = 99, DataScope = DataScopes.全部 };
            var ctrl = CreateController(SelfScope(7));

            var ctx = ctrl.TestContext();

            Assert.NotNull(ctx);
            Assert.Equal(7, ctx!.UserId);
            // 宿主系统态必须保持原样，否则实体层拦截器会被唤醒
            Assert.Equal(99, DataScopeContext.Current!.UserId);
        }
        finally
        {
            DataScopeContext.Current = old;
        }
    }

    [Fact(DisplayName = "详情_越权_拒绝访问")]
    public void FindData_OtherUser_Rejected()
    {
        var ctrl = CreateController(SelfScope(1));
        ctrl.Row = new ScopeTestEntity { Id = 5, UserId = 2, DepartmentId = 10 };

        var ex = Assert.Throws<InvalidOperationException>(() => ctrl.TestFindData(5));

        Assert.Contains("非法访问数据", ex.Message);
    }

    [Fact(DisplayName = "详情_本人_放行")]
    public void FindData_Self_Allowed()
    {
        var ctrl = CreateController(SelfScope(1));
        var row = new ScopeTestEntity { Id = 5, UserId = 1, DepartmentId = 10 };
        ctrl.Row = row;

        Assert.Same(row, ctrl.TestFindData(5));
    }

    [Fact(DisplayName = "写入_更新他人_拒绝_新增放行")]
    public void ValidPermission_UpdateOther_Rejected_InsertAllowed()
    {
        var ctrl = CreateController(SelfScope(1));
        var other = new ScopeTestEntity { Id = 5, UserId = 2, DepartmentId = 10 };
        var self = new ScopeTestEntity { Id = 5, UserId = 1, DepartmentId = 10 };

        Assert.False(ctrl.TestValidPermission(other, DataObjectMethodType.Update));
        Assert.True(ctrl.TestValidPermission(self, DataObjectMethodType.Update));

        // 新增不校验归属（主键与归属未落库），仍由保存链路赋值
        Assert.True(ctrl.TestValidPermission(new ScopeTestEntity { UserId = 0 }, DataObjectMethodType.Insert));
    }

    [Fact(DisplayName = "脱敏字段名_用真实用户上下文_不用宿主态")]
    public void GetSensitiveFieldNames_UsesRealUserContext()
    {
        // 宿主态 ViewSensitive=false，真实用户已获敏感查看权限 → 必须返回空（说明没读 Current）
        var old = DataScopeContext.Current;
        try
        {
            DataScopeContext.Current = new DataScopeContext { UserId = 1, DataScope = DataScopes.全部 };

            var method = typeof(ReadOnlyEntityController<User>).GetMethod("GetSensitiveFieldNames", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("未找到 GetSensitiveFieldNames 方法");

            var allowCtx = new DataScopeContext { UserId = 2, ViewSensitive = true };
            var allowNames = (String[])InvokeUserController(method, allowCtx);
            Assert.Empty(allowNames);

            var denyCtx = new DataScopeContext { UserId = 2, ViewSensitive = false };
            var denyNames = (String[])InvokeUserController(method, denyCtx);
            Assert.Contains("Password", denyNames);
        }
        finally
        {
            DataScopeContext.Current = old;
        }
    }

    /// <summary>在无构造函数依赖的 UserController 实例上调用基类方法</summary>
    private static Object InvokeUserController(MethodInfo method, DataScopeContext scope, Object[] args = null)
    {
        var ctrl = (UserController)RuntimeHelpers.GetUninitializedObject(typeof(UserController));
        var http = new DefaultHttpContext();
        http.Items["DataScopeContext"] = scope;
        ctrl.ControllerContext = new ControllerContext { HttpContext = http };

        return method.Invoke(ctrl, args)!;
    }

    [Fact(DisplayName = "列表脱敏_用真实上下文_不用宿主态")]
    public void MaskSensitiveList_UsesRealUserContext()
    {
        var old = DataScopeContext.Current;
        try
        {
            // 宿主态在 Current（模拟中间件注入），真实用户态在请求缓存
            DataScopeContext.Current = new DataScopeContext { UserId = 99, DataScope = DataScopes.全部 };

            var method = typeof(ReadOnlyEntityController<User>).GetMethod("MaskSensitiveList", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("未找到 MaskSensitiveList 方法");

            // 以「他人」身份看列表：自己那条不遮蔽，他人置掩码
            var list = new List<User> { new() { ID = 1, Password = "self" }, new() { ID = 2, Password = "other" } };
            var masked = (IList<User>)InvokeUserController(method, new DataScopeContext { UserId = 1, DataScope = DataScopes.仅本人 }, [list])!;

            Assert.Equal(2, masked.Count);
            Assert.Equal("self", list[0].Password);
            Assert.Equal("***", list[1].Password);
        }
        finally
        {
            DataScopeContext.Current = old;
        }
    }

    [Fact(DisplayName = "列表_viewFilter 与行权同存_any 不放大行权")]
    public void SearchData_ViewFilter_DoesNotExpandScope()
    {
        var ctrl = CreateController(new DataScopeContext { UserId = 1, DepartmentId = 10, DataScope = DataScopes.本部门, AccessibleDepartmentIds = [10, 11] });

        // 取一个列表/搜索白名单内的字段，保证 viewFilter 可下推
        var allowed = ctrl.TestAllowSearchOrList();
        var field = new[] { "Id", "UserId", "DepartmentId" }.First(allowed);

        var p = new Pager();
        p["viewFilter"] = $"{{\"logic\":\"any\",\"conditions\":[{{\"field\":\"{field}\",\"op\":\"eq\",\"value\":5}}]}}";

        ctrl.TestSearchData(p);

        var sql = Assert.IsAssignableFrom<Expression>(p.State).ToString();
        // 行权必须存在（本部门 → 部门集合），logic=any 只能 OR 前端条件
        Assert.Contains("DepartmentId", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(field, sql, StringComparison.OrdinalIgnoreCase);
    }
}
