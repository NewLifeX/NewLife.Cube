using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NewLife;
using NewLife.Cube.Areas.Admin.Controllers;
using NewLife.Model;
using XCode;
using XCode.Configuration;
using XCode.DataAccessLayer;
using XCode.Membership;
using Xunit;
using LovBatchLabelRequest = NewLife.Cube.Services.LovBatchLabelRequest;

namespace NewLife.Cube.Tests.Services;

/// <summary>LovController 守卫测试夹具：注册独立 SQLite 连接（<see cref="ConnName"/>），类结束时移除连接</summary>
/// <remarks>
/// 使用独立连接名、不占用全局 Membership/Cube 连接：避免覆盖全局连接导致后续测试类静默失效
/// （如 TenantAuthFixture 用 <c>TryAdd</c> 建立 Cube→Membership 映射，若 "Cube" 键被临时库占用则映射失效，踩坑实录）。
/// </remarks>
public class LovControllerGuardDbFixture : IDisposable
{
    /// <summary>独立连接名。避免与其它测试集合共享的 Membership/Cube 连接缓存冲突</summary>
    public const String ConnName = "LovCtrlGuard";

    /// <summary>数据库文件</summary>
    public String DbFile { get; }

    /// <summary>实例化，注册独立 SQLite 连接</summary>
    public LovControllerGuardDbFixture()
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "Data");
        Directory.CreateDirectory(dir);
        DbFile = Path.Combine(dir, $"LovCtrlGuard_{Guid.NewGuid().ToString("N")[..12]}.db");

        DAL.AddConnStr(ConnName, $"Data Source={DbFile}", null, "SQLite");
    }

    /// <summary>释放连接、移除独立连接并清理库文件，避免影响其它测试集合</summary>
    public void Dispose()
    {
        try { DAL.Create(ConnName).Reset(); } catch { }
        DAL.ConnStrs?.TryRemove(ConnName, out _);

        // 清理库文件（含 WAL/SHM 残留），避免历史数据污染下一次运行
        try
        {
            foreach (var f in Directory.GetFiles(Path.GetDirectoryName(DbFile)!, Path.GetFileName(DbFile) + "*"))
            {
                File.Delete(f);
            }
        }
        catch { }
    }
}

/// <summary>LovController 值集行权入口测试（OSC-260926c2b8 收尾审计 G5）</summary>
/// <remarks>
/// 在 API 版（LovController 所在程序集）验证 design §1.2 的控制器级行为：
/// entity: ListData 无 Detail → 403 空数据；BatchLabel 不可见键省略、Enum.* 翻译不变；
/// 租户 Enforce 无上下文 → 空集。直接调用控制器方法，不经过 MVC 授权管线（EntityAuthorize 不拦截），聚焦 Guard 本身。
/// </remarks>
public class LovControllerGuardTests : IDisposable, IClassFixture<LovControllerGuardDbFixture>
{
    #region 测试实体
    /// <summary>行权测试实体（用户 + 部门归属 + 租户）</summary>
    [BindTable("LovCtrlScope", "值集控制器守卫实体", ConnName = LovControllerGuardDbFixture.ConnName, DbType = DatabaseType.None)]
    private class LovCtrlScope : Entity<LovCtrlScope>, IDataScope, IDataScopeFieldProvider, ITenantScope
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
    #endregion

    #region 夹具
    private const String PageUrl = "/Admin/LovCtrlScope";

    private readonly IManageProvider? _oldProvider;
    private readonly Boolean _oldEnableTenant;
    private readonly TenantEnforceModes _oldEnforceMode;
    private readonly TenantQueryPolicies _oldQueryPolicy;
    private readonly TenantContext? _oldTenant;
    private readonly Boolean _oldOrderByKey;
    private readonly String? _oldMenuConn;
    private readonly String? _oldRoleConn;
    private readonly String? _oldUserConn;
    private readonly String? _oldParamConn;

