using NewLife.Cube.Entity;
using NewLife.Web;
using XCode.Membership;
using IManageUser = NewLife.Model.IManageUser;

namespace NewLife.Cube.Services.Sso;

/// <summary>用户绑定服务接口</summary>
public interface IUserBindingService
{
    /// <summary>获取用户连接信息</summary>
    /// <param name="client">OAuth 客户端对象，包含 OpenID、UserName 等字段</param>
    /// <returns>匹配到的现有 UserConnect 记录，找不到则返回新建未保存的对象</returns>
    UserConnect GetConnect(OAuthClient client);

    /// <summary>绑定用户，用户未有效绑定或需要强制绑定时</summary>
    /// <param name="uc">用户连接记录</param>
    /// <param name="client">OAuth 客户端</param>
    /// <param name="userId">前端传入的意向绑定用户ID，0 表示未指定</param>
    /// <returns>匹配成功的本地用户；未匹配时返回 null</returns>
    IManageUser OnBind(UserConnect uc, OAuthClient client, Int32 userId);

    /// <summary>登录后绑定当前用户</summary>
    /// <param name="oauthId">OAuthLog.Id，登录流程中生成的授权日志主键</param>
    /// <returns>匹配并更新的 OAuthLog；找不到或当前未登录时返回 null</returns>
    OAuthLog BindAfterLogin(Int64 oauthId);

    /// <summary>填充用户信息（角色、部门、租户、头像等）</summary>
    /// <param name="client"></param>
    /// <param name="user"></param>
    /// <param name="context">服务提供者</param>
    void Fill(OAuthClient client, IManageUser user, IServiceProvider context = null);

    /// <summary>注销</summary>
    void Logout();

    /// <summary>获取密钥</summary>
    /// <param name="name"></param>
    /// <returns></returns>
    String GetKey(String name);

    /// <summary>抓取远程头像</summary>
    /// <param name="user"></param>
    /// <param name="url"></param>
    /// <param name="accessToken"></param>
    /// <param name="fallbackUrl">降级下载地址。内网地址下载失败时使用此地址重试</param>
    /// <returns></returns>
    Task<Boolean> FetchAvatar(IManageUser user, String url = null, String accessToken = null, String fallbackUrl = null);
}
