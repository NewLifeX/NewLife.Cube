using System;
using System.ComponentModel;
using NewLife;
using NewLife.Cube;
using NewLife.Model;
using XCode;
using XCode.Configuration;
using XCode.DataAccessLayer;
using XCode.Membership;
using Xunit;

namespace XUnitTest;

/// <summary>OSC-260926c2b8 值集实体行权守卫测试</summary>
/// <remarks>
/// 覆盖 <see cref="LovEntityGuard"/> 与 <see cref="TenantScopeHelper"/> 的判定语义：菜单 Detail 失败关闭、
/// 租户三态（Enforce 空集 / Shadow 放行 / 租户模式过滤）、DataScope 行权与非介入边界。
/// 值集控制器位于 API 版（XUnitTest 引用的 NC 合并版不含 LovController），控制器级 403 与 BatchLabel 省略由冒烟覆盖；
/// 「Enum.* 与外部 HTTP 不进入 Guard」由 LovController 分支保证（枚举翻译行为由 NewLife.Cube.Tests 既有 LovRegistry 测试回归）。
/// </remarks>
[Collection("SqliteDb")]
public class LovEntityGuardTests : IDisposable
{
    #region 测试实体
    /// <summary>行权 + 租户测试实体（用户/部门/租户）</summary>
    [BindTable("LovGuardScope", "值集守卫行权实体", ConnName = "Cube", DbType = DatabaseType.None)]
    private class LovGuardScope : Entity<LovGuardScope>, IDataScope, IDataScopeFieldProvider, ITenantScope
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

        private Int32 _TenantId;
        /// <summary>租户</summary>
        [DisplayName("租户")]
        [DataObjectField(false, false, false, 0)]
        public Int32 TenantId { get => _TenantId; set { if (OnPropertyChanging("TenantId", value)) { _TenantId = value; OnPropertyChanged("TenantId"); } } }

        /// <summary>索引器重写：按字段名读写私有字段</summary>
        public override Object? this[String name]
        {
            get => name switch
            {
                "Id" => _Id,
                "UserId" => _UserId,
                "DepartmentId" => _DepartmentId,
                "TenantId" => _TenantId,
                _ => base[name],
            };
            set
            {
                switch (name)
                {
                    case "Id": _Id = value.ToInt(); break;
                    case "UserId": _UserId = value.ToInt(); break;
                    case "DepartmentId": _DepartmentId = value.ToInt(); break;
                    case "TenantId": _TenantId = value.ToInt(); break;
                    default: base[name] = value; break;
                }
            }
        }

