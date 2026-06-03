using System.Web;
using NewLife.Cube.Entity;
using NewLife.Log;
using XCode.Membership;
using ILog = NewLife.Log.ILog;
using IManageUser = NewLife.Model.IManageUser;

namespace NewLife.Cube.Services.Sso;

/// <summary>SSO 服务端服务实现</summary>
public class SsoServerService : ISsoServerService
{
    #region 属性
    /// <summary>应用验证服务</summary>
    public IOAuthAppService AppService { get; set; }

    /// <summary>令牌服务</summary>
    public ITokenService TokenService { get; set; }

    /// <summary>日志</summary>
    public ILog Log { get; set; }
    #endregion

    #region 构造
    /// <summary>实例化</summary>
    public SsoServerService() { }
    #endregion

    #region 方法
    /// <summary>验证用户身份</summary>
    /// <remarks>
    /// 子系统需要验证访问者身份时，引导用户跳转到这里。
    /// 用户登录完成后，得到一个独一无二的code，并跳转回去子系统。
    /// </remarks>
    /// <param name="client_id">应用标识</param>
    /// <param name="redirect_uri">回调地址</param>
    /// <param name="response_type">响应类型。默认code</param>
    /// <param name="scope">授权域</param>
    /// <param name="state">用户状态数据</param>
    /// <param name="ip">IP地址</param>
    /// <returns></returns>
    public virtual String Authorize(String client_id, String redirect_uri, String response_type, String scope, String state, String ip)
    {
        var log = new AppLog
        {
            Action = nameof(Authorize),
            Success = true,

            ClientId = client_id,
            RedirectUri = redirect_uri,
            ResponseType = response_type,
            Scope = scope,
            State = state,

            TraceId = DefaultSpan.Current?.TraceId,
            CreateIP = ip,
            CreateTime = DateTime.Now,
        };

        try
        {
            var app = App.FindByName(client_id);
            if (app != null) log.AppId = app.Id;

            app = AppService.Auth(client_id, null, ip);
            log.AppId = app.Id;

            if (!app.ValidCallback(redirect_uri)) throw new XException("回调地址不合法 {0}", redirect_uri);

            app.Auths++;
            app.LastAuth = DateTime.Now;
            app.SaveAsync(5_000);

            WriteLog("Authorize client_id={0} redirect_uri={1} response_type={2}", client_id, redirect_uri, response_type);
        }
        catch (Exception ex)
        {
            log.Success = false;
            log.Remark = ex.GetTrue()?.Message;

            throw;
        }
        finally
        {
            log.Insert();
        }

        return log.Id + "";
    }

    /// <summary>根据验证结果获取跳转回子系统的Url</summary>
    /// <param name="key"></param>
    /// <param name="user"></param>
    /// <returns></returns>
    public virtual String GetResult(String key, IManageUser user)
    {
        var log = AppLog.FindById(key.ToLong());
        if (log == null) throw new ArgumentOutOfRangeException(nameof(key), "操作超时，请重试！");

        var code = log.Id + "";

        var token = TokenService.CreateToken(log.App, user.Name, null, $"{log.App?.Name}#{user.Name}");

        log.AccessToken = token.AccessToken;
        log.RefreshToken = token.RefreshToken;
        log.CreateUser = user + "";
        log.Action = nameof(GetResult);

        WriteLog("Authorize appid={0} code={2} redirect_uri={1} {3}", log.AppName, log.RedirectUri, code, user);

        // 校验角色
        var ids = log.App?.RoleIds?.SplitAsInt();
        if (ids != null && ids.Length > 0 && user is XCode.Membership.User user2)
        {
            if (!user2.Roles.Any(r => ids.Contains(r.ID)))
            {
                log.Success = false;
                log.Remark = $"该应用[{log.AppName}]不支持用户所属角色登录！";
                log.Update();

                throw new InvalidOperationException(log.Remark);
            }
        }

        log.Update();

        var url = log.RedirectUri;

        switch ((log.ResponseType + "").ToLower())
        {
            case "token":
                if (url.Contains("?"))
                    url += "&";
                else
                    url += "?";
                if (!log.State.IsNullOrEmpty()) url += "state=" + HttpUtility.UrlEncode(log.State);
                url += "#token=" + HttpUtility.UrlEncode(log.AccessToken);
                break;
            case "code":
            default:
                if (url.Contains("?"))
                    url += "&";
                else
                    url += "?";
                url += "code=" + code;
                if (!log.State.IsNullOrEmpty()) url += "&state=" + HttpUtility.UrlEncode(log.State);
                break;
        }

        return url;
    }
    #endregion

    #region 日志
    /// <summary>写日志</summary>
    /// <param name="format"></param>
    /// <param name="args"></param>
    public void WriteLog(String format, params Object[] args) => Log?.Info(format, args);
    #endregion
}
