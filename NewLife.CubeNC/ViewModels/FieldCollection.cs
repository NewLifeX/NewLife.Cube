using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NewLife.Cube.ViewModels;
using XCode;
using XCode.Configuration;

namespace NewLife.Cube
{
    /// <summary>字段集合</summary>
    public class FieldCollection : List<DataField>
    {
        #region 属性
        /// <summary>类型</summary>
        public String Kind { get; set; }

        /// <summary>工厂</summary>
        public IEntityFactory Factory { get; set; }

        ///// <summary>定制版字段</summary>
        //public IList<DataField> Fields { get; set; } = new List<DataField>();
        #endregion

        #region 构造
        /// <summary>使用工厂实例化一个字段集合</summary>
        /// <param name="factory"></param>
        /// <param name="kind"></param>
        public FieldCollection(IEntityFactory factory, String kind)
        {
            Kind = kind;
            Factory = factory;
            //AddRange(Factory.Fields);

            foreach (var item in Factory.Fields)
            {
                Add(item);
            }

            switch (kind)
            {
                case "AddForm":
                    SetRelation(true);
                    //RemoveCreateField();
                    RemoveUpdateField();
                    break;
                case "EditForm":
                    SetRelation(true);
                    break;
                case "Detail":
                    SetRelation(true);
                    break;
                case "Form":
                    SetRelation(true);
                    break;
                case "List":
                default:
                    SetRelation(false);
                    break;
            }
        }
        #endregion

        #region 方法
        /// <summary>为指定字段创建数据字段，可以为空</summary>
        /// <param name="field"></param>
        /// <returns></returns>
        public DataField Create(FieldItem field)
        {
            DataField df = Kind switch
            {
                "AddForm" => new FormField(),
                "EditForm" => new FormField(),
                "Detail" => new FormField(),
                "Form" => new FormField(),
                "List" => new ListField(),
                _ => throw new NotImplementedException(),
            };
            //df.Sort = Count + 1;
            //df.Sort = Count == 0 ? 1 : (this[Count - 1].Sort + 1);
            if (field != null) df.Fill(field);

            return df;
        }

        /// <summary>为指定字段创建数据字段</summary>
        /// <param name="field"></param>
        /// <returns></returns>
        public DataField Add(FieldItem field)
        {
            var df = Create(field);

            Add(df);

            return df;
        }

        /// <summary>设置扩展关系</summary>
        /// <param name="isForm">是否表单使用</param>
        /// <returns></returns>
        public FieldCollection SetRelation(Boolean isForm)
        {
            var type = Factory.EntityType;
            // 扩展属性
            foreach (var pi in type.GetProperties())
            {
                // 处理带有Map特性的扩展属性
                var map = pi.GetCustomAttribute<MapAttribute>();
                if (map != null) Replace(map.Name, pi.Name);
            }

            if (!isForm)
            {
                // 长字段和密码字段不显示
                NoPass();
            }

            return this;
        }

        private void NoPass()
        {
            for (var i = Count - 1; i >= 0; i--)
            {
                var fi = this[i];
                if (fi.Type == typeof(String) && fi.MapField.IsNullOrEmpty())
                {
                    if (fi.Length <= 0 || fi.Length > 1000 ||
                        fi.Name.EqualIgnoreCase("password", "pass", "pwd", "Secret"))
                    {
                        RemoveAt(i);
                    }
                }
            }
        }
        #endregion

        #region 添加删除替换
        /// <summary>查找指定字段</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Int32 FindIndex(String name) => FindIndex(e => e.Name.EqualIgnoreCase(name));

        /// <summary>从AllFields中添加字段，可以是扩展属性</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public FieldCollection AddField(String name)
        {
            var fi = Factory.AllFields.FirstOrDefault(e => e.Name.EqualIgnoreCase(name));
            if (fi != null) Add(fi);

            return this;
        }

        /// <summary>删除字段</summary>
        /// <param name="names"></param>
        /// <returns></returns>
        public FieldCollection RemoveField(params String[] names)
        {
            foreach (var item in names)
            {
                if (!item.IsNullOrEmpty()) RemoveAll(e => e.Name.EqualIgnoreCase(item));
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
            if (fi == null) return this;

            // 如果本身就存在目标项，则删除
            var idx2 = FindIndex(e => e.Name.EqualIgnoreCase(fi.Name));
            if (idx2 >= 0) RemoveAt(idx2);

            this[idx] = Create(fi);

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
        /// <summary>添加定制字段，插入指定列前后</summary>
        /// <param name="name"></param>
        /// <param name="beforeName"></param>
        /// <param name="afterName"></param>
        /// <returns></returns>
        public DataField AddDataField(String name, String beforeName = null, String afterName = null)
        {
            if (name.IsNullOrEmpty()) throw new ArgumentNullException(nameof(name));

            var fi = Factory.AllFields.FirstOrDefault(e => e.Name.EqualIgnoreCase(name));
            // 有可能fi为空，创建一个所有字段都为空的field
            var field = Create(fi);
            if (field.Name.IsNullOrEmpty()) field.Name = name;

            if (!beforeName.IsNullOrEmpty())
            {
                var idx = FindIndex(beforeName);
                if (idx >= 0)
                    Insert(idx, field);
                else
                    Add(field);
            }
            else if (!afterName.IsNullOrEmpty())
            {
                var idx = FindIndex(afterName);
                if (idx >= 0)
                    Insert(idx + 1, field);
                else
                    Add(field);
            }
            else
                Add(field);

            return field;
        }

        /// <summary>添加定制字段，插入指定列前后</summary>
        /// <param name="name"></param>
        /// <param name="beforeName"></param>
        /// <param name="afterName"></param>
        /// <returns></returns>
        public ListField AddListField(String name, String beforeName = null, String afterName = null) => AddDataField(name, beforeName, afterName) as ListField;

        /// <summary>获取指定名称的定制字段</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public DataField GetField(String name) => this.FirstOrDefault(e => e.Name.EqualIgnoreCase(name));
        #endregion
    }
}