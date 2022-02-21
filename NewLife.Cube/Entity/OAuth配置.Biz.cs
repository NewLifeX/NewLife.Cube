using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using NewLife.Cube.Web.Models;
using NewLife.Data;
using NewLife.Log;
using NewLife.Security;
using NewLife.Serialization;
using XCode;
using XCode.Membership;

namespace NewLife.Cube.Entity
{
    /// <summary>OAuth2.0授权类型</summary>
    public enum GrantTypes
    {
        /// <summary>
        /// 授权码
        /// </summary>
        AuthorizationCode = 0,

        /// <summary>
        /// 隐藏式
        /// </summary>
        Implicit,

        /// <summary>
        /// 密码式
        /// </summary>
        Password,

        /// <summary>
        /// 客户端凭证
        /// </summary>
        ClientCredentials,
    }

    /// <summary>OAuth配置。需要连接的OAuth认证方</summary>
    public partial class OAuthConfig : Entity<OAuthConfig>
    {
        #region 对象操作
        static OAuthConfig()
        {
            // 累加字段，生成 Update xx Set Count=Count+1234 Where xxx
            //var df = Meta.Factory.AdditionalFields;
            //df.Add(nameof(CreateUserID));

            // 过滤器 UserModule、TimeModule、IPModule
            Meta.Modules.Add<UserModule>();
            Meta.Modules.Add<TimeModule>();
            Meta.Modules.Add<IPModule>();

            // 单对象缓存
            var sc = Meta.SingleCache;
            sc.FindSlaveKeyMethod = k => Find(_.Name == k);
            sc.GetSlaveKeyMethod = e => e.Name;
        }

        /// <summary>验证并修补数据，通过抛出异常的方式提示验证失败。</summary>
        /// <param name="isNew">是否插入</param>
        public override void Valid(Boolean isNew)
        {
            // 如果没有脏数据，则不需要进行任何处理
            if (!HasDirty) return;

            // 这里验证参数范围，建议抛出参数异常，指定参数名，前端用户界面可以捕获参数异常并聚焦到对应的参数输入框
            if (Name.IsNullOrEmpty()) throw new ArgumentNullException(nameof(Name), "名称不能为空！");

            // 建议先调用基类方法，基类方法会做一些统一处理
            base.Valid(isNew);

            // 不要写AuthUrl默认地址，否则会影响微信登录
            if (Name.EqualIgnoreCase("NewLife"))
            {
                if (AuthUrl.IsNullOrEmpty()) AuthUrl = "authorize?response_type={response_type}&client_id={key}&redirect_uri={redirect}&state={state}&scope={scope}";
                if (AccessUrl.IsNullOrEmpty()) AccessUrl = "access_token?grant_type=authorization_code&client_id={key}&client_secret={secret}&code={code}&state={state}&redirect_uri={redirect}";
            }

            if (FieldMap.IsNullOrEmpty()) FieldMap = new OAuthFieldMap().ToJson(true);
        }

        /// <summary>首次连接数据库时初始化数据，仅用于实体类重载，用户不应该调用该方法</summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected override void InitData()
        {
            // InitData一般用于当数据表没有数据时添加一些默认数据，该实体类的任何第一次数据库操作都会触发该方法，默认异步调用
            if (Meta.Session.Count > 0) return;

            if (XTrace.Debug) XTrace.WriteLine("开始初始化OAuthConfig[OAuth配置]数据……");

            //Add("NewLife", "新生命用户中心", "/Content/images/logo/NewLife.png");
            var entity = new OAuthConfig
            {
                Name = "NewLife",
                NickName = "新生命用户中心",
                Logo = "/Content/images/logo/NewLife.png",

                Server = "https://sso.newlifex.com/sso",
                AppId = "NewLife.Cube",
                Secret = Rand.NextString(16),

                Enable = true,
                Debug = true,
                Visible = true,
                AutoRegister = true,
            };
            entity.Insert();

            Add("QQ", "QQ", "/Content/images/logo/QQ.png");
            Add("Github", "Github", "/Content/images/logo/Github.png");
            Add("Baidu", "百度", "/Content/images/logo/Baidu.png");
            Add("Ding", "钉钉", "/Content/images/logo/Ding.png", "snsapi_qrlogin扫码登录，snsapi_auth钉钉内免登，snsapi_login密码登录");
            Add("QyWeiXin", "企业微信", "/Content/images/logo/QyWeiXin.png");
            //Add("Weixin", "微信公众号", "/Content/images/logo/Weixin.png", "snsapi_base静默登录，snsapi_userinfo需要用户关注后授权");
            var cfg = new OAuthConfig
            {
                Name = "Weixin",
                NickName = "微信公众号",
                Logo = "/Content/images/logo/Weixin.png",
                Remark = "snsapi_base静默登录，snsapi_userinfo需要用户关注后授权",

                Visible = false,
                AutoRegister = true,
            };
            cfg.Insert();

            Add("OpenWeixin", "微信开放平台", "/Content/images/logo/Weixin.png", "snsapi_login用于扫码登录");
            Add("Microsoft", "微软", "/Content/images/logo/Microsoft.png");
            //Add("Weibo", "微博", "/Content/images/logo/Weibo.png");
            //Add("Taobao", "淘宝", "/Content/images/logo/Taobao.png");
            //Add("Alipay", "支付宝", "/Content/images/logo/Alipay.png");

            if (XTrace.Debug) XTrace.WriteLine("完成初始化OAuthConfig[OAuth配置]数据！");
        }

