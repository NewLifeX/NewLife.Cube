using NewLife.Cube.Entity;
using XCode.Membership;
using IManageUser = NewLife.Model.IManageUser;

namespace NewLife.Cube.Services.Sso;

/// <summary>SSO 服务端服务接口</summary>
public interface ISsoServerService
{
    /// <summary>验证用户身份，开始授权流程</summary>
    /// <param name="client_id">应用标识</param>
    /// <param name="redirect_uri">回调地址</param>
    /// <param name="response_type">响应类型</param>
    /// <param name="scope">授权域</param>
    /// <param name="state">状态数据</param>
    /// <param name="ip">IP地址</param>
    /// <returns>AppLog.Id 作为授权Key</returns>
    String Authorize(String client_id, String redirect_uri, String response_type, String scope, String state, String ip);

    /// <summary>根据验证结果获取跳转回子系统的Url</summary>
    /// <param name="key">授权Key（AppLog.Id）</param>
    /// <param name="user">已登录用户</param>
    /// <returns>跳转URL</returns>
    String GetResult(String key, IManageUser user);
}
