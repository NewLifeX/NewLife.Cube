using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.Entity;
using NewLife.Cube.Services;
using NewLife.Remoting;
using NewLife.Web;

namespace NewLife.Cube;

/// <summary>应用接口控制器基类。继承该类可实现各种基于令牌的业务接口</summary>
[ApiController]
[Route("[controller]")]
public abstract class AppControllerBase : BaseController
{
    /// <summary>应用</summary>
    public App App { get; set; }

    /// <summary>令牌对象</summary>
    public JwtBuilder Jwt { get; set; }

    private readonly TokenService _tokenService;

    /// <summary>实例化</summary>
    /// <param name="tokenService"></param>
    public AppControllerBase(TokenService tokenService) => _tokenService = tokenService;

    #region 令牌验证
    /// <summary>验证令牌，并获取应用</summary>
    /// <param name="token"></param>
    /// <returns></returns>
    protected override Boolean OnAuthorize(String token)
    {
        var set = CubeSetting.Current;
        var (jwt, app) = _tokenService.DecodeToken(token, set.JwtSecret);
        App = app;
        Jwt = jwt;

        return app != null;
    }

    /// <summary>输出错误日志</summary>
    /// <param name="action"></param>
    /// <param name="message"></param>
    protected override void OnWriteError(String action, String message) => WriteLog(action, false, message);
    #endregion

    #region 辅助
    /// <summary>写日志</summary>
    /// <param name="action"></param>
    /// <param name="success"></param>
    /// <param name="message"></param>
    protected virtual void WriteLog(String action, Boolean success, String message) => (App ?? new()).WriteLog(action, success, message, UserHost, Jwt.Id);
    #endregion
}