    public LovControllerGuardTests(LovControllerGuardDbFixture fixture)
    {
        _ = fixture;

        // Membership 实体（Menu/Role/User/Parameter）重映射到独立连接（线程级配置，须在测试线程设置；类结束时还原）
        _oldMenuConn = Menu.Meta.ConnName;
        _oldRoleConn = Role.Meta.ConnName;
        _oldUserConn = User.Meta.ConnName;
        _oldParamConn = Parameter.Meta.ConnName;

        Menu.Meta.ConnName = LovControllerGuardDbFixture.ConnName;
        Role.Meta.ConnName = LovControllerGuardDbFixture.ConnName;
        User.Meta.ConnName = LovControllerGuardDbFixture.ConnName;
        Parameter.Meta.ConnName = LovControllerGuardDbFixture.ConnName;

        _oldProvider = ManageProvider.Provider;
        _oldEnableTenant = CubeSetting.Current.EnableTenant;
        _oldEnforceMode = CubeSetting.Current.TenantEnforceMode;
        _oldQueryPolicy = CubeSetting.Current.TenantQueryPolicy;
        _oldTenant = TenantContext.Current;
        _oldOrderByKey = PageSetting.Global.OrderByKey;

        // 关闭 OrderByKey，避免控制器构造内按主键查询数据库
        PageSetting.Global.OrderByKey = false;

        // 注册实体页面；菜单按用例需要再插入
        EntityPageRegistry.Register(typeof(LovCtrlScope), PageUrl, "Id");

        // 触发实体工厂注册（EntityFactory.Entities 按需填充），供 LovController 按类型解析实体值集
        _ = typeof(LovCtrlScope).AsFactory();
    }

