using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NewLife;
using NewLife.Cube;
using NewLife.Cube.Areas.Admin.Models;
using NewLife.Cube.Entity;
using NewLife.Cube.Services;
using NewLife.Cube.Services.Sso;
using NewLife.Cube.Tests.Membership;
using NewLife.Model;
using NewLife.Security;
using NewLife.Web;
using XCode.DataAccessLayer;
using XCode.Membership;
using Xunit;
// 消除 NewLife.Cube.Services.TokenService 与 Sso.TokenService 的类型歧义，本文件用的是 SSO 令牌服务
using TokenService = NewLife.Cube.Services.Sso.TokenService;

namespace NewLife.Cube.Tests.Services;

/// <summary>
/// AUTH-11 外部认证集成测试。覆盖未配置地址直接失败、外部接口响应解析（成功/失败/畸形），
/// 以及按外部结果创建或更新本地用户（新用户角色映射、存量用户仅更新非空字段、禁用用户拒绝登录）。
/// 外部接口以本机回环 HttpListener 模拟，不访问外网。
/// </summary>
[Collection("TenantAuth")]
public class ExternalAuthTests
{
    private readonly TenantAuthFixture _fx;

    /// <summary>实例化</summary>
    /// <param name="fixture">多租户认证夹具。复用其 SQLite 库（User/Role 表）</param>
    public ExternalAuthTests(TenantAuthFixture fixture) => _fx = fixture;

    #region 辅助
    private static Int32 GetFreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();

