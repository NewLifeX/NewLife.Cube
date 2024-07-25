using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.Entity;
using NewLife.Cube.Models;
using NewLife.Cube.Services;
using NewLife.Remoting;
using NewLife.Web;

namespace NewLife.Cube;

/// <summary>OAuth服务。向应用提供验证服务</summary>
[Route("[controller]/[action]")]
public class OAuthController : ControllerBase
{
    private readonly TokenService _tokenService;

    /// <summary>实例化</summary>
    /// <param name="tokenService"></param>
    public OAuthController(TokenService tokenService) => _tokenService = tokenService;

    /// <summary>验证密码颁发令牌，或刷新令牌</summary>
    /// <param name="model"></param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    [ApiFilter]
    public TokenModel Token([FromBody] TokenInModel model)
    {
        var set = CubeSetting.Current;

        if (model.grant_type.IsNullOrEmpty()) model.grant_type = "password";

        var ip = HttpContext.GetUserHost();
        var clientId = model.ClientId;

        try
        {
            // 密码模式
            if (model.grant_type == "password")
            {
                var app = _tokenService.Authorize(model.UserName, model.Password, set.AutoRegister, ip);

                // 更新应用信息
                app.LastAuth = DateTime.Now;
                app.UpdateIP = ip;
                app.Update();

                var tokenModel = _tokenService.IssueToken(app.Name, set.JwtSecret, set.TokenExpire, clientId);

                app.WriteLog("Authorize", true, model.UserName, ip, clientId);

                return tokenModel;
            }
            // 刷新令牌
            else if (model.grant_type == "refresh_token")
            {
                var (jwt, ex) = _tokenService.DecodeTokenWithError(model.refresh_token, set.JwtSecret);

                // 验证应用
                var app = _tokenService.FindByName(jwt?.Subject);
                if (app == null || !app.Enable)
                    ex ??= new ApiException(403, $"无效应用[{jwt.Subject}]");

                if (clientId.IsNullOrEmpty()) clientId = jwt.Id;

                if (ex != null)
                {
                    app.WriteLog("RefreshToken", false, ex.ToString(), ip, clientId);
                    throw ex;
                }

                var tokenModel = _tokenService.IssueToken(app.Name, set.JwtSecret, set.TokenExpire, clientId);

                //app.WriteHistory("RefreshToken", true, model.refresh_token, ip, clientId);

                return tokenModel;
            }
            else
                throw new NotSupportedException($"未支持 grant_type={model.grant_type}");
        }
        catch (Exception ex)
        {
            var app = App.FindByName(model.UserName);
            app?.WriteLog("Authorize", false, ex.ToString(), ip, clientId);

            throw;
        }
    }

    /// <summary>根据令牌获取应用信息，同时也是验证令牌是否有效</summary>
    /// <param name="token"></param>
    /// <returns></returns>
    [ApiFilter]
    public Object Info(String token)
    {
        var set = CubeSetting.Current;
        var (_, app) = _tokenService.DecodeToken(token, set.JwtSecret);
        return new
        {
            app.Id,
            app.Name,
            app.DisplayName,
            app.Category,
        };
    }
}