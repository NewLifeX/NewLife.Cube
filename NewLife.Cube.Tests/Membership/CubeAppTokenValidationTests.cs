using System;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.Controllers;
using NewLife.Cube.Entity;
using NewLife.Cube.Services;
using NewLife.Web;
using Xunit;

namespace NewLife.Cube.Tests.Membership;

/// <summary>
/// 应用令牌在 Widget / Workbench / Automation 三控制器的验签门禁回归。
/// 背景：三处 ValidateToken 曾把「验签失败（ex 非空）」误判为放行，与 CubeController 的防伪造语义相反。
/// 期望（与 CubeController 一致）：验签失败必须拒绝；验签通过且应用启用才放行；应用密钥（非 JWT）照常放行。
/// </summary>
[Collection("TenantAuth")]
public class CubeAppTokenValidationTests
{
    private const String AppName = "oscAppTokenGuard";
    private const String AppSecret = "osc-app-token-guard-secret";
    private readonly TenantAuthFixture _fx;

    /// <summary>实例化</summary>
    /// <param name="fixture">多租户认证夹具（提供 SQLite 与固定 JWT 密钥）</param>
    public CubeAppTokenValidationTests(TenantAuthFixture fixture) => _fx = fixture;

    /// <summary>确保存在启用的测试应用；实体缓存需在插入后失效再查询</summary>
    private static App EnsureApp()
    {
        var app = App.FindByName(AppName);
        if (app == null)
        {
            app = new App { Name = AppName, Secret = AppSecret, Enable = true };
            app.Insert();
            App.Meta.Session.ClearCache("AppTokenGuard", true);
            app = App.FindByName(AppName);
        }

        Assert.NotNull(app);
        return app;
    }

    /// <summary>生成应用 JWT；OAuthController 同款用法，由“算法:密钥”直接签发</summary>
    /// <param name="subject">主题（应用名）</param>
    /// <param name="secret">签名密钥</param>
    /// <returns>JWT 字符串</returns>
    private static String EncodeToken(String subject, String secret)
    {
        var jwt = new JwtBuilder
        {
            Algorithm = "HS256",
            Secret = secret,
            Subject = subject,
            Expire = DateTime.Now.AddHours(1),
        };
        return jwt.Encode(null);
    }

    /// <summary>反射调用私有 ValidateToken，直接验证过滤器判定</summary>
    /// <param name="controller">控制器实例</param>
    /// <returns>是否放行</returns>
    private static Boolean Validate(Object controller)
    {
        var mi = controller.GetType().GetMethod("ValidateToken", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(mi);
        return (Boolean)mi!.Invoke(controller, null)!;
    }

    /// <summary>构造三个使用同一令牌服务的控制器</summary>
    private Object[] CreateControllers()
    {
        var tokenService = new TokenService();
        return
        [
            new WidgetController(tokenService),
            new WorkbenchController(tokenService),
            new AutomationController(tokenService),
        ];
    }

    /// <summary>为控制器挂载携带 X-Token 的独立 Http 上下文</summary>
    private void Attach(Object controller, String token)
    {
        var ctx = _fx.CreateContext();
        ctx.Request.Headers["X-Token"] = token;
        ((ControllerBase)controller).ControllerContext = new ControllerContext { HttpContext = ctx };
    }

    [Fact(DisplayName = "伪造签名应用 JWT：Widget/Workbench/Automation 必须拒绝")]
    public void Forged_App_Jwt_Rejected()
    {
        EnsureApp();

        var old = CubeSetting.Current.JwtSecret;
        CubeSetting.Current.JwtSecret = "HS256:CubeTenantReproTestSecret";
        try
        {
            var forged = EncodeToken(AppName, "Wrong-Not-The-Jwt-Secret");
            foreach (var controller in CreateControllers())
            {
                Attach(controller, forged);
                Assert.False(Validate(controller), $"{controller.GetType().Name} 放行了验签失败的 JWT");
            }
        }
        finally
        {
            CubeSetting.Current.JwtSecret = old;
        }
    }

    [Fact(DisplayName = "合法签名应用 JWT：Widget/Workbench/Automation 验证放行")]
    public void Valid_App_Jwt_Accepted()
    {
        EnsureApp();

        var old = CubeSetting.Current.JwtSecret;
        CubeSetting.Current.JwtSecret = "HS256:CubeTenantReproTestSecret";
        try
        {
            var valid = EncodeToken(AppName, "CubeTenantReproTestSecret");
            foreach (var controller in CreateControllers())
            {
                Attach(controller, valid);
                Assert.True(Validate(controller), $"{controller.GetType().Name} 拒绝了合法签名的应用 JWT");
            }
        }
        finally
        {
            CubeSetting.Current.JwtSecret = old;
        }
    }

    [Fact(DisplayName = "应用密钥（非 JWT）：Widget/Workbench/Automation 仍放行")]
    public void App_Secret_Accepted()
    {
        EnsureApp();

        foreach (var controller in CreateControllers())
        {
            Attach(controller, AppSecret);
            Assert.True(Validate(controller), $"{controller.GetType().Name} 拒绝了应用密钥");
        }
    }
}
