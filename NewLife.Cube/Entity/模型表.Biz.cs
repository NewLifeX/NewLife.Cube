using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web.Script.Serialization;
using System.Xml.Serialization;
using NewLife.Data;
using XCode;
using XCode.Cache;
using XCode.Configuration;
using XCode.DataAccessLayer;
using XCode.Membership;

namespace NewLife.Cube.Entity
{
    /// <summary>模型表。实体表模型</summary>
    public partial class ModelTable : Entity<ModelTable>
    {
        #region 对象操作
        static ModelTable()
        {
            // 累加字段，生成 Update xx Set Count=Count+1234 Where xxx
            //var df = Meta.Factory.AdditionalFields;
            //df.Add(nameof(CreateUserId));

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

            // 这里验证参数范围，建议抛出参数异常，指定参数名，前端用户界面可以捕获参数异常并聚焦到对应的参数输入框
            if (Category.IsNullOrEmpty()) throw new ArgumentNullException(nameof(Category), "分类不能为空！");
            if (Name.IsNullOrEmpty()) throw new ArgumentNullException(nameof(Name), "名称不能为空！");

            // 建议先调用基类方法，基类方法会做一些统一处理
            base.Valid(isNew);
        }
        #endregion

        #region 扩展属性
        ///// <summary>模型列集合</summary>
        //[XmlIgnore, ScriptIgnore, IgnoreDataMember]
        //public IList<ModelColumn> Columns => Extends.Get(nameof(Columns), k => ModelColumn.FindAll());
        #endregion

        #region 扩展查询
        /// <summary>根据编号查找</summary>
        /// <param name="id">编号</param>
        /// <returns>实体对象</returns>
        public static ModelTable FindById(Int32 id)
        {
            if (id <= 0) return null;

            // 实体缓存
            if (Meta.Session.Count < 1000) return Meta.Cache.Find(e => e.Id == id);

            // 单对象缓存
            return Meta.SingleCache[id];

            //return Find(_.Id == id);
        }

        /// <summary>根据分类、名称查找</summary>
        /// <param name="category">分类</param>
        /// <param name="name">名称</param>
        /// <returns>实体对象</returns>
        public static ModelTable FindByCategoryAndName(String category, String name)
        {
            // 实体缓存
            if (Meta.Session.Count < 1000) return Meta.Cache.Find(e => e.Category.EqualIgnoreCase(category) && e.Name.EqualIgnoreCase(name));

            return Find(_.Category == category & _.Name == name);
        }
        #endregion

        #region 高级查询
        /// <summary>高级查询</summary>
        /// <param name="category">分类</param>
        /// <param name="name">名称</param>
        /// <param name="start">更新时间开始</param>
        /// <param name="end">更新时间结束</param>
        /// <param name="key">关键字</param>
        /// <param name="page">分页参数信息。可携带统计和数据权限扩展查询等信息</param>
        /// <returns>实体列表</returns>
        public static IList<ModelTable> Search(String category, String name, DateTime start, DateTime end, String key, PageParameter page)
        {
            var exp = new WhereExpression();

            if (!category.IsNullOrEmpty()) exp &= _.Category == category;
            if (!name.IsNullOrEmpty()) exp &= _.Name == name;
            exp &= _.UpdateTime.Between(start, end);
            if (!key.IsNullOrEmpty()) exp &= _.DisplayName.Contains(key) | _.Url.Contains(key) | _.Controller.Contains(key) | _.TableName.Contains(key) | _.ConnName.Contains(key) | _.Description.Contains(key) | _.CreateIP.Contains(key) | _.UpdateIP.Contains(key);

            return FindAll(exp, page);
        }

        // Select Count(Id) as Id,Category From ModelTable Where CreateTime>'2020-01-24 00:00:00' Group By Category Order By Id Desc limit 20
        private static readonly FieldCache<ModelTable> _CategoryCache = new FieldCache<ModelTable>(nameof(Category))
        {
            //Where = _.CreateTime > DateTime.Today.AddDays(-30) & Expression.Empty
        };

        /// <summary>获取分类列表，字段缓存10分钟，分组统计数据最多的前20种，用于魔方前台下拉选择</summary>
        /// <returns></returns>
        public static IDictionary<String, String> GetCategoryList() => _CategoryCache.FindAllName();
        #endregion

        #region 业务操作
        /// <summary>获取有效模型表</summary>
        /// <returns></returns>
        public static IList<ModelTable> GetValids() => FindAllWithCache().Where(e => e.Enable).ToList();

        /// <summary>填充</summary>
        /// <param name="table"></param>
        public void Fill(TableItem table)
        {
            var dt = table.DataTable;

            DisplayName = dt.DisplayName;
            Description = dt.Description;

            TableName = dt.TableName;
            ConnName = table.ConnName;
            InsertOnly = dt.InsertOnly;
        }
        #endregion
    }
}