        return port;
    }

    /// <summary>启动一次性回环接口，返回外部认证地址</summary>
    private static (String url, Task serve, HttpListener listener) StartServer(String json, Int32 statusCode = 200)
    {
        var port = GetFreePort();
        var listener = new HttpListener();
        listener.Prefixes.Add($"http://127.0.0.1:{port}/");
        listener.Start();

        var serve = Task.Run(async () =>
        {
            var ctx = await listener.GetContextAsync();
            var body = Encoding.UTF8.GetBytes(json);
            ctx.Response.StatusCode = statusCode;
            ctx.Response.ContentType = "application/json";
            ctx.Response.ContentLength64 = body.Length;
            await ctx.Response.OutputStream.WriteAsync(body);
            ctx.Response.Close();
        });

        return ($"http://127.0.0.1:{port}/auth", serve, listener);
    }

    private static ExternalAuthModel Call(String json)
    {
        var (url, serve, listener) = StartServer(json);
        try
        {
            return ExternalAuthHelper.Validate("input_user", "pwd", url);
        }
        finally
        {
            // 请求未到达时回环接口仍在等待，Stop 会中止 GetContextAsync
            listener.Stop();
            try { serve.GetAwaiter().GetResult(); } catch { }
        }
    }

    private static String UniqueName() => "ext_" + Guid.NewGuid().ToString("N")[..8];

    #endregion

    #region 外部验证接口
    [Fact(DisplayName = "外部验证_未配置地址直接返回空")]
    public void Validate_NoUrl_ReturnsNull()
    {
        Assert.Null(ExternalAuthHelper.Validate("u", "p", null));
        Assert.Null(ExternalAuthHelper.Validate("u", "p", ""));
    }

    [Fact(DisplayName = "外部验证_解析成功响应并支持字段别名")]
    public void Validate_ParsesSuccessResponse()
    {
        var json = """{"code":0,"data":{"username":"zhangsan","displayName":"张三","email":"z@example.com","phone":"13800138000","avatar":"/avatar.png","roleName":"普通用户","code":"E001"}}""";

        var model = Call(json);

        Assert.NotNull(model);
        Assert.Equal("zhangsan", model.UserName);
        Assert.Equal("张三", model.DisplayName);
        Assert.Equal("z@example.com", model.Mail);
        Assert.Equal("13800138000", model.Mobile);
        Assert.Equal("/avatar.png", model.Avatar);
        Assert.Equal("普通用户", model.RoleName);
        Assert.Equal("E001", model.Code);
    }

    [Fact(DisplayName = "外部验证_字符串状态码与根级字段同样识别")]
    public void Validate_ParsesStringCode_AndRootFields()
    {
        var byStringCode = Call("""{"code":"0","data":{"name":"wangwu"}}""");
        Assert.NotNull(byStringCode);
        Assert.Equal("wangwu", byStringCode.UserName);

        // username 缺失时回退使用登录输入的用户名
        var fallback = Call("""{"success":true,"successValue":"ignored"}""");
        Assert.NotNull(fallback);
        Assert.Equal("input_user", fallback.UserName);
    }

    [Fact(DisplayName = "外部验证_业务失败与畸形响应返回空")]
    public void Validate_FailureResponses_ReturnNull()
    {
        Assert.Null(Call("""{"code":1,"message":"账号或密码错误"}"""));
        Assert.Null(Call("""{"success":false}"""));
        Assert.Null(Call("not-a-json"));
    }
    #endregion

    #region 本地用户创建与更新
    [Fact(DisplayName = "外部认证_新用户按角色名建档并写入资料")]
    public void CreateOrUpdateUser_CreatesWithRole()
    {
        var role = Role.FindByName(CubeSetting.Current.DefaultRole) ?? _fx.LegacyUser.Role;
        var name = UniqueName();
        var ext = new ExternalAuthModel
        {
            UserName = name,
            DisplayName = "外部用户",
            Mail = $"{name}@example.com",
            Mobile = "13512345678",
            Avatar = "/ext.png",
            RoleName = role?.Name,
            Code = "E100",
        };

        var user = ExternalAuthHelper.CreateOrUpdateUser(ext, "127.0.0.1", CubeSetting.Current);

        Assert.NotNull(user);
        Assert.True(user.Id > 0);
        Assert.Equal(name, user.Name);
        Assert.Equal("外部用户", user.DisplayName);
        Assert.Equal($"{name}@example.com", user.Mail);
        Assert.Equal("13512345678", user.Mobile);
        Assert.Equal("E100", user.Code);
        Assert.True(user.Enable);
        if (role != null) Assert.Equal(role.Id, user.RoleId);
    }

    [Fact(DisplayName = "外部认证_存量用户仅更新非空字段")]
    public void CreateOrUpdateUser_UpdatesOnlyProvidedFields()
    {
        var name = UniqueName();
        var created = ExternalAuthHelper.CreateOrUpdateUser(new ExternalAuthModel
        {
            UserName = name,
            DisplayName = "旧昵称",
            Mail = $"{name}@example.com",
        }, "127.0.0.1", CubeSetting.Current);

        var updated = ExternalAuthHelper.CreateOrUpdateUser(new ExternalAuthModel
        {
            UserName = name,
            DisplayName = "新昵称",
            Mail = null,
            Mobile = "13600001111",
        }, "127.0.0.1", CubeSetting.Current);

        Assert.Equal(created.Id, updated.Id);
        Assert.Equal("新昵称", updated.DisplayName);
        Assert.Equal($"{name}@example.com", updated.Mail);
        Assert.Equal("13600001111", updated.Mobile);
    }

    [Fact(DisplayName = "外部认证_禁用用户拒绝登录")]
    public void CreateOrUpdateUser_DisabledUser_Throws()
    {
        var name = UniqueName();
        var user = ExternalAuthHelper.CreateOrUpdateUser(new ExternalAuthModel { UserName = name }, "127.0.0.1", CubeSetting.Current);
        user.Enable = false;
        user.Update();

        var ex = Assert.Throws<InvalidOperationException>(() => ExternalAuthHelper.CreateOrUpdateUser(new ExternalAuthModel { UserName = name }, "127.0.0.1", CubeSetting.Current));
        Assert.Contains(name, ex.Message);

        user.Enable = true;
        user.Update();
    }
    #endregion

    #region IExternalAuthService DI 扩展点
    [Fact(DisplayName = "DI扩展点_默认实现委托静态助手并遵循ExternalAuthUrl语义")]
    public void DefaultExternalAuthService_DelegatesToStaticHelper()
    {
        var svc = new DefaultExternalAuthService();

        // 未配置地址：与静态助手一致返回 null
        var url = CubeSetting.Current.ExternalAuthUrl;
        CubeSetting.Current.ExternalAuthUrl = null;
        try
        {
            Assert.Null(svc.Validate("u", "p"));
        }
        finally { CubeSetting.Current.ExternalAuthUrl = url; }

        // 配置地址：与静态助手同协议解析
        var json = """{"code":0,"data":{"username":"zhangsan","displayName":"张三"}}""";
        var (server, serve, listener) = StartServer(json);
        CubeSetting.Current.ExternalAuthUrl = server;
        try
        {
            var model = svc.Validate("input_user", "pwd");
            Assert.NotNull(model);
            Assert.Equal("zhangsan", model.UserName);
            Assert.Equal("张三", model.DisplayName);
        }
        finally
        {
            CubeSetting.Current.ExternalAuthUrl = url;
            listener.Stop();
            try { serve.GetAwaiter().GetResult(); } catch { }
        }
    }

    [Fact(DisplayName = "DI扩展点_容器无实现解析为空_有实现可解析")]
    public void DI_Resolution_ReturnsRegisteredImplementationOnly()
    {
        var sc = new ServiceCollection();
        var empty = sc.BuildServiceProvider();
        Assert.Null(ModelExtension.GetService<IExternalAuthService>(empty));

        var fake = new FakeExternalAuthService { OnValidate = (u, p) => new ExternalAuthModel { UserName = u } };
        sc.AddSingleton<IExternalAuthService>(fake);
        using var sp = sc.BuildServiceProvider();
        Assert.Same(fake, ModelExtension.GetService<IExternalAuthService>(sp));
    }

    [Fact(DisplayName = "DI扩展点_自定义实现验证成功时短路，不回退静态ExternalAuthUrl")]
    public void LoginByPassword_CustomExternalAuthService_ShortCircuits()
    {
        // 静态回退指向一个必失败的外部接口：若 DI 实现未短路而走了静态路径，登录必然失败
        var (server, serve, listener) = StartServer("""{"code":1,"message":"静态路径不应被调用"}""");

        var oldUrl = CubeSetting.Current.ExternalAuthUrl;
        var oldTenant = CubeSetting.Current.EnableTenant;
        CubeSetting.Current.ExternalAuthUrl = server;
        CubeSetting.Current.EnableTenant = false;
        try
        {
            var name = UniqueName();
            String capturedUser = null, capturedPass = null;
            var fake = new FakeExternalAuthService
            {
                OnValidate = (u, p) => { capturedUser = u; capturedPass = p; return new ExternalAuthModel { UserName = name, DisplayName = "外部用户", Code = "E001" }; },
            };

            var sc = new ServiceCollection();
            sc.AddSingleton<IExternalAuthService>(fake);
            using var sp = sc.BuildServiceProvider();

            // 与 TenantAuthFixture 相同的依赖装配，仅 serviceProvider 换成携带自定义实现的容器
            var userService = new UserService(new PasswordService(), new TestCacheProvider(), null!, null!, null!, new TenantContextService(), sp);

            var loginCtx = _fx.CreateContext();
            var model = new LoginModel { Username = name, Password = "plain_pwd" };
            var result = userService.Login(model, loginCtx);

            // DI 实现成功 → 短路，登录成功且本地按 Name=工号 建档
            Assert.True(result.IsSuccess);
            Assert.Equal(name, capturedUser);
            Assert.Equal("plain_pwd", capturedPass);
            var user = User.FindByName(name);
            Assert.NotNull(user);
            Assert.Equal("外部用户", user.DisplayName);
            Assert.Equal("E001", user.Code);
        }
        finally
        {
            CubeSetting.Current.ExternalAuthUrl = oldUrl;
            CubeSetting.Current.EnableTenant = oldTenant;
            listener.Stop();
            try { serve.GetAwaiter().GetResult(); } catch { }
        }
    }

    [Fact(DisplayName = "DI扩展点_无实现时回退静态ExternalAuthUrl原逻辑")]
    public void LoginByPassword_NoImplementation_FallsBackToStaticHelper()
    {
        // 不注册 IExternalAuthService：外部验证走静态 ExternalAuthUrl（本用例指向成功响应）
        var json = """{"code":0,"data":{"username":"fallback_user","displayName":"静态用户"}}""";
        var (server, serve, listener) = StartServer(json);

        var oldUrl = CubeSetting.Current.ExternalAuthUrl;
        var oldTenant = CubeSetting.Current.EnableTenant;
        CubeSetting.Current.ExternalAuthUrl = server;
        CubeSetting.Current.EnableTenant = false;
        try
        {
            var sc = new ServiceCollection();
            using var sp = sc.BuildServiceProvider();

            var userService = new UserService(new PasswordService(), new TestCacheProvider(), null!, null!, null!, new TenantContextService(), sp);

            var loginCtx = _fx.CreateContext();
            var model = new LoginModel { Username = "fallback_user", Password = "plain_pwd" };
            var result = userService.Login(model, loginCtx);

            // 静态路径成功 → 登录成功且建档（原行为保持）
            Assert.True(result.IsSuccess);
            var user = User.FindByName("fallback_user");
            Assert.NotNull(user);
            Assert.Equal("静态用户", user.DisplayName);
        }
        finally
        {
            CubeSetting.Current.ExternalAuthUrl = oldUrl;
            CubeSetting.Current.EnableTenant = oldTenant;
            listener.Stop();
            try { serve.GetAwaiter().GetResult(); } catch { }
        }
    }
    #endregion

    #region TokenService 密码式授权 md5#/RSA 分支的外部认证回退
    /// <summary>装配令牌服务与启用密码式授权的测试应用。Secret 供 RSA 分支取私钥，null 表示不设置</summary>
    private TokenService CreateTokenService(IExternalAuthService externalAuth, out App app, String appSecret = null)
    {
        CubeSetting.Current.EnableOAuthServer = true;
        CubeSetting.Current.AllowLogin = true;
        CubeSetting.Current.EnableTenant = false;

        app = new App { Name = UniqueName(), DisplayName = "密码式授权测试应用", Enable = true, Secret = appSecret, Scopes = "password" };
        app.Insert();

        // 注入 DSA 私钥的 TokenProvider，避免 GetProvider 落到 Parameter 表/密钥文件依赖
        return new TokenService
        {
            Provider = _fx.Provider,
            AppService = new OAuthAppService { Setting = CubeSetting.Current, TokenProvider = new TokenProvider { Key = DSAHelper.GenerateKey()[0] } },
            ExternalAuth = externalAuth,
        };
    }

    /// <summary>绕过 App.Secret 的 50 字符实体校验，经 DAL 直写超长私钥并重建实体缓存</summary>
    private static void WriteAppSecret(String appName, String secret)
    {
        var dal = DAL.Create("Cube");
        // 实体名 App 对应表名 OAuthApp，经 Meta 取真实表名，不硬编码
        var table = App.Meta.Table.DataTable.TableName;
        dal.Execute($"Update {table} Set Secret='{secret.Replace("'", "''")}' Where Name='{appName.Replace("'", "''")}'");
        App.Meta.Cache.Clear("rsa-secret-test", true);
    }

    [Fact(DisplayName = "密码式授权_md5#分支_本地无此用户时经DI外部认证建档登录")]
    public void AccessTokenByPassword_Md5_UnknownUser_FallsBackToExternal()
    {
        // 本地不存在该用户名，md5# 分支 User.Login 抛 EntityException，应捕获后落入外部认证回退
        String capturedUser = null, capturedPass = null;
        var fake = new FakeExternalAuthService
        {
            OnValidate = (u, p) => { capturedUser = u; capturedPass = p; return new ExternalAuthModel { UserName = u, DisplayName = "MD5外部用户", Code = "M001" }; },
        };
        var svc = CreateTokenService(fake, out var app);
        var name = UniqueName();
        var md5Hex = "0192023a7bbd73250516f069df18b500"; // 任意32位hex即可，本地无此用户不会走到密码比对

        var token = svc.GetAccessTokenByPassword(app.Name, name, "md5#" + md5Hex, "127.0.0.1");

        // 外部验证成功 → 短路建档并颁发令牌；密码原样传递（含 md5# 前缀，由自定义实现自行解释）
        Assert.False(token.AccessToken.IsNullOrEmpty());
        Assert.Equal(name, capturedUser);
        Assert.Equal("md5#" + md5Hex, capturedPass);
        var user = User.FindByName(name);
        Assert.NotNull(user);
        Assert.Equal("MD5外部用户", user.DisplayName);
        Assert.Equal("M001", user.Code);
    }

    [Fact(DisplayName = "密码式授权_RSA分支_本地无此用户时以解密明文经DI外部认证建档登录")]
    public void AccessTokenByPassword_Rsa_UnknownUser_FallsBackToExternal_WithPlainText()
    {
        var keys = RSAHelper.GenerateKey(1024);
        var prvKey = keys[0]; // GenerateKey 返回 [含私钥XML, 公钥XML]
        var pubKey = keys[1];

        String capturedUser = null, capturedPass = null;
        var fake = new FakeExternalAuthService
        {
            OnValidate = (u, p) => { capturedUser = u; capturedPass = p; return new ExternalAuthModel { UserName = u, DisplayName = "RSA外部用户", Code = "R001" }; },
        };
        var svc = CreateTokenService(fake, out var app, appSecret: null);
        var name = UniqueName();
        var plain = "Rsa@12345";

        // App.Secret 列限长 50 字符，存不下完整 RSA 私钥；Insert 后经 DAL 直改并清实体缓存，模拟宿主已存好私钥
        WriteAppSecret(app.Name, prvKey);

        // 客户端传 $rsa$ + base64(RSA加密后的密文)，服务端解密得到明文后走本地验证
        var cipher = RSAHelper.Encrypt(plain.GetBytes(), pubKey).ToBase64();
        var token = svc.GetAccessTokenByPassword(app.Name, name, $"$rsa${cipher}", "127.0.0.1");

        // 外部认证收到的必须是解密后的明文，而非 $rsa$ 原始串
        Assert.False(token.AccessToken.IsNullOrEmpty());
        Assert.Equal(name, capturedUser);
        Assert.Equal(plain, capturedPass);
        var user = User.FindByName(name);
        Assert.NotNull(user);
        Assert.Equal("RSA外部用户", user.DisplayName);
    }

    [Fact(DisplayName = "密码式授权_md5#分支_本地密码错误不落入外部认证")]
    public void AccessTokenByPassword_Md5_WrongPassword_DoesNotFallback()
    {
        // 本地已有用户 tenant01（密码 Test@12345）。md5 不匹配抛 InvalidOperationException：
        // 外部认证协议需要明文密码，md5 场景回退无意义，因此不应调用外部实现
        var called = false;
        var fake = new FakeExternalAuthService
        {
            OnValidate = (u, p) => { called = true; return new ExternalAuthModel { UserName = u, DisplayName = "不应生效" }; },
        };
        var svc = CreateTokenService(fake, out var app);

        Assert.Throws<InvalidOperationException>(() =>
            svc.GetAccessTokenByPassword(app.Name, _fx.BoundUser.Name, "md5#00000000000000000000000000000000", "127.0.0.1"));

        Assert.False(called);
    }

    [Fact(DisplayName = "密码式授权_RSA分支_本地密码错误经外部回退失败后抛认证失败")]
    public void AccessTokenByPassword_Rsa_WrongPassword_FallsBackThenFails()
    {
        // 本地已有用户 tenant01，RSA 解密出的明文密码错误 → EntityException 捕获 → 外部回退
        // 外部实现拒绝（返回 null）且未配置 ExternalAuthUrl → 最终抛"authentication failed"
        var called = false;
        var fake = new FakeExternalAuthService
        {
            OnValidate = (u, p) => { called = true; return null; },
        };
        var keys = RSAHelper.GenerateKey(1024);
        var svc = CreateTokenService(fake, out var app, appSecret: null);

        // App.Secret 列限长 50 字符，存不下完整 RSA 私钥；Insert 后经 DAL 直改并清实体缓存
        WriteAppSecret(app.Name, keys[0]);

        var cipher = RSAHelper.Encrypt("WrongPwd".GetBytes(), keys[1]).ToBase64();

        var ex = Assert.Throws<XException>(() =>
            svc.GetAccessTokenByPassword(app.Name, _fx.BoundUser.Name, $"$rsa${cipher}", "127.0.0.1"));

        // 确认确实走了外部回退（而非 EntityException 直接冒泡），且最终以认证失败收场
        Assert.True(called);
        Assert.Contains(_fx.BoundUser.Name, ex.Message);
    }
    #endregion
}

/// <summary>测试用外部认证服务桩。记录收到的一次验证请求，行为由 OnValidate 决定</summary>
internal sealed class FakeExternalAuthService : IExternalAuthService
{
    /// <summary>验证行为。返回非 null 模拟验证成功</summary>
    public Func<String, String, ExternalAuthModel> OnValidate { get; set; }

    /// <summary>验证用户名密码</summary>
    public ExternalAuthModel Validate(String username, String password) => OnValidate?.Invoke(username, password);
}

