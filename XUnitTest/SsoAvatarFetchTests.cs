using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using NewLife.Cube;
using NewLife.Cube.Entity;
using NewLife.Cube.Services.Sso;
using NewLife.Security;
using NewLife.Web;
using XCode.Membership;
using Xunit;

namespace XUnitTest;

/// <summary>SSO 头像抓取流程单元测试。覆盖 FillAvatar/FetchAvatar 的下载、哈希校验与本地落盘</summary>
/// <remarks>
/// 通过本地 HttpListener 模拟 SSO 头像服务端，验证：
/// 1. FetchAvatar 能按 URL 下载头像到 AvatarPath 并按扩展名落盘
/// 2. FillAvatar 在 FetchAvatar 配置开启时触发异步下载
/// 3. 哈希校验失败时不会落盘（防脏数据）
/// </remarks>
public class SsoAvatarFetchTests : IDisposable
{
    private readonly HttpListener _server;
    private readonly Int32 _port;
    private String _tmpDir;
    private static Int32 _id;

    public SsoAvatarFetchTests()
    {
        // 启动本地图片服务，模拟 SSO 头像地址。随机端口，冲突时重试
        _server = new HttpListener();
        for (var i = 0; i < 5; i++)
        {
            _port = Rand.Next(20000, 60000);
            _server.Prefixes.Clear();
            _server.Prefixes.Add($"http://127.0.0.1:{_port}/");
            try
            {
                _server.Start();
                break;
            }
            catch (System.Net.HttpListenerException)
            {
                if (i == 4) throw;
            }
        }
        _ = Task.Run(ProcessRequest);

        _tmpDir = Path.Combine(Path.GetTempPath(), "cube-avatar-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tmpDir);
    }

    public void Dispose()
    {
        _server?.Stop();
        try
        {
            if (Directory.Exists(_tmpDir)) Directory.Delete(_tmpDir, true);
        }
        catch { }
    }

    /// <summary>响应头像请求：/avatar.png 返回 PNG；/avatar.svg 返回 SVG；/bad 返回错误页</summary>
    private async Task ProcessRequest()
    {
        while (_server.IsListening)
        {
            HttpListenerContext ctx;
            try { ctx = await _server.GetContextAsync(); }
            catch { break; }

            var path = ctx.Request.Url.AbsolutePath;
            try
            {
                if (path == "/avatar.png")
                {
                    var data = AvatarPng;
                    ctx.Response.StatusCode = 200;
                    ctx.Response.ContentType = "image/png";
                    ctx.Response.ContentLength64 = data.Length;
                    await ctx.Response.OutputStream.WriteAsync(data, 0, data.Length);
                }
                else if (path == "/avatar.svg")
                {
                    var data = Encoding.UTF8.GetBytes("<svg xmlns='http://www.w3.org/2000/svg' width='100' height='100'><text x='50' y='50'>A</text></svg>");
                    ctx.Response.StatusCode = 200;
                    ctx.Response.ContentType = "image/svg+xml";
                    ctx.Response.ContentLength64 = data.Length;
                    await ctx.Response.OutputStream.WriteAsync(data, 0, data.Length);
                }
                else if (path == "/bad")
                {
                    var data = Encoding.UTF8.GetBytes("<html>Bad Gateway</html>");
                    ctx.Response.StatusCode = 502;
                    ctx.Response.ContentType = "text/html";
                    ctx.Response.ContentLength64 = data.Length;
                    await ctx.Response.OutputStream.WriteAsync(data, 0, data.Length);
                }
                else
                {
                    ctx.Response.StatusCode = 404;
                }
            }
            catch { }
            finally
            {
                ctx.Response.Close();
            }
        }
    }

    /// <summary>内存 PNG 数据（1x1 像素，固定字节，MD5 可复现）</summary>
    private static Byte[] AvatarPng { get; } = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==");

    private static String PngMd5 { get; } = Md5Of(AvatarPng);

    private static String Md5Of(Byte[] data)
    {
        using var md5 = System.Security.Cryptography.MD5.Create();
        return Convert.ToHexString(md5.ComputeHash(data)).ToLower();
    }

    /// <summary>创建内存用户，无需数据库</summary>
    private static User CreateUser()
    {
        var user = new User { ID = ++_id, Name = $"avatar{_id}", DisplayName = $"测试{_id}" };
        return user;
    }

    /// <summary>构造带 FetchAvatar 配置的 OAuth 客户端</summary>
    private static OAuthClient CreateClient(String avatarUrl, Boolean fetchAvatar = true)
    {
        var client = new OAuthClient();
        client.Config = new OAuthConfig { FetchAvatar = fetchAvatar };
        client.Avatar = avatarUrl;
        return client;
    }

    /// <summary>暴露受保护的 FillAvatar 方法</summary>
    private class TestBindingService : UserBindingService
    {
        public void CallFillAvatar(OAuthClient client, User user2, NewLife.Model.IManageUser user, CubeSetting set)
            => FillAvatar(client, user2, user, set);
    }

    [Fact(DisplayName = "FetchAvatar 直接下载 PNG 到本地头像目录")]
    public async Task FetchAvatar_DownloadPng()
    {
        var user = CreateUser();
        var old = CubeSetting.Current.AvatarPath;
        CubeSetting.Current.AvatarPath = _tmpDir;
        try
        {
            var url = $"http://127.0.0.1:{_port}/avatar.png#md5${PngMd5}";
            var svc = new UserBindingService();

            var rs = await svc.FetchAvatar(user, url);

            var dest = Path.Combine(_tmpDir, user.ID + ".png");
            Assert.True(File.Exists(dest), $"头像文件未落盘: {dest}");
            Assert.True(rs, "FetchAvatar 应返回成功");
        }
        finally
        {
            CubeSetting.Current.AvatarPath = old;
        }
    }

    [Fact(DisplayName = "FetchAvatar 下载 SVG 时落盘为 .svg")]
    public async Task FetchAvatar_DownloadSvg()
    {
        var user = CreateUser();
        var old = CubeSetting.Current.AvatarPath;
        CubeSetting.Current.AvatarPath = _tmpDir;
        try
        {
            var svgData = Encoding.UTF8.GetBytes("<svg xmlns='http://www.w3.org/2000/svg' width='100' height='100'><text x='50' y='50'>A</text></svg>");
            var hash = Md5Of(svgData);
            var url = $"http://127.0.0.1:{_port}/avatar.svg#md5${hash}";
            var svc = new UserBindingService();

            await svc.FetchAvatar(user, url);

            Assert.True(File.Exists(Path.Combine(_tmpDir, user.ID + ".svg")), "SVG 应落盘为 .svg");
            Assert.False(File.Exists(Path.Combine(_tmpDir, user.ID + ".png")), "PNG 不应存在");
        }
        finally
        {
            CubeSetting.Current.AvatarPath = old;
        }
    }

    [Fact(DisplayName = "FetchAvatar 哈希不匹配时拒绝落盘")]
    public async Task FetchAvatar_HashMismatch()
    {
        var user = CreateUser();
        var old = CubeSetting.Current.AvatarPath;
        CubeSetting.Current.AvatarPath = _tmpDir;
        try
        {
            var url = $"http://127.0.0.1:{_port}/avatar.png#md5${new String('0', 32)}";
            var svc = new UserBindingService();

            var rs = await svc.FetchAvatar(user, url);

            Assert.False(File.Exists(Path.Combine(_tmpDir, user.ID + ".png")), "哈希不匹配时不应落盘");
            Assert.False(rs);
        }
        finally
        {
            CubeSetting.Current.AvatarPath = old;
        }
    }

    [Fact(DisplayName = "FillAvatar 开启抓取时设置本地头像地址")]
    public void FillAvatar_WithFetchConfig_SetsLocalPath()
    {
        var user = CreateUser();
        var old = CubeSetting.Current.AvatarPath;
        CubeSetting.Current.AvatarPath = _tmpDir;
        try
        {
            var avatarUrl = $"http://127.0.0.1:{_port}/avatar.png#md5${PngMd5}";
            var client = CreateClient(avatarUrl, true);
            var svc = new TestBindingService();

            svc.CallFillAvatar(client, user, user, CubeSetting.Current);

            // FetchAvatar 为异步触发，此处等待落盘
            var dest = Path.Combine(_tmpDir, user.ID + ".png");
            for (var i = 0; i < 50 && !File.Exists(dest); i++)
            {
                System.Threading.Thread.Sleep(100);
            }

            Assert.True(File.Exists(dest), $"FillAvatar 未触发头像下载: {dest}");
            Assert.StartsWith("/Sso/Avatar?id=", user.Avatar, StringComparison.Ordinal);
        }
        finally
        {
            CubeSetting.Current.AvatarPath = old;
        }
    }

    [Fact(DisplayName = "FillAvatar 关闭抓取时保存外网地址")]
    public void FillAvatar_NoFetchConfig_SavesRemoteUrl()
    {
        var user = CreateUser();
        var old = CubeSetting.Current.AvatarPath;
        CubeSetting.Current.AvatarPath = _tmpDir;
        try
        {
            var avatarUrl = $"http://127.0.0.1:{_port}/avatar.png";
            var client = CreateClient(avatarUrl, false);
            var svc = new TestBindingService();

            svc.CallFillAvatar(client, user, user, CubeSetting.Current);

            Assert.Equal(avatarUrl, user.Avatar);
            Assert.False(File.Exists(Path.Combine(_tmpDir, user.ID + ".png")), "未开启抓取时不应下载");
        }
        finally
        {
            CubeSetting.Current.AvatarPath = old;
        }
    }

    [Fact(DisplayName = "ResolveAvatarUrl 绝对地址原样返回")]
    public void ResolveAvatarUrl_Absolute_ReturnsAsIs()
    {
        var svc = new UserBindingService();
        var url = $"http://127.0.0.1:{_port}/avatar.png#md5${PngMd5}";

        var rs = svc.ResolveAvatarUrl("NewLife", url);

        Assert.Equal(url, rs);
    }

    [Fact(DisplayName = "ResolveAvatarUrl 空地址返回 null")]
    public void ResolveAvatarUrl_Empty_ReturnsNull()
    {
        var svc = new UserBindingService();

        Assert.Null(svc.ResolveAvatarUrl("NewLife", null));
        Assert.Null(svc.ResolveAvatarUrl("NewLife", ""));
    }

    [Fact(DisplayName = "FetchAvatar 支持相对地址按提供商解析后下载")]
    public async Task FetchAvatar_RelativeUrl_ResolvesAndDownloads()
    {
        var user = CreateUser();
        var old = CubeSetting.Current.AvatarPath;
        CubeSetting.Current.AvatarPath = _tmpDir;
        try
        {
            // 相对地址 + 无用户连接时无法解析，直接返回 false
            var svc = new UserBindingService();
            var rs = await svc.FetchAvatar(user, "/avatar.png");

            Assert.False(rs, "无用户连接时相对地址应解析失败返回 false");
        }
        finally
        {
            CubeSetting.Current.AvatarPath = old;
        }
    }

    [Fact(DisplayName = "TryFetchRemoteAvatar 从用户连接解析远程头像并触发下载")]
    public async Task TryFetchRemoteAvatar_ResolvesAndTriggers()
    {
        var user = CreateUser();
        var old = CubeSetting.Current.AvatarPath;
        CubeSetting.Current.AvatarPath = _tmpDir;
        try
        {
            // 先清理可能存在的测试数据（表不存在时忽略）
            try { UserConnect.Delete(UserConnect._.UserID == user.ID); } catch { }
            try { OAuthConfig.Delete(OAuthConfig._.Name == "NewLife"); } catch { }

            // 构造用户连接记录
            var uc = new UserConnect
            {
                UserID = user.ID,
                Provider = "NewLife",
                Avatar = $"/avatar.png#md5${PngMd5}",
                AccessToken = "",
                Enable = true,
                UpdateTime = DateTime.Now,
            };
            uc.Insert();

            // 构造提供商配置（FetchAvatar + Server）
            var cfg = new OAuthConfig
            {
                Name = "NewLife",
                Server = $"http://127.0.0.1:{_port}",
                FetchAvatar = true,
                Enable = true,
            };
            cfg.Insert();

            var svc = new UserBindingService();
            var remote = svc.TryFetchRemoteAvatar(user);

            Assert.NotNull(remote);
            Assert.StartsWith($"http://127.0.0.1:{_port}", remote, StringComparison.Ordinal);
            Assert.EndsWith($"/avatar.png#md5${PngMd5}", remote, StringComparison.Ordinal);

            // 等待异步下载落盘
            var dest = Path.Combine(_tmpDir, user.ID + ".png");
            for (var i = 0; i < 50 && !File.Exists(dest); i++)
            {
                await Task.Delay(100);
            }
            Assert.True(File.Exists(dest), $"TryFetchRemoteAvatar 未触发下载: {dest}");
        }
        finally
        {
            CubeSetting.Current.AvatarPath = old;
            try
            {
                UserConnect.Delete(UserConnect._.UserID == user.ID);
                OAuthConfig.Delete(OAuthConfig._.Name == "NewLife");
            }
            catch { }
        }
    }

    [Fact(DisplayName = "Fill 方法在 Items 非空时触发 FillAvatar 下载")]
    public async Task Fill_WithItems_TriggersAvatarDownload()
    {
        var user = CreateUser();
        var old = CubeSetting.Current.AvatarPath;
        CubeSetting.Current.AvatarPath = _tmpDir;
        try
        {
            // 模拟真实 SSO 场景：Avatar 为相对路径 + Server，Items 含完整用户信息
            var avatarUrl = $"/avatar.png#md5${PngMd5}";
            var client = CreateClient(avatarUrl, true);
            client.Server = $"http://127.0.0.1:{_port}";
            client.NickName = "测试";
            client.Items = new System.Collections.Generic.Dictionary<String, Object>(System.StringComparer.OrdinalIgnoreCase)
            {
                ["userid"] = "4",
                ["username"] = "Stone",
                ["nickname"] = "测试",
                ["avatar"] = avatarUrl,
                ["scope"] = "basic,UserInfo",
            };

            var svc = new TestBindingService();
            svc.CallFillAvatar(client, user, user, CubeSetting.Current);

            var dest = Path.Combine(_tmpDir, user.ID + ".png");
            for (var i = 0; i < 50 && !File.Exists(dest); i++)
            {
                await Task.Delay(100);
            }

            Assert.True(File.Exists(dest), $"Fill 未触发头像下载: {dest}");
            Assert.StartsWith("/Sso/Avatar?id=", user.Avatar, StringComparison.Ordinal);
        }
        finally
        {
            CubeSetting.Current.AvatarPath = old;
        }
    }
}
