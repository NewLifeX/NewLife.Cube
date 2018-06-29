using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;
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

namespace NewLife.Cube.Entity
{
    /// <summary>应用日志。用于OAuthServer的子系统</summary>
    public partial class AppLog : Entity<AppLog>
    {
        #region 对象操作
        static AppLog()
        {
            // 累加字段
            //Meta.Factory.AdditionalFields.Add(__.Logins);

            // 过滤器 UserModule、TimeModule、IPModule
            Meta.Modules.Add<UserModule>();
            Meta.Modules.Add<TimeModule>();
            Meta.Modules.Add<IPModule>();

            Meta.Table.DataTable.InsertOnly = true;
        }
        #endregion

        #region 扩展属性
        /// <summary>应用</summary>
        [XmlIgnore]
        //[ScriptIgnore]
        public App App => Extends.Get(nameof(App), k => App.FindByID(AppID));

        /// <summary>应用</summary>
        [XmlIgnore]
        //[ScriptIgnore]
        [DisplayName("应用")]
        [Map(__.AppID, typeof(App), "ID")]
        public String AppName => App + "";
        #endregion

        #region 扩展查询
        /// <summary>根据编号查找</summary>
        /// <param name="id">编号</param>
        /// <returns>实体对象</returns>
        public static AppLog FindByID(Int32 id)
        {
            if (id <= 0) return null;

            // 实体缓存
            if (Meta.Session.Count < 1000) return Meta.Cache.Find(e => e.ID == id);

            // 单对象缓存
            //return Meta.SingleCache[id];

            return Find(_.ID == id);
        }
        #endregion

        #region 高级查询
        #endregion

        #region 业务操作
        /// <summary>创建日志</summary>
        /// <param name="appid"></param>
        /// <param name="action"></param>
        /// <param name="success"></param>
        /// <param name="remark"></param>
        /// <returns></returns>
        public static AppLog Create(Int32 appid, String action, Boolean success, String remark)
        {
            var log = new AppLog
            {
                AppID = appid,
                Action = action,
                Success = success,
                Remark = remark,
                CreateTime = DateTime.Now,
            };
            log.SaveAsync();

            return log;
        }
        #endregion
    }
}