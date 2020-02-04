using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Web.Script.Serialization;
using System.Xml.Serialization;
using NewLife.Data;
using NewLife.Serialization;
using NewLife.Web;
using XCode;
using XCode.Cache;
using XCode.Membership;

namespace NewLife.Cube.Entity
{
    /// <summary>用户链接。第三方绑定</summary>
    [ModelCheckMode(ModelCheckModes.CheckTableWhenFirstUse)]
    public partial class UserConnect : Entity<UserConnect>
    {
        #region 对象操作
        static UserConnect()
        {
            // 累加字段
            //Meta.Factory.AdditionalFields.Add(__.Logins);

            // 过滤器 UserModule、TimeModule、IPModule
            Meta.Modules.Add<UserModule>();
            Meta.Modules.Add<TimeModule>();
            Meta.Modules.Add<IPModule>();
        }

        /// <summary>验证数据，通过抛出异常的方式提示验证失败。</summary>
        /// <param name="isNew">是否插入</param>
        public override void Valid(Boolean isNew)
        {
            // 如果没有脏数据，则不需要进行任何处理
            if (!HasDirty) return;

            // 备注字段超长截取
            var len = _.Remark.Length;
            if (!Remark.IsNullOrEmpty() && len > 0 && Remark.Length > len) Remark = Remark.Substring(0, len);
        }
        #endregion

        #region 扩展属性
        /// <summary>用户</summary>
        [XmlIgnore, ScriptIgnore, IgnoreDataMember]
        public UserX User => Extends.Get(nameof(User), k => UserX.FindByID(UserID));

        /// <summary>用户</summary>
        [Map(__.UserID)]
        public String UserName => User?.ToString();
        #endregion

        #region 扩展查询
        /// <summary>根据编号查找</summary>
        /// <param name="id">编号</param>
        /// <returns>实体对象</returns>
        public static UserConnect FindByID(Int32 id)
        {
            if (id <= 0) return null;

            // 实体缓存
            if (Meta.Session.Count < 1000) return Meta.Cache.Find(e => e.ID == id);

            // 单对象缓存
            //return Meta.SingleCache[id];

            return Find(_.ID == id);
        }

        /// <summary>根据提供商、用户查找</summary>
        /// <param name="provider">提供商</param>
        /// <param name="openid">身份标识</param>
        /// <returns>实体对象</returns>
        public static UserConnect FindByProviderAndOpenID(String provider, String openid)
        {
            //// 实体缓存
            //if (Meta.Session.Count < 1000) return Meta.Cache.Find(e => e.Provider == provider && e.OpenID == openid);

            return Find(_.Provider == provider & _.OpenID == openid);
        }

        /// <summary>根据用户查找</summary>
        /// <param name="userid">用户</param>
        /// <returns>实体列表</returns>
        public static IList<UserConnect> FindAllByUserID(Int32 userid)
        {
            //// 实体缓存
            //if (Meta.Session.Count < 1000) return Meta.Cache.FindAll(e => e.UserID == userid);

            return FindAll(_.UserID == userid);
        }
        #endregion

        #region 高级查询
        /// <summary>高级查询</summary>
        /// <param name="provider"></param>
        /// <param name="userid"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="key"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        public static IList<UserConnect> Search(String provider, Int32 userid, DateTime start, DateTime end, String key, PageParameter p)
        {
            var exp = new WhereExpression();

            if (!provider.IsNullOrEmpty()) exp &= _.Provider == provider;
            if (userid > 0) exp &= _.UserID == userid;
            exp &= _.UpdateTime.Between(start, end);
            if (!key.IsNullOrEmpty()) exp &= _.NickName.Contains(key) | _.OpenID.Contains(key);

            return FindAll(exp, p);
        }
        #endregion

        #region 业务操作
        /// <summary>填充用户</summary>
        /// <param name="client"></param>
        public virtual void Fill(OAuthClient client)
        {
            var uc = this;
            if (!client.NickName.IsNullOrEmpty()) uc.NickName = client.NickName;
            if (!client.Avatar.IsNullOrEmpty()) uc.Avatar = client.Avatar;

            uc.LinkID = client.UserID;
            //ub.OpenID = client.OpenID;
            uc.AccessToken = client.AccessToken;
            uc.RefreshToken = client.RefreshToken;
            uc.Expire = client.Expire;

            if (client.Items != null) uc.Remark = client.Items.ToJson();
        }

        static FieldCache<UserConnect> ProviderCache = new FieldCache<UserConnect>(_.Provider);

        /// <summary>获取所有提供商名称</summary>
        /// <returns></returns>
        public static IDictionary<String, String> FindAllProviderName() => ProviderCache.FindAllName();
        #endregion
    }
}