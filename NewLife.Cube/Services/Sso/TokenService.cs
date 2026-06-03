using System.Web;
using NewLife.Cube.Entity;
using NewLife.Log;
using NewLife.Security;
using NewLife.Serialization;
using NewLife.Web;
using XCode.Membership;
using ILog = NewLife.Log.ILog;
using IManageUser = NewLife.Model.IManageUser;

namespace NewLife.Cube.Services.Sso;

/// <summary>令牌服务实现</summary>
public class TokenService : ITokenService
{
    #region 属性
    /// <summary>应用验证服务</summary>
    public IOAuthAppService AppService { get; set; }

    /// <summary>用户管理提供者</summary>
    public IManageProvider Provider { get; set; }

    /// <summary>性能追踪器</summary>
    public ITracer Tracer { get; set; }

    /// <summary>日志</summary>
    public ILog Log { get; set; }

    /// <summary>令牌定制扩展点</summary>
    public IEnumerable<ITokenCustomizer> TokenCustomizers { get; set; }
    #endregion

    #region 构造
    /// <summary>实例化</summary>
    public TokenService()
    {
        Provider = ManageProvider.Provider;
    }
    #endregion

    #region 令牌创建与解码
    /// <summary>创建令牌</summary>
    /// <param name="app"></param>
    /// <param name="name"></param>
    /// <param name="payload"></param>
    /// <param name="refreshName"></param>
    /// <returns></returns>
    public virtual TokenModel CreateToken(App app, String name, Object payload, String refreshName)
    {
        var prv = AppService.GetProvider();

        // 计算有效期，优先应用指定有效期，再使用全局有效期
        var expire = 0;
        if (app != null) expire = app.TokenExpire;

        var set = CubeSetting.Current;
        if (expire <= 0) expire = set.TokenExpire;
        var exp = DateTime.Now.AddSeconds(expire);

        // 颁发JWT令牌，优先应用密钥HS256，同时也是子应用请求sso的密钥。再使用全局密钥
        var jwt = new JwtBuilder
        {
            Algorithm = "HS256",
            Secret = app.Secret,

            Subject = name,
            Expire = exp,
            //Issuer = Environment.MachineName,
            Audience = app.Name,
        };
        if (jwt.Secret.IsNullOrEmpty())
        {
            var ss = set.JwtSecret.Split(':');
            jwt.Algorithm = ss[0];
            jwt.Secret = ss[1];
        }

        // 建立令牌
        var token = new TokenModel
        {
            AccessToken = jwt.Encode(payload),
            RefreshToken = prv.Encode(refreshName, exp),
            ExpireIn = expire
        };

        // 调用令牌定制扩展点
        if (TokenCustomizers != null)
        {
            foreach (var customizer in TokenCustomizers)
            {
                customizer.CustomizeAsync(token, jwt, app, null).ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }

        return token;
    }

    /// <summary>根据Code获取令牌</summary>
    /// <param name="code"></param>
    /// <returns></returns>
    public virtual TokenModel GetToken(String code)
    {
        var log = AppLog.FindById(code.ToLong());
        if (log == null) throw new ArgumentOutOfRangeException(nameof(code), "Code无效！");
        if (log.CreateTime.AddMinutes(5) < DateTime.Now) throw new ArgumentOutOfRangeException(nameof(code), "Code已过期！");

        WriteLog("Token appid={0} code={1} token={2} {3}", log.AppName, code, log.AccessToken, log.CreateUser);

        log.Action = nameof(GetToken);
        log.Update();

        var expire = 0;
        if (log.App != null) expire = log.App.TokenExpire;

        var set = CubeSetting.Current;
        if (expire <= 0) expire = set.TokenExpire;

        return new TokenModel
        {
            AccessToken = log.AccessToken,
            RefreshToken = log.RefreshToken,
            ExpireIn = expire
        };
    }

    /// <summary>解码令牌</summary>
    /// <param name="token"></param>
    /// <returns></returns>
    public virtual String Decode(String token)
    {
        // 区分访问令牌和内部刷新令牌
        var ts = token.Split('.');
        if (ts.Length == 3)
        {
            var secret = "";

            // 从头部找到颁发者，拿它的密钥
            var header = JsonParser.Decode(ts[1].ToBase64().ToStr());
            if (header.TryGetValue("aud", out var str))
            {
                var app = App.FindByName(str as String);
                secret = app?.Secret;
            }

            // 从配置加载密钥
            var set = CubeSetting.Current;
            var ss = set.JwtSecret.Split(':');

            var jwt = new JwtBuilder
            {
                Algorithm = ss[0],
                Secret = ss[1],
            };
            if (!secret.IsNullOrEmpty())
            {
                jwt.Algorithm = "HS256";
                jwt.Secret = secret;
            }

            if (!jwt.TryDecode(token, out var msg))
            {
                XTrace.WriteLine("令牌无效：{0}, token={1}", msg, token);
                throw new Exception(msg);
            }

            return jwt.Subject;
        }
        else
        {
            var prv = AppService.GetProvider();

            var rs = prv.TryDecode(token, out var name, out var expire);
            if (!rs || name.IsNullOrEmpty()) throw new Exception("非法访问令牌");
            if (expire < DateTime.Now) throw new Exception("令牌已过期");

            return name;
        }
    }
    #endregion

    #region 令牌颁发
    /// <summary>授权码方式获取访问令牌</summary>
    /// <param name="client_id"></param>
    /// <param name="client_secret"></param>
    /// <param name="code"></param>
    /// <param name="ip"></param>
    /// <returns></returns>
    public virtual TokenModel GetAccessToken(String client_id, String client_secret, String code, String ip)
    {
        using var span = Tracer?.NewSpan(nameof(GetAccessToken), client_id);
        try
        {
            AppService.Auth(client_id, client_secret + "", ip);

            var token = GetToken(code);
            token.Scope = "basic,UserInfo";

            return token;
        }
        catch (Exception ex)
        {
            span?.SetError(ex, new { client_id, client_secret, code, ip });
            throw;
        }
    }

    /// <summary>密码式获取令牌</summary>
    /// <param name="client_id">应用标识</param>
    /// <param name="username">用户名</param>
    /// <param name="password">密码。支持md5密码，以md5#开头</param>
    /// <param name="ip"></param>
    /// <returns></returns>
    public virtual TokenModel GetAccessTokenByPassword(String client_id, String username, String password, String ip)
    {
        var log = new AppLog
        {
            Action = "Password",
            Success = true,

            ClientId = client_id,
            ResponseType = "password",

            TraceId = DefaultSpan.Current?.TraceId,
            CreateIP = ip,
            CreateTime = DateTime.Now,
        };

        using var span = Tracer?.NewSpan(nameof(GetAccessTokenByPassword), username);
        try
        {
            var app = AppService.Auth(client_id, null, ip);
            log.AppId = app.Id;

            // 验证应用能力
            var scopes = app.Scopes?.Split(",");
            if (scopes == null || !"password".EqualIgnoreCase(scopes)) throw new InvalidOperationException($"应用[{app}]没有使用password密码凭证的能力！");

            IManageUser user = null;
            if (password.StartsWithIgnoreCase("md5#"))
            {
                var pass = password["md5#".Length..];
                user = User.Login(username, u =>
                {
                    if (!u.Password.IsNullOrEmpty() && !u.Password.EqualIgnoreCase(pass))
                        throw new InvalidOperationException($"密码不正确！");
                });
            }
            else if (password.StartsWithIgnoreCase("$rsa$"))
            {
                var ss = password.Split('$');
                var key = ""; // RSA密钥将在SsoClientService中管理，这里从SecurityKey获取
                var pass = ss[ss.Length - 1];
                pass = RSAHelper.Decrypt(pass.ToBase64(), key).ToStr();

                if (Provider is ManageProvider prv)
                    user = prv.LoginCore(username, pass);
                else
                    user = User.Login(username, pass, false);
            }
            else
            {
                if (Provider is ManageProvider prv)
                    user = prv.LoginCore(username, password);
                else
                    user = User.Login(username, password, false);
            }
            if (user == null)
            {
                // 本地验证失败，尝试外部验证服务
                var extAuthUrl = CubeSetting.Current.ExternalAuthUrl;
                if (!extAuthUrl.IsNullOrEmpty())
                {
                    var extUser = ExternalAuthHelper.Validate(username, password, extAuthUrl);
                    if (extUser != null)
                        user = ExternalAuthHelper.CreateOrUpdateUser(extUser, ip, CubeSetting.Current);
                }
                if (user == null) throw new XException("用户{0}验证失败", username);
            }

            var token = CreateToken(app, user.Name, null, $"{client_id}#{user.Name}");

            log.AccessToken = token.AccessToken;
            log.RefreshToken = token.RefreshToken;

            log.CreateUser = user.Name;
            log.Scope = token.Scope;

            return token;
        }
        catch (Exception ex)
        {
            log.Success = false;
            log.Remark = ex.GetTrue()?.Message;

            span?.SetError(ex, new { client_id, username, ip });

            throw;
        }
        finally
        {
            log.Insert();
        }
    }

    /// <summary>凭证式获取令牌</summary>
    /// <param name="client_id">应用标识</param>
    /// <param name="client_secret">密钥</param>
    /// <param name="username">用户名。可以是设备编码等唯一使用者标识</param>
    /// <param name="ip"></param>
    /// <returns></returns>
    public virtual TokenModel GetAccessTokenByClientCredentials(String client_id, String client_secret, String username, String ip)
    {
        var log = new AppLog
        {
            Action = "ClientCredentials",
            Success = true,

            ClientId = client_id,
            ResponseType = "client_credentials",

            TraceId = DefaultSpan.Current?.TraceId,
            CreateIP = ip,
            CreateTime = DateTime.Now,
        };

        using var span = Tracer?.NewSpan(nameof(GetAccessTokenByClientCredentials), username);
        try
        {
            var app = App.FindByName(client_id);
            if (app != null) log.AppId = app.Id;

            app = AppService.Auth(client_id, client_secret + "", ip);
            log.AppId = app.Id;

            // 验证应用能力
            var scopes = app.Scopes?.Split(",");
            if (scopes == null || !"client_credentials".EqualIgnoreCase(scopes)) throw new InvalidOperationException($"应用[{app}]没有使用client_credentials客户端凭证的能力！");

            var code = !username.IsNullOrEmpty() ? username : ("_" + Rand.NextString(7));
            var token = CreateToken(app, code, null, $"{client_id}#{code}");

            log.AccessToken = token.AccessToken;
            log.RefreshToken = token.RefreshToken;

            log.CreateUser = code;
            log.Scope = token.Scope;

            return token;
        }
        catch (Exception ex)
        {
            log.Success = false;
            log.Remark = ex.GetTrue()?.Message;

            span?.SetError(ex, new { client_id, client_secret, username, ip });

            throw;
        }
        finally
        {
            log.Insert();
        }
    }

    /// <summary>刷新令牌</summary>
    /// <param name="client_id">应用标识</param>
    /// <param name="refresh_token">刷新令牌</param>
    /// <param name="ip">IP地址</param>
    /// <returns></returns>
    public virtual TokenModel RefreshToken(String client_id, String refresh_token, String ip)
    {
        var log = new AppLog
        {
            Action = "RefreshToken",
            Success = true,

            ClientId = client_id,
            ResponseType = "refresh_token",

            TraceId = DefaultSpan.Current?.TraceId,
            CreateIP = ip,
            CreateTime = DateTime.Now,
        };

        using var span = Tracer?.NewSpan(nameof(RefreshToken), refresh_token);
        try
        {
            var app = App.FindByName(client_id);
            if (app != null) log.AppId = app.Id;

            app = AppService.Auth(client_id, null, ip);
            log.AppId = app.Id;

            var name = Decode(refresh_token);
            var ss = name.Split("#");
            if (ss.Length != 2 || ss[0] != client_id) throw new Exception("非法令牌");

            // 使用者标识保持不变
            var code = ss[1];
            var token = CreateToken(app, code, null, $"{client_id}#{code}");

            log.AccessToken = token.AccessToken;
            log.RefreshToken = token.RefreshToken;

            log.CreateUser = code;
            log.Scope = token.Scope;

            return token;
        }
        catch (Exception ex)
        {
            log.Success = false;
            log.Remark = ex.GetTrue()?.Message;

            span?.SetError(ex, new { client_id, refresh_token, ip });

            throw;
        }
        finally
        {
            log.Insert();
        }
    }
    #endregion

    #region 用户信息
    /// <summary>获取用户</summary>
    /// <param name="username"></param>
    /// <returns></returns>
    public virtual IManageUser GetUser(String username)
    {
        var user = Provider?.FindByName(username);
        // 两级单点登录可能因缓存造成查不到用户
        user ??= User.FindForLogin(username);

        return user;
    }

    /// <summary>获取用户信息</summary>
    /// <param name="token"></param>
    /// <param name="user"></param>
    /// <returns></returns>
    public virtual Object GetUserInfo(String token, IManageUser user)
    {
        // 返回用户资源，可作为子系统数据权限
        var res = Parameter.FindAllByUserID(user.ID, "Resources");
        var dic = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in res.Where(e => e.Enable))
        {
            dic[item.Name] = item.Value;
        }

        var online = UserOnline.FindAllByUserID(user.ID).FirstOrDefault(e => !e.DeviceId.IsNullOrEmpty());

        // 获取用户当前租户信息
        var tenantId = 0;
        String tenantCode = null;
        String tenantName = null;
        if (user is User user2)
        {
            // 从 TenantUser 查找用户的租户关系
            var tenantUser = TenantUser.FindAllByUserId(user2.ID).FirstOrDefault(e => e.Enable);
            if (tenantUser != null)
            {
                tenantId = tenantUser.TenantId;
                var tenant = Tenant.FindById(tenantId);
                if (tenant != null)
                {
                    tenantCode = tenant.Code;
                    tenantName = tenant.Name;
                }
            }

            // 头像URL构建优先级：
            // 1. 本地有头像文件 → /Sso/Avatar?id=xxx#md5$hash（附哈希供下游增量校验）
            // 2. 本地无文件 + user.Avatar 是外部 HTTP URL → 透传原始 URL
            // 3. 其他 → /Sso/Avatar?id=xxx（SVG 虚拟头像降级）
            var avatarUrl = "/Sso/Avatar?id=" + user2.ID;
            var (localAvatarFile, _) = global::NewLife.Cube.Services.SvgAvatarService.FindAvatarFile(CubeSetting.Current.AvatarPath, user2.ID);
            if (!localAvatarFile.IsNullOrEmpty())
            {
                var hash = localAvatarFile.AsFile().MD5().ToHex();
                DefaultSpan.Current?.AppendTag($"avatarFile={localAvatarFile} md5={hash}");
                avatarUrl += $"#md5${hash}";
            }
            else if (!user2.Avatar.IsNullOrEmpty() && user2.Avatar.StartsWithIgnoreCase("http://", "https://"))
            {
                avatarUrl = user2.Avatar;
            }

            return new
            {
                userid = user.ID,
                username = user.Name,
                nickname = user.NickName,
                sex = user2.Sex,
                mail = user2.Mail,
                mobile = user2.Mobile,
                code = user2.Code,
                roleid = user2.RoleID,
                rolename = user2.RoleName,
                roleids = user2.RoleIds,
                rolenames = user2.Roles.Skip(1).Join(",", e => e + ""),
                departmentCode = user2.Department?.Code,
                departmentName = user2.Department?.Name,
                tenantid = tenantId,
                tenantcode = tenantCode,
                tenantname = tenantName,
                areaid = user2.AreaId,
                deviceid = online?.DeviceId,
                avatar = avatarUrl,
                birthday = user2.Birthday.ToString("yyyy-MM-dd", ""),
                detail = user2.Remark,
                resources = dic,
            };
        }
        else
        {
            return new
            {
                userid = user.ID,
                username = user.Name,
                nickname = user.NickName,
                resources = dic,
            };
        }
    }
    #endregion

    #region 日志
    /// <summary>写日志</summary>
    /// <param name="format"></param>
    /// <param name="args"></param>
    public void WriteLog(String format, params Object[] args) => Log?.Info(format, args);
    #endregion
}
