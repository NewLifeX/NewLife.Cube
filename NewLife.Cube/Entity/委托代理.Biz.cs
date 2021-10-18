using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Web.Script.Serialization;
using System.Xml.Serialization;
using NewLife.Data;
using XCode;
using XCode.Membership;

namespace NewLife.Cube.Entity
{
    /// <summary>委托代理。委托某人代理自己的用户权限，代理人下一次登录时将得到委托人的身份，一次有效</summary>
    [ModelCheckMode(ModelCheckModes.CheckTableWhenFirstUse)]
    public partial class PrincipalAgent : Entity<PrincipalAgent>
    {
        #region 对象操作
        static PrincipalAgent()
        {
            // 累加字段，生成 Update xx Set Count=Count+1234 Where xxx
            //var df = Meta.Factory.AdditionalFields;
            //df.Add(nameof(PrincipalId));

            // 过滤器 UserModule、TimeModule、IPModule
            Meta.Modules.Add<UserModule>();
            Meta.Modules.Add<TimeModule>();
            Meta.Modules.Add<IPModule>();
        }

        /// <summary>验证并修补数据，通过抛出异常的方式提示验证失败。</summary>
        /// <param name="isNew">是否插入</param>
        public override void Valid(Boolean isNew)
        {
            // 如果没有脏数据，则不需要进行任何处理
            if (!HasDirty) return;

            if (PrincipalId <= 0) throw new ArgumentOutOfRangeException(nameof(PrincipalId));
            if (AgentId <= 0) throw new ArgumentOutOfRangeException(nameof(AgentId));
            if (PrincipalId == AgentId) throw new ArgumentOutOfRangeException(nameof(AgentId), "委托人和代理人不能是同一个人");
        }
        #endregion

        #region 扩展属性
        /// <summary>委托人</summary>
        [XmlIgnore, ScriptIgnore, IgnoreDataMember]
        public IUser Principal => Extends.Get(nameof(Principal), k => User.FindByID(PrincipalId));

        /// <summary>委托人</summary>
        [Map(nameof(PrincipalId))]
        public String PrincipalName => Principal + "";

        /// <summary>代理人</summary>
        [XmlIgnore, ScriptIgnore, IgnoreDataMember]
        public IUser Agent => Extends.Get(nameof(Agent), k => User.FindByID(AgentId));

        /// <summary>代理人</summary>
        [Map(nameof(AgentId))]
        public String AgentName => Agent + "";
        #endregion

        #region 扩展查询
        /// <summary>根据编号查找</summary>
        /// <param name="id">编号</param>
        /// <returns>实体对象</returns>
        public static PrincipalAgent FindById(Int32 id)
        {
            if (id <= 0) return null;

            //// 实体缓存
            //if (Meta.Session.Count < 1000) return Meta.Cache.Find(e => e.Id == id);

            //// 单对象缓存
            //return Meta.SingleCache[id];

            return Find(_.Id == id);
        }

        /// <summary>根据委托人查找</summary>
        /// <param name="principalId">委托人</param>
        /// <returns>实体列表</returns>
        public static IList<PrincipalAgent> FindAllByPrincipalId(Int32 principalId) => FindAll(_.PrincipalId == principalId);

        /// <summary>根据代理人查找</summary>
        /// <param name="agentId">代理人</param>
        /// <returns>实体列表</returns>
        public static IList<PrincipalAgent> FindAllByAgentId(Int32 agentId) => FindAll(_.AgentId == agentId);

        /// <summary>根据代理人选择可用代理项</summary>
        /// <param name="agentId"></param>
        /// <returns></returns>
        public static IList<PrincipalAgent> GetAllValidByAgentId(Int32 agentId) => FindAll(_.AgentId == agentId & _.Enable == true, _.Id.Asc(), null, 0, 100);
        #endregion

        #region 高级查询
        /// <summary>高级查询</summary>
        /// <param name="principalId">委托人。把自己的身份权限委托给别人</param>
        /// <param name="agentId">代理人。代理获得别人身份权限</param>
        /// <param name="start">更新时间开始</param>
        /// <param name="end">更新时间结束</param>
        /// <param name="key">关键字</param>
        /// <param name="page">分页参数信息。可携带统计和数据权限扩展查询等信息</param>
        /// <returns>实体列表</returns>
        public static IList<PrincipalAgent> Search(Int32 principalId, Int32 agentId, DateTime start, DateTime end, String key, PageParameter page)
        {
            var exp = new WhereExpression();

            if (principalId >= 0) exp &= _.PrincipalId == principalId;
            if (agentId >= 0) exp &= _.AgentId == agentId;
            exp &= _.UpdateTime.Between(start, end);
            if (!key.IsNullOrEmpty()) exp &= _.CreateIP.Contains(key) | _.UpdateIP.Contains(key) | _.Remark.Contains(key);

            return FindAll(exp, page);
        }
        #endregion

        #region 业务操作
        #endregion
    }
}