        // 字段名即接口默认名（UserId/DepartmentId/TenantId），返回 null 交回默认解析
        FieldItem? IDataScopeFieldProvider.GetUserField() => null;
        FieldItem? IDataScopeFieldProvider.GetDepartmentField() => null;
        FieldItem? IDataScopeFieldProvider.GetTenantField() => null;
    }

    /// <summary>无归属接口实体</summary>
    [BindTable("LovGuardPlain", "值集守卫无接口实体", ConnName = "Cube", DbType = DatabaseType.None)]
    private class LovGuardPlain : Entity<LovGuardPlain>
    {
        private Int32 _Id;
        /// <summary>编号</summary>
        [DisplayName("编号")]
        [DataObjectField(true, true, false, 0)]
        public Int32 Id { get => _Id; set { if (OnPropertyChanging("Id", value)) { _Id = value; OnPropertyChanged("Id"); } } }

        private String _Name;
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

    /// <summary>从未注册页面的实体（验证失败关闭）</summary>
    [BindTable("LovGuardBare", "值集守卫未注册实体", ConnName = "Cube", DbType = DatabaseType.None)]
    private class LovGuardBare : Entity<LovGuardBare>
    {
        private Int32 _Id;
        /// <summary>编号</summary>
        [DisplayName("编号")]
        [DataObjectField(true, true, false, 0)]
        public Int32 Id { get => _Id; set { if (OnPropertyChanging("Id", value)) { _Id = value; OnPropertyChanged("Id"); } } }
    }
    #endregion

    #region 夹具
    private readonly IManageProvider? _oldProvider;
    private readonly Boolean _oldEnableTenant;
    private readonly TenantEnforceModes _oldEnforceMode;
    private readonly TenantQueryPolicies _oldQueryPolicy;
    private readonly TenantContext? _oldTenant;

    public LovEntityGuardTests()
    {
        SqliteDb.Ensure();
        DAL.AddConnStr("Cube", "Data Source=LovEntityGuardTests;Mode=Memory;Cache=Shared", null, "SQLite");

        _oldProvider = ManageProvider.Provider;
        _oldEnableTenant = CubeSetting.Current.EnableTenant;
        _oldEnforceMode = CubeSetting.Current.TenantEnforceMode;
        _oldQueryPolicy = CubeSetting.Current.TenantQueryPolicy;
        _oldTenant = TenantContext.Current;
    }

    public void Dispose()
    {
        ManageProvider.Provider = _oldProvider;
        CubeSetting.Current.EnableTenant = _oldEnableTenant;
        CubeSetting.Current.TenantEnforceMode = _oldEnforceMode;
        CubeSetting.Current.TenantQueryPolicy = _oldQueryPolicy;
        TenantContext.Current = _oldTenant;
    }

    /// <summary>当前登录用户桩：ManageProvider.User 经此返回指定用户</summary>
    private sealed class StubProvider : ManageProvider
    {
        public IManageUser? Mock { get; set; }

        public override IManageUser? GetCurrent(IServiceProvider? context = null) => Mock;

        public override void SetCurrent(IManageUser? user, IServiceProvider? context = null) => Mock = user;
    }

    private static void SetUser(IManageUser? user) => ManageProvider.Provider = new StubProvider { Mock = user };

    /// <summary>登记实体页面并插入唯一菜单（权限项含 Detail）</summary>
    private static Menu EnsureMenu(Type entityType, String url)
    {
        EntityPageRegistry.Register(entityType, url, "Id");

        var menu = Menu.FindByUrl(url);
        if (menu == null)
        {
            menu = new Menu
            {
                Name = "守卫菜单" + Guid.NewGuid().ToString("N")[..8],
                Url = url,
                ParentID = 0,
                Visible = true,
                Permission = $"{(Int32)PermissionFlags.Detail}#查看",
            };
            menu.Insert();
            Menu.Meta.Cache.Clear(nameof(LovEntityGuardTests), true);
            menu = Menu.FindByUrl(url);
        }

        return menu!;
    }

    /// <summary>创建带菜单权限的角色与用户（角色落库，用户对象无需落库）</summary>
    private static User CreateUser(Menu? menu, PermissionFlags flags, Boolean isSystem = false)
    {
        var role = new Role
        {
            Name = "守卫角色" + Guid.NewGuid().ToString("N")[..8],
            Enable = true,
            IsSystem = isSystem,
            DataScope = isSystem ? DataScopes.全部 : DataScopes.仅本人,
        };
        if (menu != null && flags != PermissionFlags.None) role.Set(menu.ID, flags);
        role.Insert();

        return new User
        {
            ID = 1001,
            Name = "守卫用户" + Guid.NewGuid().ToString("N")[..8],
            RoleID = role.ID,
            Enable = true,
        };
    }

    /// <summary>插入启用租户</summary>
    private static Tenant CreateTenant()
    {
        var tenant = new Tenant
        {
            Name = "守卫租户" + Guid.NewGuid().ToString("N")[..6],
            Code = "lg" + Guid.NewGuid().ToString("N")[..6],
            Enable = true,
        };
        tenant.Insert();

        return tenant;
    }

    /// <summary>开启多租户并清空租户上下文</summary>
    private static void EnableTenant(TenantEnforceModes mode)
    {
        CubeSetting.Current.EnableTenant = true;
        CubeSetting.Current.TenantEnforceMode = mode;
        TenantContext.Current = null!;
    }
    #endregion

    #region 菜单
    [Fact(DisplayName = "菜单_未注册页面_失败关闭")]
    public void CheckMenu_NoPageRegistry_False()
    {
        SetUser(CreateUser(null, PermissionFlags.None));

        // 从未调用 EntityPageRegistry.Register 的实体类型 → 找不到菜单 → false
        Assert.False(LovEntityGuard.CheckMenu(LovGuardBare.Meta.Factory));
    }

    [Fact(DisplayName = "菜单_无菜单记录_失败关闭")]
    public void CheckMenu_MenuMissing_False()
    {
        var url = "/Admin/LovGuardGhost" + Guid.NewGuid().ToString("N")[..6];
        EntityPageRegistry.Register(typeof(LovGuardPlain), url, "Id");
        SetUser(CreateUser(null, PermissionFlags.None));

        // 页面已注册但菜单表无该 URL → false
        Assert.False(LovEntityGuard.CheckMenu(LovGuardPlain.Meta.Factory));
    }

    [Fact(DisplayName = "菜单_无Detail权限_失败关闭")]
    public void CheckMenu_NoDetail_False()
    {
        var menu = EnsureMenu(typeof(LovGuardScope), "/Admin/LovGuardScope");
        SetUser(CreateUser(menu, PermissionFlags.None));

        Assert.False(LovEntityGuard.CheckMenu(LovGuardScope.Meta.Factory));
    }

    [Fact(DisplayName = "菜单_有Detail权限_通过")]
    public void CheckMenu_WithDetail_True()
    {
        var menu = EnsureMenu(typeof(LovGuardScope), "/Admin/LovGuardScope");
        SetUser(CreateUser(menu, PermissionFlags.Detail));

        Assert.True(LovEntityGuard.CheckMenu(LovGuardScope.Meta.Factory));
    }

    [Fact(DisplayName = "菜单_未登录_失败关闭")]
    public void CheckMenu_NotLoggedIn_False()
    {
        var menu = EnsureMenu(typeof(LovGuardScope), "/Admin/LovGuardScope");
        Assert.NotNull(menu);
        SetUser(null);

        Assert.False(LovEntityGuard.CheckMenu(LovGuardScope.Meta.Factory));
    }
    #endregion

    #region 租户
    [Fact(DisplayName = "租户_Enforce无上下文_空集且单行拒绝")]
    public void Tenant_Enforce_NoContext_FailClosed()
    {
        EnableTenant(TenantEnforceModes.Enforce);

        Assert.Equal("1=0", TenantScopeHelper.GetFilter(LovGuardScope.Meta.Factory));

        var exp = TenantScopeHelper.GetExpression(LovGuardScope.Meta.Factory);
        Assert.NotNull(exp);
        Assert.Contains("1=0", exp!.ToString());

        Assert.False(TenantScopeHelper.CanAccess(LovGuardScope.Meta.Factory, new LovGuardScope { Id = 1, TenantId = 3 }));

        // Guard 组合表达式同样为空集
        Assert.NotNull(LovEntityGuard.GetFilter(LovGuardScope.Meta.Factory));
    }

    [Fact(DisplayName = "租户_Shadow无上下文_兼容放行")]
    public void Tenant_Shadow_NoContext_PassThrough()
    {
        EnableTenant(TenantEnforceModes.Shadow);

        Assert.Null(TenantScopeHelper.GetFilter(LovGuardScope.Meta.Factory));
        Assert.Null(TenantScopeHelper.GetExpression(LovGuardScope.Meta.Factory));
        Assert.True(TenantScopeHelper.CanAccess(LovGuardScope.Meta.Factory, new LovGuardScope { Id = 1, TenantId = 3 }));
    }

    [Fact(DisplayName = "租户_租户模式_按TenantId过滤")]
    public void Tenant_TenantMode_FiltersByTenantId()
    {
        var tenant = CreateTenant();
        EnableTenant(TenantEnforceModes.Enforce);
        TenantContext.Current = new TenantContext { TenantId = tenant.Id };

        Assert.Equal("TenantId={#TenantId}", TenantScopeHelper.GetFilter(LovGuardScope.Meta.Factory));

        var exp = TenantScopeHelper.GetExpression(LovGuardScope.Meta.Factory);
        Assert.NotNull(exp);
        Assert.Contains("TenantId", exp!.ToString());
        Assert.Contains(tenant.Id.ToString(), exp.ToString());

        Assert.True(TenantScopeHelper.CanAccess(LovGuardScope.Meta.Factory, new LovGuardScope { Id = 1, TenantId = tenant.Id }));
        Assert.False(TenantScopeHelper.CanAccess(LovGuardScope.Meta.Factory, new LovGuardScope { Id = 2, TenantId = tenant.Id + 1 }));
    }

    [Fact(DisplayName = "租户_ThrowOnMissingTenant_显式抛错")]
    public void Tenant_ThrowOnMissingTenant_Throws()
    {
        CubeSetting.Current.EnableTenant = true;
        CubeSetting.Current.TenantEnforceMode = TenantEnforceModes.Enforce;
        CubeSetting.Current.TenantQueryPolicy = TenantQueryPolicies.ThrowOnMissingTenant;
        TenantContext.Current = null!;

        Assert.Throws<NoPermissionException>(() => TenantScopeHelper.GetFilter(LovGuardScope.Meta.Factory));
    }

    [Fact(DisplayName = "租户_非租户实体_不介入")]
    public void Tenant_NonTenantEntity_NotApplied()
    {
        EnableTenant(TenantEnforceModes.Enforce);

        Assert.Null(TenantScopeHelper.GetFilter(LovGuardPlain.Meta.Factory));
        Assert.True(TenantScopeHelper.CanAccess(LovGuardPlain.Meta.Factory, new LovGuardPlain { Id = 1, Name = "x" }));
    }
    #endregion

    #region 行权
    [Fact(DisplayName = "行权_仅本人_过滤表达式按用户列")]
    public void Guard_Filter_SelfScope_ByUser()
    {
        CubeSetting.Current.EnableTenant = false;
        SetUser(CreateUser(null, PermissionFlags.None));

        var exp = LovEntityGuard.GetFilter(LovGuardScope.Meta.Factory);

        Assert.NotNull(exp);
        Assert.Contains("UserId", exp!.ToString());
    }

    [Fact(DisplayName = "行权_单行判定_本人可见他人不可见")]
    public void Guard_CanAccess_SelfOnly()
    {
        CubeSetting.Current.EnableTenant = false;
        var user = CreateUser(null, PermissionFlags.None);
        SetUser(user);

        Assert.True(LovEntityGuard.CanAccess(LovGuardScope.Meta.Factory, new LovGuardScope { Id = 1, UserId = user.ID }));
        Assert.False(LovEntityGuard.CanAccess(LovGuardScope.Meta.Factory, new LovGuardScope { Id = 2, UserId = user.ID + 1 }));
        Assert.False(LovEntityGuard.CanAccess(LovGuardScope.Meta.Factory, null!));
    }

    [Fact(DisplayName = "行权_系统角色_不产生过滤条件")]
    public void Guard_SystemRole_NoScopeFilter()
    {
        CubeSetting.Current.EnableTenant = false;
        SetUser(CreateUser(null, PermissionFlags.None, isSystem: true));

        Assert.Null(LovEntityGuard.GetFilter(LovGuardScope.Meta.Factory));
    }

    [Fact(DisplayName = "行权_系统角色_租户Enforce仍约束")]
    public void Guard_SystemRole_TenantStillApplies()
    {
        SetUser(CreateUser(null, PermissionFlags.None, isSystem: true));
        EnableTenant(TenantEnforceModes.Enforce);

        var exp = LovEntityGuard.GetFilter(LovGuardScope.Meta.Factory);

        Assert.NotNull(exp);
        Assert.Contains("1=0", exp!.ToString());
    }

    [Fact(DisplayName = "行权_无归属接口实体_只受菜单与租户")]
    public void Guard_PlainEntity_NoScopeFilter()
    {
        CubeSetting.Current.EnableTenant = false;
        SetUser(CreateUser(null, PermissionFlags.None));

        Assert.Null(LovEntityGuard.GetFilter(LovGuardPlain.Meta.Factory));
        Assert.True(LovEntityGuard.CanAccess(LovGuardPlain.Meta.Factory, new LovGuardPlain { Id = 1, Name = "x" }));
    }
    #endregion
}