        /// <summary>已重载。显示友好名称</summary>
        /// <returns></returns>
        public override String ToString() => !NickName.IsNullOrEmpty() ? NickName : Name;
        #endregion

        #region 扩展属性
        #endregion

        #region 扩展查询
        /// <summary>根据编号查找</summary>
        /// <param name="id">编号</param>
        /// <returns>实体对象</returns>
        public static OAuthConfig FindByID(Int32 id)
        {
            if (id <= 0) return null;

            // 实体缓存
            if (Meta.Session.Count < 1000) return Meta.Cache.Find(e => e.ID == id);

            // 单对象缓存
            return Meta.SingleCache[id];

            //return Find(_.ID == id);
        }

        /// <summary>根据名称查找</summary>
        /// <param name="name">名称</param>
        /// <returns>实体对象</returns>
        public static OAuthConfig FindByName(String name)
        {
            // 实体缓存
            if (Meta.Session.Count < 1000) return Meta.Cache.Find(e => e.Name.EqualIgnoreCase(name));

            // 单对象缓存
            //return Meta.SingleCache.GetItemWithSlaveKey(name) as OAuthConfig;

            return Find(_.Name == name);
        }
        #endregion

        #region 高级查询
        /// <summary>高级查询</summary>
        /// <param name="name">名称。AppID</param>
        /// <param name="start">更新时间开始</param>
        /// <param name="end">更新时间结束</param>
        /// <param name="key">关键字</param>
        /// <param name="page">分页参数信息。可携带统计和数据权限扩展查询等信息</param>
        /// <returns>实体列表</returns>
        public static IList<OAuthConfig> Search(String name, DateTime start, DateTime end, String key, PageParameter page)
        {
            var exp = new WhereExpression();

            if (!name.IsNullOrEmpty()) exp &= _.Name == name;
            exp &= _.UpdateTime.Between(start, end);
            if (!key.IsNullOrEmpty()) exp &= _.Server.Contains(key) | _.AccessServer.Contains(key) | _.AppId.Contains(key) | _.Secret.Contains(key) | _.Scope.Contains(key) | _.AppUrl.Contains(key) | _.CreateIP.Contains(key) | _.UpdateIP.Contains(key) | _.Remark.Contains(key);

            return FindAll(exp, page);
        }
        #endregion

        #region 业务操作
        /// <summary>添加配置</summary>
        /// <param name="name"></param>
        /// <param name="nickName"></param>
        /// <param name="logo"></param>
        /// <param name="remark"></param>
        /// <returns></returns>
        public static OAuthConfig Add(String name, String nickName, String logo, String remark = null)
        {
            var entity = new OAuthConfig
            {
                Name = name,
                NickName = nickName,
                Logo = logo,
                Visible = true,
                AutoRegister = true,
                Remark = remark,
            };

            entity.Insert();

            return entity;
        }

        /// <summary>获取全部有效设置</summary>
        /// <param name="grantType">授权类型</param>
        /// <returns></returns>
        public static IList<OAuthConfig> GetValids(GrantTypes grantType) => FindAllWithCache().Where(e => e.Enable && e.GrantType == grantType).OrderByDescending(e => e.Sort).ThenByDescending(e => e.ID).ToList();

        /// <summary>获取全部有效且可见设置</summary>
        /// <returns></returns>
        public static IList<OAuthConfig> GetVisibles() => FindAllWithCache().Where(e => e.Enable && e.Visible).OrderByDescending(e => e.Sort).ThenByDescending(e => e.ID).ToList();
        #endregion
    }
}