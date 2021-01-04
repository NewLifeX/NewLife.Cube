using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using XCode;
using XCode.Configuration;

namespace NewLife.Cube
{
    /// <summary>字段集合</summary>
    public class FieldCollection : List<FieldItem>
    {
        #region 属性
        /// <summary>工厂</summary>
        public IEntityFactory Factory { get; set; }

        /// <summary>定制版字段</summary>
        public IList<DataField> Fields { get; set; } = new List<DataField>();
        #endregion

        #region 构造
        /// <summary>使用工厂实例化一个字段集合</summary>
        /// <param name="factory"></param>
        public FieldCollection(IEntityFactory factory)
        {
            Factory = factory;
            AddRange(Factory.Fields);
        }
        #endregion

        #region 方法
        /// <summary>设置扩展关系</summary>
        /// <param name="isForm">是否表单使用</param>
        /// <returns></returns>
        public FieldCollection SetRelation(Boolean isForm)
        {
            var type = Factory.EntityType;
            // 扩展属性
            foreach (var pi in type.GetProperties())
            {
                ProcessRelation(pi, isForm);
            }

            if (!isForm)
            {
                // 长字段和密码字段不显示
                NoPass();
            }

            return this;
        }

        void ProcessRelation(PropertyInfo pi, Boolean isForm)
        {
            // 处理带有Map特性的扩展属性
            var map = pi.GetCustomAttribute<MapAttribute>();
            if (map == null) return;

            Replace(map.Name, pi.Name);
        }

        void NoPass()
        {
            for (var i = Count - 1; i >= 0; i--)
            {
                var fi = this[i];
                if (fi.IsDataObjectField && fi.Type == typeof(String))
                {
                    if (fi.Length <= 0 || fi.Length > 1000 ||
                        fi.Name.EqualIgnoreCase("password", "pass"))
                    {
                        RemoveAt(i);
                    }
                }
            }
        }
        #endregion

        #region 添加删除替换
        /// <summary>从AllFields中添加字段，可以是扩展属性</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public FieldCollection AddField(String name)
        {
            var fi = Factory.AllFields.FirstOrDefault(e => e.Name.EqualIgnoreCase(name));
            if (fi != null) Add(fi);

            return this;
        }

        /// <summary>在指定字段之后添加扩展属性</summary>
        /// <param name="oriName"></param>
        /// <param name="newName"></param>
        /// <returns></returns>
        public FieldCollection AddField(String oriName, String newName)
        {
            for (var i = 0; i < Count; i++)
            {
                if (this[i].Name.EqualIgnoreCase(oriName))
                {
                    var fi = Factory.AllFields.FirstOrDefault(e => e.Name.EqualIgnoreCase(newName));
                    if (fi != null) Insert(i + 1, fi);
                    break;
                }
            }

            return this;
        }

        /// <summary>删除字段</summary>
        /// <param name="names"></param>
        /// <returns></returns>
        public FieldCollection RemoveField(params String[] names)
        {
            foreach (var item in names)
            {
                if (!item.IsNullOrEmpty()) RemoveAll(e => e.Name.EqualIgnoreCase(item) || e.ColumnName.EqualIgnoreCase(item));
            }

            return this;
        }

        /// <summary>操作字段列表，把旧项换成新项</summary>
        /// <param name="oriName"></param>
        /// <param name="newName"></param>
        /// <returns></returns>
        public FieldCollection Replace(String oriName, String newName)
        {
            var idx = FindIndex(e => e.Name.EqualIgnoreCase(oriName));
            if (idx < 0) return this;

            var fi = Factory.AllFields.FirstOrDefault(e => e.Name.EqualIgnoreCase(newName));
            // 如果没有找到新项，则删除旧项
            if (fi == null)
            {
                RemoveAt(idx);
                return this;
            }
            // 如果本身就存在目标项，则删除旧项
            if (Contains(fi))
            {
                RemoveAt(idx);
                return this;
            }

            this[idx] = fi;

            return this;
        }
        #endregion

        #region 创建信息/更新信息
        /// <summary>设置是否显示创建信息</summary>
        /// <returns></returns>
        public FieldCollection RemoveCreateField()
        {
            RemoveAll(e => e.Name.EqualIgnoreCase("CreateUserID", "CreateUser", "CreateTime", "CreateIP"));

            return this;
        }

        /// <summary>设置是否显示更新信息</summary>
        /// <returns></returns>
        public FieldCollection RemoveUpdateField()
        {
            RemoveAll(e => e.Name.EqualIgnoreCase("UpdateUserID", "UpdateUser", "UpdateTime", "UpdateIP"));

            return this;
        }

        /// <summary>设置是否显示备注信息</summary>
        /// <returns></returns>
        public FieldCollection RemoveRemarkField()
        {
            RemoveAll(e => e.Name.EqualIgnoreCase("Remark", "Description"));

            return this;
        }
        #endregion

        #region 自定义字段
        /// <summary>添加定制版数据字段</summary>
        /// <param name="fi"></param>
        /// <returns></returns>
        public DataField AddDataField(FieldItem fi)
        {
            var df = new DataField
            {
                Name = fi.Name,
                Header = fi.DisplayName,
            };
            Fields.Add(df);

            return df;
        }

        /// <summary>添加定制字段，插入指定列之前</summary>
        /// <param name="name"></param>
        /// <param name="beforeName"></param>
        /// <returns></returns>
        public DataField AddDataField(String name, String beforeName = null)
        {
            var df = new DataField
            {
                Name = name,
                BeforeName = beforeName,
            };
            
            var fi = Find(e => e.Name == name);
            if (fi != null) df.DisplayName = fi.DisplayName;

            Fields.Add(df);

            return df;
        }

        /// <summary>获取指定名称的定制字段</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public DataField GetField(String name) => Fields.FirstOrDefault(e => e.Name == name);

        /// <summary>获取指定列名之前的定制字段</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public DataField GetBeforeField(String name) => Fields.FirstOrDefault(e => e.BeforeName == name);
        #endregion
    }
}