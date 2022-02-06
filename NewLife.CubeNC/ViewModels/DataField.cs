using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using NewLife.Collections;
using NewLife.Reflection;
using XCode;
using XCode.Configuration;

namespace NewLife.Cube.ViewModels
{
    /// <summary>获取数据源委托</summary>
    /// <param name="entity"></param>
    /// <param name="field"></param>
    /// <returns></returns>
    public delegate IDictionary DataSourceDelegate(IEntity entity, DataField field);

    /// <summary>数据可见委托</summary>
    /// <param name="entity"></param>
    /// <param name="field"></param>
    /// <returns></returns>
    public delegate Boolean DataVisibleDelegate(IEntity entity, DataField field);

    /// <summary>数据字段</summary>
    public class DataField
    {
        #region 属性
        /// <summary>名称</summary>
        public String Name { get; set; }

        /// <summary>显示名</summary>
        public String DisplayName { get; set; }

        /// <summary>描述</summary>
        public String Description { get; set; }

        /// <summary>类别</summary>
        public String Category { get; set; }

        /// <summary>属性类型</summary>
        [IgnoreDataMember]
        public Type Type { get; set; }

        /// <summary>数据类型</summary>
        public String DataType { get; set; }

        /// <summary>元素类型。image,file,html,singleSelect,multipleSelect</summary>
        public String ItemType { get; set; }

        /// <summary>长度</summary>
        public Int32 Length { get; set; }

        /// <summary>精度</summary>
        public Int32 Precision { get; set; }

        /// <summary>位数</summary>
        public Int32 Scale { get; set; }

        /// <summary>允许空</summary>
        public Boolean Nullable { get; set; }

        /// <summary>主键</summary>
        public Boolean PrimaryKey { get; set; }

        /// <summary>只读</summary>
        public Boolean Readonly { get; set; }

        ///// <summary>排序</summary>
        //public Int32 Sort { get; set; }

        /// <summary>映射字段</summary>
        public String MapField { get; set; }

        /// <summary>映射提供者</summary>
        [XmlIgnore, IgnoreDataMember]
        public MapProvider MapProvider { get; set; }

        /// <summary>多选数据源</summary>
        public DataSourceDelegate DataSource { get; set; }
      
        /// <summary>是否显示</summary>
        public DataVisibleDelegate DataVisible { get; set; }

        /// <summary>扩展属性</summary>
        [XmlIgnore, IgnoreDataMember]
        public IDictionary<String, String> Properties { get; set; } = new NullableDictionary<String, String>(StringComparer.OrdinalIgnoreCase);
        #endregion

        #region 构造
        /// <summary>已重载</summary>
        /// <returns></returns>
        public override String ToString() => $"{Name} {DisplayName} {Type.Name}";
        #endregion

        #region 方法
        ///// <summary>实例化</summary>
        //public DataField() { }

        /// <summary>从FieldItem填充</summary>
        /// <param name="field"></param>
        public virtual void Fill(FieldItem field)
        {
            var dc = field.Field;
            var pi = field.GetValue("_Property", false) as PropertyInfo;

            Name = field.Name;
            DisplayName = field.DisplayName;
            Description = field.Description;

            Category = pi?.GetCustomAttribute<CategoryAttribute>()?.Category + "";

            Type = field.Type;
            DataType = field.Type.Name;

            Length = field.Length;
            Nullable = field.IsNullable;
            PrimaryKey = field.PrimaryKey;
            Readonly = field.ReadOnly;

            if (field.Map != null)
            {
                MapField = field.Map.Name;
                MapProvider = field.Map.Provider;
            }

            if (dc != null)
            {
                ItemType = dc.ItemType;
                Precision = dc.Precision;
                Scale = dc.Scale;

                if (dc.Properties != null)
                {
                    foreach (var item in dc.Properties)
                    {
                        Properties[item.Key] = item.Value;
                    }
                }
            }
        }

        /// <summary>克隆</summary>
        /// <returns></returns>
        public virtual DataField Clone()
        {
            //var df = GetType().CreateInstance() as DataField;

            //df.Name = Name;
            //df.DisplayName = DisplayName;
            //df.Description = Description;
            //df.Category = Category;
            //df.Type = Type;
            //df.DataType = DataType;
            //df.ItemType = ItemType;
            //df.Length = Length;
            //df.Precision = Precision;
            //df.Scale = Scale;
            //df.Nullable = Nullable;
            //df.PrimaryKey = PrimaryKey;
            //df.Readonly = Readonly;
            //df.MapField = MapField;
            //df.MapProvider = MapProvider;
            //df.DataSource = DataSource;
            //df.Properties = Properties;

            //return df;

            return MemberwiseClone() as DataField;
        }

        /// <summary>是否大文本字段</summary>
        /// <returns></returns>
        public virtual Boolean IsBigText() => Type == typeof(String) && (Length < 0 || Length >= 300 || Length >= 200 && Name.EqualIgnoreCase("Remark", "Description", "Comment"));
        #endregion

        #region 服务
        private readonly List<Object> _services = new();
        /// <summary>添加服务</summary>
        /// <typeparam name="TService"></typeparam>
        /// <param name="service"></param>
        public virtual void AddService<TService>(TService service) => _services.Add(service);

        /// <summary>获取服务</summary>
        /// <typeparam name="TService"></typeparam>
        /// <returns></returns>
        public virtual TService GetService<TService>() => (TService)_services.FirstOrDefault(e => e is TService);
        #endregion
    }
}