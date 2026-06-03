using NewLife.Cube.Entity;
using NewLife.Log;
using NewLife.Security;
using NewLife.Web;
using XCode.Membership;

namespace NewLife.Cube.Services.Sso;

/// <summary>OAuth 应用验证服务实现</summary>
public class OAuthAppService : IOAuthAppService
{
    #region 属性
    /// <summary>魔方设置</summary>
    public CubeSetting Setting { get; set; }

    /// <summary>令牌提供者</summary>
    public TokenProvider TokenProvider { get; set; } = new TokenProvider();
    #endregion

    #region 构造
    /// <summary>实例化</summary>
    public OAuthAppService() { }
    #endregion

    #region 方法
    private void Valid()
    {
        var set = Setting;
        if (set != null && !set.EnableOAuthServer) throw new XException("未启用OAuth服务");
    }

    /// <summary>验证应用</summary>
    /// <param name="client_id"></param>
    /// <param name="client_secret"></param>
    /// <param name="ip"></param>
    /// <returns></returns>
    public virtual App Auth(String client_id, String client_secret, String ip)
    {
        Valid();

        var app = App.FindByName(client_id);
        // 找不到应用时自动创建，但处于禁用状态
        if (app == null)
        {
            app = new App { Name = client_id };
            app.Insert();
        }

        if (!app.Enable) throw new XException("应用[{0}]不可用", client_id);
        if (app.Expired.Year > 2000 && app.Expired < DateTime.Now) throw new XException("应用[{0}]已过期", client_id);

        if (!ip.IsNullOrEmpty() && !app.ValidSource(ip)) throw new XException("来源地址不合法 {0}", ip);

        if (client_secret != null)
        {
            if (!app.Secret.IsNullOrEmpty() && !app.Secret.EqualIgnoreCase(client_secret)) throw new XException("应用密钥错误");
        }

        return app;
    }

    /// <summary>获取令牌提供者</summary>
    /// <returns></returns>
    public virtual TokenProvider GetProvider()
    {
        var provider = TokenProvider;
        provider ??= TokenProvider = new TokenProvider();
        if (provider.Key.IsNullOrEmpty())
        {
            // 从配置加载密钥
            var prv = Parameter.GetOrAdd(0, "Keys", "OAuth.prvkey");
            var pub = Parameter.GetOrAdd(0, "Keys", "OAuth.pubkey");

            if (prv.LongValue.IsNullOrEmpty())
            {
                // 从文件加载密钥
                var file = "..\\Keys\\OAuth.prvkey";
                provider.ReadKey(file, false);

                // 删除文件
                var file2 = file.GetBasePath();
                if (File.Exists(file2)) File.Delete(file2);
                file2 = Path.ChangeExtension(file, ".pubkey");
                file2 = file2.GetBasePath();
                if (File.Exists(file2)) File.Delete(file2);

                prv.LongValue = provider.Key;

                // 生成新密钥
                if (prv.LongValue.IsNullOrEmpty())
                {
                    var ss = DSAHelper.GenerateKey();
                    prv.LongValue = ss[0];
                    pub.LongValue = ss[1];
                }

                prv.Update();
                pub.Update();
            }

            provider.Key = prv.LongValue;
        }

        return provider;
    }
    #endregion
}
