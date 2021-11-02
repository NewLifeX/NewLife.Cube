using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Script.Serialization;
using System.Xml.Serialization;
using NewLife;
using NewLife.Data;
using NewLife.Log;
using NewLife.Model;
using NewLife.Reflection;
using NewLife.Threading;
using NewLife.Web;
using XCode;
using XCode.Cache;
using XCode.Configuration;
using XCode.DataAccessLayer;
using XCode.Membership;
using XCode.Shards;

namespace NewLife.Cube.Entity
{
    /// <summary>用户在线</summary>
    public partial class UserOnline : Entity<UserOnline>
    {
        #region 对象操作
        static UserOnline()
        {
            // 累加字段，生成 Update xx Set Count=Count+1234 Where xxx
            var df = Meta.Factory.AdditionalFields;
            df.Add(__.Times);
            //df.Add(__.OnlineTime);

            var sc = Meta.SingleCache;
            if (sc.Expire < 20 * 60) sc.Expire = 20 * 60;
            sc.FindSlaveKeyMethod = k => Find(__.SessionID, k);
            sc.GetSlaveKeyMethod = e => e.SessionID;

            // 过滤器 UserModule、TimeModule、IPModule
            Meta.Modules.Add<TimeModule>();
            Meta.Modules.Add<IPModule>();
        }

        /// <summary>验证并修补数据，通过抛出异常的方式提示验证失败。</summary>
        /// <param name="isNew">是否插入</param>
        public override void Valid(Boolean isNew)
        {
            // 如果没有脏数据，则不需要进行任何处理
            if (!HasDirty) return;

            // 建议先调用基类方法，基类方法会做一些统一处理
            base.Valid(isNew);

            // 截取长度
            var len = _.Status.Length;
            if (len <= 0) len = 50;
            if (!Status.IsNullOrEmpty() && Status.Length > len) Status = Status.Substring(0, len);

            len = _.Page.Length;
            if (len <= 0) len = 50;
            if (!Page.IsNullOrEmpty() && Page.Length > len) Page = Page.Substring(0, len);
        }
        #endregion

        #region 扩展属性
        #endregion

        #region 扩展查询
        /// <summary>根据编号查找</summary>
        /// <param name="id">编号</param>
        /// <returns>实体对象</returns>
        public static UserOnline FindByID(Int32 id)
        {
            if (id <= 0) return null;

            // 实体缓存
            if (Meta.Session.Count < 1000) return Meta.Cache.Find(e => e.ID == id);

            // 单对象缓存
            return Meta.SingleCache[id];

            //return Find(_.ID == id);
        }

        /// <summary>根据用户查找</summary>
        /// <param name="userId">用户</param>
        /// <returns>实体列表</returns>
        public static IList<UserOnline> FindAllByUserID(Int32 userId)
        {
            // 实体缓存
            if (Meta.Session.Count < 1000) return Meta.Cache.FindAll(e => e.UserID == userId);

            return FindAll(_.UserID == userId);
        }

        /// <summary>根据会话查找</summary>
        /// <param name="sessionId">会话</param>
        /// <param name="cache">是否从缓存查询</param>
        /// <returns>实体列表</returns>
        public static UserOnline FindBySessionID(String sessionId, Boolean cache = true)
        {
            if (sessionId.IsNullOrEmpty()) return null;

            if (cache)
            {
                // 实体缓存
                if (Meta.Session.Count < 1000) return Meta.Cache.Find(e => e.SessionID.EqualIgnoreCase(sessionId));

                return Meta.SingleCache.GetItemWithSlaveKey(sessionId) as UserOnline;
            }

            return Find(_.SessionID == sessionId);
        }
        #endregion

        #region 高级查询
        /// <summary>高级查询</summary>
        /// <param name="userId">用户</param>
        /// <param name="sessionId">会话。Web的SessionID或Server的会话编号</param>
        /// <param name="start">创建时间开始</param>
        /// <param name="end">创建时间结束</param>
        /// <param name="key">关键字</param>
        /// <param name="page">分页参数信息。可携带统计和数据权限扩展查询等信息</param>
        /// <returns>实体列表</returns>
        public static IList<UserOnline> Search(Int32 userId, String sessionId, DateTime start, DateTime end, String key, PageParameter page)
        {
            var exp = new WhereExpression();

            if (userId >= 0) exp &= _.UserID == userId;
            if (!sessionId.IsNullOrEmpty()) exp &= _.SessionID == sessionId;
            exp &= _.CreateTime.Between(start, end);
            if (!key.IsNullOrEmpty()) exp &= _.Name.Contains(key) | _.SessionID.Contains(key) | _.Page.Contains(key) | _.Status.Contains(key) | _.CreateIP.Contains(key) | _.UpdateIP.Contains(key);

            return FindAll(exp, page);
        }

        // Select Count(ID) as ID,SessionID From UserOnline Where CreateTime>'2020-01-24 00:00:00' Group By SessionID Order By ID Desc limit 20
        static readonly FieldCache<UserOnline> _SessionIDCache = new FieldCache<UserOnline>(nameof(SessionID))
        {
            //Where = _.CreateTime > DateTime.Today.AddDays(-30) & Expression.Empty
        };

        /// <summary>获取会话列表，字段缓存10分钟，分组统计数据最多的前20种，用于魔方前台下拉选择</summary>
        /// <returns></returns>
        public static IDictionary<String, String> GetSessionIDList() => _SessionIDCache.FindAllName();
        #endregion

        #region 业务操作
        public void SetError(String error)
        {
            Status = error;
            LastError = DateTime.Now;
        }
        #endregion
    }
}