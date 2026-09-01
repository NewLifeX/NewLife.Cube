using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using NewLife;
using NewLife.Cube;
using XCode;
using XCode.DataAccessLayer;
using XCode.Membership;
using Xunit;

namespace XUnitTest;

/// <summary>
/// 角色权限保存/读取往返集成测试（XCode SQLite 轻量集成）
///
/// 覆盖：RolePermissionHelper.Apply 解析权限串（含 All=-1、额外权限位 16）、
/// Role.SavePermission 序列化（All → -1）、Update 落库后重新读取往返一致、
/// 空串清空权限、受限模式保留未拥有权限。
/// 关键回归：PermissionFlags.All 为 UInt32(0xFFFFFFFF)，落库/解析均以 Int32 -1 表示，
/// 前端「全部」级联生成的 "id#-1" 必须能通过 Apply 保存并读回。
/// </summary>
public class RolePermissionTests : IDisposable
{
    private static readonly String _dbFile = Path.Combine(Path.GetTempPath(), $"roleperm_{Guid.NewGuid():N}.db");
    private static Boolean _inited;

    public RolePermissionTests()
    {
        if (!_inited)
        {
            _inited = true;
            var connStr = $"Data Source={_dbFile};provider=sqlite;Migration=On";
            DAL.AddConnStr("Membership", connStr, null, "sqlite");

            // 触发表结构检查与初始化（自动建表 + 默认角色）
            var _ = Role.Meta.Count;
        }

        // 每个测试独立起点：清空角色缓存，避免跨用例缓存干扰
        Role.Meta.Session.ClearCache("RolePermissionTests", true);
    }

    public void Dispose()
    {
        try
        {
            Role.Meta.Session.ClearCache("RolePermissionTests-dispose", true);
        }
        catch
        {
            // 忽略清理异常
        }
    }

    /// <summary>创建独立测试角色</summary>
    private static Role CreateRole()
    {
        var role = new Role { Name = $"测试角色_{Guid.NewGuid():N}" };
        role.Insert();
        return role;
    }

    [Fact]
    [DisplayName("Apply_解析权限串_含All负一与额外权限位16")]
    public void Apply_ParsesAllAndExtraFlags()
    {
        var role = CreateRole();

        // 52=All(-1)、53=查看(1)、54=自定义权限位(16)
        RolePermissionHelper.Apply(role, "52#-1,53#1,54#16", null, false);

        Assert.Equal(PermissionFlags.All, role.Get(52));
        Assert.Equal(PermissionFlags.Detail, role.Get(53));
        Assert.Equal((PermissionFlags)16, role.Get(54));
        // 未出现的资源不应有权限
        Assert.Equal(PermissionFlags.None, role.Get(55));
    }

    [Fact]
    [DisplayName("Apply_全部权限负一_Update落库后重新读取往返一致")]
    public void Apply_AllNegativeOne_RoundTrip()
    {
        var role = CreateRole();
        var id = role.ID;

        // 全部权限（0xFFFFFFFF）经序列化为 -1，必须能保存
        RolePermissionHelper.Apply(role, "52#-1,53#16", null, false);
        role.Update();

        // 清缓存后从数据库重新读取
        Role.Meta.Session.ClearCache($"roundtrip-{id}", true);
        var reloaded = Role.FindByID(id);
        Assert.NotNull(reloaded);
        Assert.Equal(PermissionFlags.All, reloaded!.Get(52));
        Assert.Equal((PermissionFlags)16, reloaded.Get(53));
        // Permission 字段以 Int32 -1 落库
        Assert.Contains("52#-1", reloaded.Permission);
        Assert.Contains("53#16", reloaded.Permission);
    }

    [Fact]
    [DisplayName("SavePermission_全部权限序列化为负一")]
    public void SavePermission_SerializesAllAsNegativeOne()
    {
        var role = CreateRole();

        role.Set(52, PermissionFlags.All);
        role.Set(53, (PermissionFlags)16);

        // Update 内部 SavePermission 将 Permissions 字典写回 Permission 字段
        role.Update();

        Assert.Contains("52#-1", role.Permission);
        Assert.Contains("53#16", role.Permission);
        Assert.DoesNotContain("4294967295", role.Permission);
    }

    [Fact]
    [DisplayName("Apply_空字符串_清空全部权限")]
    public void Apply_EmptyString_ClearsAll()
    {
        var role = CreateRole();
        role.Set(52, PermissionFlags.All);
        role.Set(53, PermissionFlags.Detail);
        role.Update();

        RolePermissionHelper.Apply(role, "", null, false);
        role.Update();

        Assert.Equal(PermissionFlags.None, role.Get(52));
        Assert.Equal(PermissionFlags.None, role.Get(53));
        Assert.True(role.Permission.IsNullOrEmpty());
    }

    [Fact]
    [DisplayName("Apply_受限模式_无自有权限时不越权修改并保留系统授予")]
    public void Apply_Restricted_NoOwnedPermissions_KeepsSystemGrants()
    {
        var role = CreateRole();
        // 系统管理员先授予 52=All、53=Detail
        role.Set(52, PermissionFlags.All);
        role.Set(53, PermissionFlags.Detail);
        role.Update();

        // 受限管理员（current=null 表示无任何自有权限）提交修改 52=只读，应被拦截不生效
        RolePermissionHelper.Apply(role, "52#1", null, true);
        role.Update();

        // 非受限模式会把 52 改成 Detail；受限模式应保持系统授予的 All，且 53 保留
        Assert.Equal(PermissionFlags.All, role.Get(52));
        Assert.Equal(PermissionFlags.Detail, role.Get(53));
    }

    [Fact]
    [DisplayName("RoundTrip_含额外权限位16_落库重读一致")]
    public void RoundTrip_ExtraFlags_Persisted()
    {
        var role = CreateRole();
        var id = role.ID;

        // 模拟前端「首页」菜单权限串：查看(1)+释放内存(16)
        RolePermissionHelper.Apply(role, "32#17", null, false);
        role.Update();

        Role.Meta.Session.ClearCache($"extra-{id}", true);
        var reloaded = Role.FindByID(id);
        Assert.NotNull(reloaded);
        Assert.Equal((PermissionFlags)17, reloaded!.Get(32));
        Assert.True(reloaded.Has(32, PermissionFlags.Detail));
        Assert.True(reloaded.Has(32, (PermissionFlags)16));
    }

    [Fact]
    [DisplayName("RoundTrip_权限串顺序_按资源ID升序")]
    public void RoundTrip_PermissionString_OrderedByResourceId()
    {
        var role = CreateRole();

        // 乱序设置，SavePermission 应按资源 ID 升序序列化
        role.Set(53, PermissionFlags.Detail);
        role.Set(52, PermissionFlags.All);
        role.Set(51, (PermissionFlags)16);
        role.Update();

        var idx52 = role.Permission.IndexOf("52#-1", StringComparison.Ordinal);
        var idx51 = role.Permission.IndexOf("51#16", StringComparison.Ordinal);
        var idx53 = role.Permission.IndexOf("53#1", StringComparison.Ordinal);
        Assert.True(idx51 >= 0 && idx52 > idx51 && idx53 > idx52, $"权限串应按资源ID升序: {role.Permission}");
    }
}
