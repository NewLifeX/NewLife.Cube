using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Script.Serialization;
using System.Xml.Serialization;
using NewLife;
using NewLife.Data;
using NewLife.Log;
using NewLife.Model;
using NewLife.Reflection;
using NewLife.Security;
using NewLife.Threading;
using NewLife.Web;
using XCode;
using XCode.Cache;
using XCode.Configuration;
using XCode.DataAccessLayer;
using XCode.Membership;

namespace NewLife.Cube.Entity
{
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

            // 在新插入数据或者修改了指定字段时进行修正
            // 处理当前已登录用户信息，可以由UserModule过滤器代劳
            /*var user = ManageProvider.User;
            if (user != null)
            {
                if (isNew && !Dirtys[nameof(CreateUserID)]) CreateUserID = user.ID;
                if (!Dirtys[nameof(UpdateUserID)]) UpdateUserID = user.ID;
            }*/
            //if (isNew && !Dirtys[nameof(CreateTime)]) CreateTime = DateTime.Now;
            //if (!Dirtys[nameof(UpdateTime)]) UpdateTime = DateTime.Now;
            //if (isNew && !Dirtys[nameof(CreateIP)]) CreateIP = ManageProvider.UserHost;
            //if (!Dirtys[nameof(UpdateIP)]) UpdateIP = ManageProvider.UserHost;

            // 检查唯一索引
            // CheckExist(isNew, nameof(Name));
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
            };
            entity.Insert();

            Add("QQ", "QQ", "/Content/images/logo/QQ.png");
            Add("Github", "Github", "/Content/images/logo/Github.png");
            Add("Baidu", "百度", "/Content/images/logo/Baidu.png");
            Add("Ding", "钉钉", "/Content/images/logo/Ding.png");
            Add("QyWeiXin", "企业微信", "/Content/images/logo/QyWeixin.png");
            Add("Weixin", "微信", "/Content/images/logo/Weixin.png");
            Add("Microsoft", "微软", "/Content/images/logo/Microsoft.png");
            //Add("Weibo", "微博", "/Content/images/logo/Weibo.png");
            //Add("Taobao", "淘宝", "/Content/images/logo/Taobao.png");
            //Add("Alipay", "支付宝", "/Content/images/logo/Alipay.png");

            if (XTrace.Debug) XTrace.WriteLine("完成初始化OAuthConfig[OAuth配置]数据！");
        }
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
        /// <returns></returns>
        public static OAuthConfig Add(String name, String nickName, String logo)
        {
            var entity = new OAuthConfig
            {
                Name = name,
                NickName = nickName,
                Logo = logo,
            };

            entity.Insert();

            return entity;
        }

        /// <summary>获取全部有效设置</summary>
        /// <returns></returns>
        public static IList<OAuthConfig> GetValids() => FindAllWithCache().Where(e => e.Enable).ToList();
        #endregion
    }
}