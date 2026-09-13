using Microsoft.AspNetCore.Http;
using NewLife.Common;
using NewLife.Cube;
using XCode.Membership;
using Xunit;

namespace NewLife.Cube.Tests.Membership;

/// <summary>
/// 请求级租户解析测试。覆盖 TenantContextService 重构后的行为：
/// 1. 优先按当前请求解析租户（复用 ResolveTenant 单一入口）并缓存到 HttpContext.Items，同一请求只解析一次；
/// 2. 请求未声明任何租户标识时，回退到已建立的租户上下文（登录流程 ChooseTenant/SetTenant 设置）；
/// 3. 显式声明但无效的标识不参与回退，避免被陈旧上下文覆盖；
/// 4. 无请求上下文（后台任务）回退 AsyncLocal，行为与旧版一致。
/// 复用 <see cref="TenantAuthFixture"/> 的 SQLite 数据（t1/t2 租户、无绑定存量用户等），串行执行。
/// </summary>
[Collection("TenantAuth")]
public class TenantResolutionTests
{
    private readonly TenantAuthFixture _fx;

    public TenantResolutionTests(TenantAuthFixture fx) => _fx = fx;

    [Fact(DisplayName = "请求带 X-Tenant：按请求解析对应租户，不读写静态 AsyncLocal")]
    public void Expect_XTenant_Resolves_From_Request()
    {
        CubeSetting.Current.EnableTenant = true;
        TenantContext.Current = null!;
        try
        {
            var ctx = _fx.CreateContext();
            ctx.Request.Headers["X-Tenant"] = TenantAuthFixture.TenantCode;

            var svc = new TenantContextService(_fx.Accessor);

            Assert.Equal(_fx.Tenant1.Id, svc.TenantId);
            Assert.Equal(TenantMode.Tenant, svc.Mode);
            // 请求解析不写 AsyncLocal，静态上下文保持未设置
            Assert.Equal(0, TenantContext.CurrentId);
        }
        finally
        {
            CubeSetting.Current.EnableTenant = false;
            TenantContext.Current = null!;
        }
    }

    [Fact(DisplayName = "同一请求重复读取：Items 缓存命中，后续请求头变化不影响本次解析")]
    public void Expect_Resolution_Cached_In_Items()
    {
        CubeSetting.Current.EnableTenant = true;
        TenantContext.Current = null!;
        try
        {
            var ctx = _fx.CreateContext();
            ctx.Request.Headers["X-Tenant"] = TenantAuthFixture.TenantCode;

            var svc1 = new TenantContextService(_fx.Accessor);
            Assert.Equal(_fx.Tenant1.Id, svc1.TenantId);

            // 第一次解析已缓存；即使本次请求后续改变请求头，仍用缓存结果（同一请求只解析一次）
            ctx.Request.Headers["X-Tenant"] = TenantAuthFixture.OtherTenantCode;
            var svc2 = new TenantContextService(_fx.Accessor);
            Assert.Equal(_fx.Tenant1.Id, svc2.TenantId);
            Assert.True(ctx.Items.ContainsKey("Cube.TenantResolution"));
        }
        finally
        {
            CubeSetting.Current.EnableTenant = false;
            TenantContext.Current = null!;
        }
    }

    [Fact(DisplayName = "显式但无效的 X-Tenant：不触发 AsyncLocal 回退（避免被陈旧上下文覆盖）")]
    public void Expect_Invalid_XTenant_No_Fallback()
    {
        CubeSetting.Current.EnableTenant = true;
        TenantContext.Current = null!;
        try
        {
            var ctx = _fx.CreateContext();
            ctx.Request.Headers["X-Tenant"] = "t9"; // 不存在

            // 模拟静态 AsyncLocal 残留旧租户
            TenantContext.Current = new TenantContext { TenantId = _fx.Tenant1.Id };

            var svc = new TenantContextService(_fx.Accessor);

            Assert.Equal(0, svc.TenantId); // 显式无效 → 不回落旧租户
            Assert.Equal(TenantMode.None, svc.Mode);
        }
        finally
        {
            CubeSetting.Current.EnableTenant = false;
            TenantContext.Current = null!;
        }
    }

    [Fact(DisplayName = "请求未声明任何租户标识：回退到已建立的租户上下文（登录流程 ChooseTenant 后读取）")]
    public void Expect_NoIdentifier_FallsBack_To_Established()
    {
        CubeSetting.Current.EnableTenant = true;
        TenantContext.Current = null!;
        try
        {
            var ctx = _fx.CreateContext(); // 无任何请求头/Cookie
            TenantContext.Current = new TenantContext { TenantId = _fx.Tenant1.Id };

            var svc = new TenantContextService(_fx.Accessor);

            Assert.Equal(_fx.Tenant1.Id, svc.TenantId);
            Assert.Equal(TenantMode.Tenant, svc.Mode);
        }
        finally
        {
            CubeSetting.Current.EnableTenant = false;
            TenantContext.Current = null!;
        }
    }

    [Fact(DisplayName = "请求未声明标识且上下文为管理后台（0）：回退返回 0/AdminBackend")]
    public void Expect_NoIdentifier_AdminBackend_FallsBack()
    {
        CubeSetting.Current.EnableTenant = true;
        TenantContext.Current = null!;
        try
        {
            var ctx = _fx.CreateContext();
            TenantContext.Current = new TenantContext { TenantId = 0 };

            var svc = new TenantContextService(_fx.Accessor);

            Assert.Equal(0, svc.TenantId);
            Assert.Equal(TenantMode.AdminBackend, svc.Mode);
        }
        finally
        {
            CubeSetting.Current.EnableTenant = false;
            TenantContext.Current = null!;
        }
    }

    [Fact(DisplayName = "无请求上下文（后台任务）：回退 AsyncLocal，行为与旧版一致")]
    public void Expect_NoHttpContext_Uses_AsyncLocal()
    {
        TenantContext.Current = null!;
        try
        {
            var svc = new TenantContextService(); // 无 IHttpContextAccessor

            TenantContext.Current = new TenantContext { TenantId = _fx.Tenant1.Id };
            Assert.Equal(_fx.Tenant1.Id, svc.TenantId);
            Assert.Equal(TenantMode.Tenant, svc.Mode);

            TenantContext.Current = null!;
            Assert.Equal(0, svc.TenantId);
            Assert.Equal(TenantMode.None, svc.Mode);
        }
        finally
        {
            TenantContext.Current = null!;
        }
    }
}
