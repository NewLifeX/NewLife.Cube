using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using NewLife.Cube;
using NewLife.Cube.Services;
using NewLife.Cube.Tests.Membership;
using XCode.Membership;
using Xunit;

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
        Assert.True(user.ID > 0);
        Assert.Equal(name, user.Name);
        Assert.Equal("外部用户", user.DisplayName);
        Assert.Equal($"{name}@example.com", user.Mail);
        Assert.Equal("13512345678", user.Mobile);
        Assert.Equal("E100", user.Code);
        Assert.True(user.Enable);
        if (role != null) Assert.Equal(role.ID, user.RoleID);
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

        Assert.Equal(created.ID, updated.ID);
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
}