    public void Dispose()
    {
        // 还原 Membership 实体连接映射（线程级）
        Menu.Meta.ConnName = _oldMenuConn;
        Role.Meta.ConnName = _oldRoleConn;
        User.Meta.ConnName = _oldUserConn;
        Parameter.Meta.ConnName = _oldParamConn;

        PageSetting.Global.OrderByKey = _oldOrderByKey;
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

    /// <summary>插入唯一菜单（权限项含 Detail）</summary>
    private static Menu EnsureMenu()
    {
        var menu = Menu.FindByUrl(PageUrl);
        if (menu == null)
        {
            menu = new Menu
            {
                Name = "控制器守卫菜单" + Guid.NewGuid().ToString("N")[..8],
                Url = PageUrl,
                ParentID = 0,
                Visible = true,
                Permission = $"{(Int32)PermissionFlags.Detail}#查看",
            };
            menu.Insert();
            Menu.Meta.Cache.Clear(nameof(LovControllerGuardTests), true);
            menu = Menu.FindByUrl(PageUrl);
        }

        return menu!;
    }

    /// <summary>创建带菜单权限的角色与用户（角色落库，用户对象无需落库）。每用例使用独立用户 ID 以隔离共享库数据</summary>
    private static User CreateUser(Int32 userId, Menu? menu, PermissionFlags flags)
    {
        var role = new Role
        {
            Name = "控制器守卫角色" + Guid.NewGuid().ToString("N")[..8],
            Enable = true,
            DataScope = DataScopes.仅本人,
        };
        if (menu != null && flags != PermissionFlags.None) role.Set(menu.ID, flags);
        role.Insert();

        return new User
        {
            ID = userId,
            Name = "控制器守卫用户" + Guid.NewGuid().ToString("N")[..8],
            RoleID = role.ID,
            Enable = true,
        };
    }

    private static LovController CreateController()
    {
        var http = new DefaultHttpContext();
        var ctrl = new LovController();
        ctrl.ControllerContext = new ControllerContext { HttpContext = http };
        return ctrl;
    }

    /// <summary>插入两行数据：1 行归属指定用户（本人），1 行归属他人</summary>
    private static (Int32 SelfId, Int32 OtherId) SeedRows(Int32 selfUserId)
    {
        var self = new LovCtrlScope { UserId = selfUserId, DepartmentId = 10 };
        self.Insert();

        var other = new LovCtrlScope { UserId = selfUserId + 5000, DepartmentId = 20 };
        other.Insert();

        return (self.Id, other.Id);
    }

    private static Dictionary<String, String> BatchResult(Object res) => (Dictionary<String, String>)res;

    private static IDictionary<String, Object> DictOf(Object res) => res.ToDictionary();
    #endregion

    #region entity: ListData
    [Fact(DisplayName = "值集控制器_无菜单Detail_ListData返回403空数据")]
    public async Task ListData_WithoutDetail_Returns403()
    {
        // 无菜单记录 → CheckMenu 失败关闭
        SetUser(CreateUser(9001, null, PermissionFlags.None));

        var ctrl = CreateController();
        var res = await ctrl.ListData(new LovListDataRequest { LovCode = "Entity." + typeof(LovCtrlScope).FullName });

        Assert.Equal(403, ctrl.Response.StatusCode);
        Assert.IsType<JsonResult>(res);
    }

    [Fact(DisplayName = "值集控制器_有Detail_ListData仅返回行权范围内行")]
    public async Task ListData_WithDetail_ReturnsScopedRows()
    {
        var menu = EnsureMenu();
        SetUser(CreateUser(1001, menu, PermissionFlags.Detail));
        var (selfId, otherId) = SeedRows(1001);

        var ctrl = CreateController();
        var res = await ctrl.ListData(new LovListDataRequest { LovCode = "Entity." + typeof(LovCtrlScope).FullName });

        Assert.Equal(200, ctrl.Response.StatusCode);
        var data = DictOf(res)["Data"] as IEnumerable<Dictionary<String, Object>>;
        Assert.NotNull(data);

        var rows = new List<Dictionary<String, Object>>(data!);

        // 仅本人行权：本人行可见、他人行不可见（共享库按唯一 Id 断言，不依赖全库精确行数）
        Assert.Contains(rows, r => r["Id"]?.ToString() == selfId.ToString());
        Assert.DoesNotContain(rows, r => r["Id"]?.ToString() == otherId.ToString());
    }

    [Fact(DisplayName = "值集控制器_Enforce无租户上下文_ListData空集")]
    public async Task ListData_Enforce_NoTenant_Empty()
    {
        var menu = EnsureMenu();
        SetUser(CreateUser(1101, menu, PermissionFlags.Detail));
        SeedRows(1101);

        CubeSetting.Current.EnableTenant = true;
        CubeSetting.Current.TenantEnforceMode = TenantEnforceModes.Enforce;
        TenantContext.Current = null!;

        var ctrl = CreateController();
        var res = await ctrl.ListData(new LovListDataRequest { LovCode = "Entity." + typeof(LovCtrlScope).FullName });

        var data = DictOf(res)["Data"] as IEnumerable<Dictionary<String, Object>>;
        Assert.NotNull(data);
        Assert.Empty(new List<Dictionary<String, Object>>(data!));
    }
    #endregion

    #region BatchLabel
    [Fact(DisplayName = "值集控制器_无菜单Detail_BatchLabel全部省略")]
    public async Task BatchLabel_WithoutDetail_ReturnsEmpty()
    {
        SetUser(CreateUser(1201, null, PermissionFlags.None));
        var (selfId, otherId) = SeedRows(1201);

        var ctrl = CreateController();
        var res = await ctrl.BatchLabel(new LovBatchLabelRequest
        {
            LovCode = "Entity." + typeof(LovCtrlScope).FullName,
            Values = [selfId.ToString(), otherId.ToString()],
        });

        Assert.Empty(BatchResult(res));
    }

    [Fact(DisplayName = "值集控制器_有Detail_BatchLabel省略范围外键且保留范围内键")]
    public async Task BatchLabel_OmitsInvisibleKeys()
    {
        var menu = EnsureMenu();
        SetUser(CreateUser(1301, menu, PermissionFlags.Detail));
        var (selfId, otherId) = SeedRows(1301);

        var ctrl = CreateController();
        var res = await ctrl.BatchLabel(new LovBatchLabelRequest
        {
            LovCode = "Entity." + typeof(LovCtrlScope).FullName,
            Values = [selfId.ToString(), otherId.ToString()],
        });

        var dict = BatchResult(res);

        // 本人行可见并翻译；他人行被省略（防枚举探测）
        Assert.True(dict.ContainsKey(selfId.ToString()));
        Assert.False(dict.ContainsKey(otherId.ToString()));
    }

    [Fact(DisplayName = "值集控制器_非法主键_BatchLabel静默跳过不抛异常")]
    public async Task BatchLabel_InvalidKey_Skipped()
    {
        var menu = EnsureMenu();
        SetUser(CreateUser(1401, menu, PermissionFlags.Detail));
        var (selfId, _) = SeedRows(1401);

        var ctrl = CreateController();
        var res = await ctrl.BatchLabel(new LovBatchLabelRequest
        {
            LovCode = "Entity." + typeof(LovCtrlScope).FullName,
            Values = ["not-a-number", selfId.ToString()],
        });

        var dict = BatchResult(res);
        Assert.True(dict.ContainsKey(selfId.ToString()));
        Assert.False(dict.ContainsKey("not-a-number"));
    }

    [Fact(DisplayName = "值集控制器_枚举型_BatchLabel仍返回枚举文案")]
    public async Task BatchLabel_Enum_StillTranslated()
    {
        var ctrl = CreateController();
        var res = await ctrl.BatchLabel(new LovBatchLabelRequest
        {
            LovCode = "Enum.XCode.Membership.PermissionFlags",
            Values = ["1", "2"],
        });

        var dict = BatchResult(res);
        Assert.Equal("查看", dict["1"]);
        Assert.Equal("添加", dict["2"]);
    }
    #endregion
}
