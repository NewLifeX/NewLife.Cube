using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Web.Script.Serialization;
using System.Xml.Serialization;
using NewLife.Data;
using XCode;
using XCode.Configuration;
using XCode.Membership;

namespace NewLife.Cube.Entity
{
    /// <summary>表单显示</summary>
    [Flags]
    public enum ShowInForm
    {
        /// <summary>详情</summary>
        详情 = 1,

        /// <summary>添加</summary>
        添加 = 2,

        /// <summary>编辑</summary>
        编辑 = 4
    }

    /// <summary>模型列。实体表的数据列</summary>
    public partial class ModelColumn : Entity<ModelColumn>
    {
        #region 对象操作
        static ModelColumn()
        {
            // 累加字段，生成 Update xx Set Count=Count+1234 Where xxx
            //var df = Meta.Factory.AdditionalFields;
            //df.Add(nameof(TableId));

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
            if (Name.IsNullOrEmpty()) throw new ArgumentNullException(nameof(Name), "名称不能为空！");

            // 建议先调用基类方法，基类方法会做一些统一处理
            base.Valid(isNew);
        }
        #endregion

        #region 扩展属性
        /// <summary>模型表</summary>
        [XmlIgnore, ScriptIgnore, IgnoreDataMember]
        public ModelTable Table => Extends.Get(nameof(Table), k => ModelTable.FindById(TableId));

        /// <summary>模型表</summary>
        [Map(__.TableId, typeof(ModelTable), "Id")]
        public String TableName => Table + "";

        /// <summary>对应字段</summary>
        [XmlIgnore, ScriptIgnore, IgnoreDataMember]
        public Field Field => Meta.Table.FindByName(Name);
        #endregion

        #region 扩展查询
        /// <summary>根据编号查找</summary>
        /// <param name="id">编号</param>
        /// <returns>实体对象</returns>
        public static ModelColumn FindById(Int32 id)
        {
            if (id <= 0) return null;

            // 实体缓存
            if (Meta.Session.Count < 1000) return Meta.Cache.Find(e => e.Id == id);

            // 单对象缓存
            return Meta.SingleCache[id];

            //return Find(_.Id == id);
        }

        /// <summary>根据模型表、名称查找</summary>
        /// <param name="tableId">模型表</param>
        /// <param name="name">名称</param>
        /// <returns>实体对象</returns>
        public static ModelColumn FindByTableIdAndName(Int32 tableId, String name)
        {
            // 实体缓存
            if (Meta.Session.Count < 1000) return Meta.Cache.Find(e => e.TableId == tableId && e.Name.EqualIgnoreCase(name));

            return Find(_.TableId == tableId & _.Name == name);
        }

        /// <summary>返回表之下的所有字段</summary>
        /// <param name="tableId"></param>
        /// <returns></returns>
        public static IList<ModelColumn> FindAllByTableId(Int32 tableId)
        {
            if (Meta.Count < 1000) return Meta.Cache.FindAll(e => e.TableId == tableId);

            return FindAll(_.TableId == tableId);
        }
        #endregion

        #region 高级查询
        /// <summary>高级查询</summary>
        /// <param name="tableId">模型表</param>
        /// <param name="name">名称</param>
        /// <param name="start">更新时间开始</param>
        /// <param name="end">更新时间结束</param>
        /// <param name="key">关键字</param>
        /// <param name="page">分页参数信息。可携带统计和数据权限扩展查询等信息</param>
        /// <returns>实体列表</returns>
        public static IList<ModelColumn> Search(Int32 tableId, String name, DateTime start, DateTime end, String key, PageParameter page)
        {
            var exp = new WhereExpression();

            if (tableId >= 0) exp &= _.TableId == tableId;
            if (!name.IsNullOrEmpty()) exp &= _.Name == name;
            exp &= _.UpdateTime.Between(start, end);
            if (!key.IsNullOrEmpty()) exp &= _.DisplayName.Contains(key) | _.DataType.Contains(key) | _.ItemType.Contains(key) | _.Description.Contains(key) | _.Width.Contains(key) | _.CellText.Contains(key) | _.CellTitle.Contains(key) | _.CellUrl.Contains(key) | _.HeaderText.Contains(key) | _.HeaderTitle.Contains(key) | _.HeaderUrl.Contains(key) | _.DataAction.Contains(key) | _.DataSource.Contains(key) | _.CreateIP.Contains(key) | _.UpdateIP.Contains(key);

            return FindAll(exp, page);
        }
        #endregion

        #region 业务操作
        /// <summary>填充字段</summary>
        /// <param name="field"></param>
        public void Fill(FieldItem field)
        {
            var dc = field.Field;
            if (dc != null)
            {
                DisplayName = dc.DisplayName;
                Description = dc.Description;

                DataType = dc.DataType.Name;
                ItemType = dc.ItemType;
                PrimaryKey = dc.PrimaryKey;
                Master = dc.Master;
                Length = dc.Length;
                Nullable = dc.Nullable;
            }
            else
            {
                DisplayName = field.DisplayName;

                DataType = field.Type.Name;
            }

            IsDataObjectField = field.IsDataObjectField;
        }

        /// <summary>
        /// 根据DisplayName长度设置列宽，两个字-80，三个字-90，四个字-105，五个字-115
        /// </summary>
        public void SetWidth()
        {
            var length = DisplayName.Length;
            var width = length switch
            {
                < 3 => "80",
                3 => "90",
                4 => "105",
                > 4 => "115",
            };

            // 特殊类型处理
            if (DataType == nameof(DateTime))
            {
                width = "155";
            }

            Width = width;
        }
        #endregion
    }
}