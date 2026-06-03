using NewLife.Cube.Entity;
using NewLife.Web;
using XCode.Membership;
using IManageUser = NewLife.Model.IManageUser;

namespace NewLife.Cube.Services.Sso;

/// <summary>用户绑定服务接口</summary>
public interface IUserBindingService
{
    /// <summary>获取用户连接信息</summary>
    /// <param name="client"></param>
    /// <returns></returns>
    UserConnect GetConnect(OAuthClient client);

    /// <summary>绑定用户，用户未有效绑定或需要强制绑定时</summary>
    /// <param name="uc"></param>
    /// <param name="client"></param>
    /// <param name="userId"></param>
    /// <returns></returns>
    IManageUser OnBind(UserConnect uc, OAuthClient client, Int32 userId);

    /// <summary>登录后绑定当前用户</summary>
    /// <param name="oauthId"></param>
    /// <returns></returns>
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
    /// <returns></returns>
    Task<Boolean> FetchAvatar(IManageUser user, String url = null, String accessToken = null);
}
