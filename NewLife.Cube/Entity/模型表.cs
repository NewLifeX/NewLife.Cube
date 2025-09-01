using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Web.Script.Serialization;
using System.Xml.Serialization;
using NewLife;
using NewLife.Data;
using XCode;
using XCode.Cache;
using XCode.Configuration;
using XCode.DataAccessLayer;

namespace NewLife.Cube.Entity;

/// <summary>模型表。实体表模型</summary>
[Serializable]
[DataObject]
[Description("模型表。实体表模型")]
[BindIndex("IU_ModelTable_Category_Name", true, "Category,Name")]
[BindTable("ModelTable", Description = "模型表。实体表模型", ConnName = "Cube", DbType = DatabaseType.None)]
public partial class ModelTable : IEntity<ModelTableModel>
{
    #region 属性
    private Int32 _Id;
    /// <summary>编号</summary>
    [DisplayName("编号")]
    [Description("编号")]
    [DataObjectField(true, true, false, 0)]
    [BindColumn("Id", "编号", "")]
    public Int32 Id { get => _Id; set { if (OnPropertyChanging("Id", value)) { _Id = value; OnPropertyChanged("Id"); } } }

    private String _Category;
    /// <summary>分类</summary>
    [DisplayName("分类")]
    [Description("分类")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("Category", "分类", "")]
    public String Category { get => _Category; set { if (OnPropertyChanging("Category", value)) { _Category = value; OnPropertyChanged("Category"); } } }

    private String _Name;
    /// <summary>名称</summary>
    [DisplayName("名称")]
    [Description("名称")]
    [DataObjectField(false, false, false, 50)]
    [BindColumn("Name", "名称", "", Master = true)]
    public String Name { get => _Name; set { if (OnPropertyChanging("Name", value)) { _Name = value; OnPropertyChanged("Name"); } } }

    private String _DisplayName;
    /// <summary>显示名</summary>
    [DisplayName("显示名")]
    [Description("显示名")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("DisplayName", "显示名", "")]
    public String DisplayName { get => _DisplayName; set { if (OnPropertyChanging("DisplayName", value)) { _DisplayName = value; OnPropertyChanged("DisplayName"); } } }

    private Boolean _Enable;
    /// <summary>启用</summary>
    [DisplayName("启用")]
    [Description("启用")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("Enable", "启用", "")]
    public Boolean Enable { get => _Enable; set { if (OnPropertyChanging("Enable", value)) { _Enable = value; OnPropertyChanged("Enable"); } } }

    private String _Url;
    /// <summary>路径。全路径</summary>
    [DisplayName("路径")]
    [Description("路径。全路径")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("Url", "路径。全路径", "")]
    public String Url { get => _Url; set { if (OnPropertyChanging("Url", value)) { _Url = value; OnPropertyChanged("Url"); } } }

    private String _Controller;
    /// <summary>控制器。控制器类型全名</summary>
    [DisplayName("控制器")]
    [Description("控制器。控制器类型全名")]
    [DataObjectField(false, false, true, 100)]
    [BindColumn("Controller", "控制器。控制器类型全名", "")]
    public String Controller { get => _Controller; set { if (OnPropertyChanging("Controller", value)) { _Controller = value; OnPropertyChanged("Controller"); } } }

    private String _TableName;
    /// <summary>表名</summary>
    [DisplayName("表名")]
    [Description("表名")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("TableName", "表名", "")]
    public String TableName { get => _TableName; set { if (OnPropertyChanging("TableName", value)) { _TableName = value; OnPropertyChanged("TableName"); } } }

    private String _ConnName;
    /// <summary>连接名</summary>
    [DisplayName("连接名")]
    [Description("连接名")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("ConnName", "连接名", "")]
    public String ConnName { get => _ConnName; set { if (OnPropertyChanging("ConnName", value)) { _ConnName = value; OnPropertyChanged("ConnName"); } } }

    private Boolean _InsertOnly;
    /// <summary>仅插入。日志型数据</summary>
    [DisplayName("仅插入")]
    [Description("仅插入。日志型数据")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("InsertOnly", "仅插入。日志型数据", "")]
    public Boolean InsertOnly { get => _InsertOnly; set { if (OnPropertyChanging("InsertOnly", value)) { _InsertOnly = value; OnPropertyChanged("InsertOnly"); } } }

    private String _Description;
    /// <summary>说明</summary>
    [DisplayName("说明")]
    [Description("说明")]
    [DataObjectField(false, false, true, 500)]
    [BindColumn("Description", "说明", "")]
    public String Description { get => _Description; set { if (OnPropertyChanging("Description", value)) { _Description = value; OnPropertyChanged("Description"); } } }

    private Int32 _CreateUserId;
    /// <summary>创建者</summary>
    [Category("扩展")]
    [DisplayName("创建者")]
    [Description("创建者")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("CreateUserId", "创建者", "")]
    public Int32 CreateUserId { get => _CreateUserId; set { if (OnPropertyChanging("CreateUserId", value)) { _CreateUserId = value; OnPropertyChanged("CreateUserId"); } } }

    private DateTime _CreateTime;
    /// <summary>创建时间</summary>
    [Category("扩展")]
    [DisplayName("创建时间")]
    [Description("创建时间")]
    [DataObjectField(false, false, true, 0)]
    [BindColumn("CreateTime", "创建时间", "")]
    public DateTime CreateTime { get => _CreateTime; set { if (OnPropertyChanging("CreateTime", value)) { _CreateTime = value; OnPropertyChanged("CreateTime"); } } }

    private String _CreateIP;
    /// <summary>创建地址</summary>
    [Category("扩展")]
    [DisplayName("创建地址")]
    [Description("创建地址")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("CreateIP", "创建地址", "")]
    public String CreateIP { get => _CreateIP; set { if (OnPropertyChanging("CreateIP", value)) { _CreateIP = value; OnPropertyChanged("CreateIP"); } } }

    private Int32 _UpdateUserId;
    /// <summary>更新者</summary>
    [Category("扩展")]
    [DisplayName("更新者")]
    [Description("更新者")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("UpdateUserId", "更新者", "")]
    public Int32 UpdateUserId { get => _UpdateUserId; set { if (OnPropertyChanging("UpdateUserId", value)) { _UpdateUserId = value; OnPropertyChanged("UpdateUserId"); } } }

    private DateTime _UpdateTime;
    /// <summary>更新时间</summary>
    [Category("扩展")]
    [DisplayName("更新时间")]
    [Description("更新时间")]
    [DataObjectField(false, false, true, 0)]
    [BindColumn("UpdateTime", "更新时间", "")]
    public DateTime UpdateTime { get => _UpdateTime; set { if (OnPropertyChanging("UpdateTime", value)) { _UpdateTime = value; OnPropertyChanged("UpdateTime"); } } }

    private String _UpdateIP;
    /// <summary>更新地址</summary>
    [Category("扩展")]
    [DisplayName("更新地址")]
    [Description("更新地址")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("UpdateIP", "更新地址", "")]
    public String UpdateIP { get => _UpdateIP; set { if (OnPropertyChanging("UpdateIP", value)) { _UpdateIP = value; OnPropertyChanged("UpdateIP"); } } }
    #endregion

    #region 拷贝
    /// <summary>拷贝模型对象</summary>
    /// <param name="model">模型</param>
    public void Copy(ModelTableModel model)
    {
        Id = model.Id;
        Category = model.Category;
        Name = model.Name;
        DisplayName = model.DisplayName;
        Enable = model.Enable;
        Url = model.Url;
        Controller = model.Controller;
        TableName = model.TableName;
        ConnName = model.ConnName;
        InsertOnly = model.InsertOnly;
        Description = model.Description;
        CreateUserId = model.CreateUserId;
        CreateTime = model.CreateTime;
        CreateIP = model.CreateIP;
        UpdateUserId = model.UpdateUserId;
        UpdateTime = model.UpdateTime;
        UpdateIP = model.UpdateIP;
    }
    #endregion

    #region 获取/设置 字段值
    /// <summary>获取/设置 字段值</summary>
    /// <param name="name">字段名</param>
    /// <returns></returns>
    public override Object this[String name]
    {
        get => name switch
        {
            "Id" => _Id,
            "Category" => _Category,
            "Name" => _Name,
            "DisplayName" => _DisplayName,
            "Enable" => _Enable,
            "Url" => _Url,
            "Controller" => _Controller,
            "TableName" => _TableName,
            "ConnName" => _ConnName,
            "InsertOnly" => _InsertOnly,
            "Description" => _Description,
            "CreateUserId" => _CreateUserId,
            "CreateTime" => _CreateTime,
            "CreateIP" => _CreateIP,
            "UpdateUserId" => _UpdateUserId,
            "UpdateTime" => _UpdateTime,
            "UpdateIP" => _UpdateIP,
            _ => base[name]
        };
        set
        {
            switch (name)
            {
                case "Id": _Id = value.ToInt(); break;
                case "Category": _Category = Convert.ToString(value); break;
                case "Name": _Name = Convert.ToString(value); break;
                case "DisplayName": _DisplayName = Convert.ToString(value); break;
                case "Enable": _Enable = value.ToBoolean(); break;
                case "Url": _Url = Convert.ToString(value); break;
                case "Controller": _Controller = Convert.ToString(value); break;
                case "TableName": _TableName = Convert.ToString(value); break;
                case "ConnName": _ConnName = Convert.ToString(value); break;
                case "InsertOnly": _InsertOnly = value.ToBoolean(); break;
                case "Description": _Description = Convert.ToString(value); break;
                case "CreateUserId": _CreateUserId = value.ToInt(); break;
                case "CreateTime": _CreateTime = value.ToDateTime(); break;
                case "CreateIP": _CreateIP = Convert.ToString(value); break;
                case "UpdateUserId": _UpdateUserId = value.ToInt(); break;
                case "UpdateTime": _UpdateTime = value.ToDateTime(); break;
                case "UpdateIP": _UpdateIP = Convert.ToString(value); break;
                default: base[name] = value; break;
            }
        }
    }
    #endregion

    #region 关联映射
    #endregion

    #region 扩展查询
    /// <summary>根据分类查找</summary>
    /// <param name="category">分类</param>
    /// <returns>实体列表</returns>
    public static IList<ModelTable> FindAllByCategory(String category)
    {
        if (category.IsNullOrEmpty()) return [];

        // 实体缓存
        if (Meta.Session.Count < 1000) return Meta.Cache.FindAll(e => e.Category.EqualIgnoreCase(category));

        return FindAll(_.Category == category);
    }
    #endregion

    #region 高级查询
    /// <summary>高级查询</summary>
    /// <param name="category">分类</param>
    /// <param name="insertOnly">仅插入。日志型数据</param>
    /// <param name="enable">启用</param>
    /// <param name="start">更新时间开始</param>
    /// <param name="end">更新时间结束</param>
    /// <param name="key">关键字</param>
    /// <param name="page">分页参数信息。可携带统计和数据权限扩展查询等信息</param>
    /// <returns>实体列表</returns>
    public static IList<ModelTable> Search(String category, Boolean? insertOnly, Boolean? enable, DateTime start, DateTime end, String key, PageParameter page)
    {
        var exp = new WhereExpression();

        if (!category.IsNullOrEmpty()) exp &= _.Category == category;
        if (insertOnly != null) exp &= _.InsertOnly == insertOnly;
        if (enable != null) exp &= _.Enable == enable;
        exp &= _.UpdateTime.Between(start, end);
        if (!key.IsNullOrEmpty()) exp &= SearchWhereByKeys(key);

        return FindAll(exp, page);
    }
    #endregion

    #region 字段名
    /// <summary>取得模型表字段信息的快捷方式</summary>
    public partial class _
    {
        /// <summary>编号</summary>
        public static readonly Field Id = FindByName("Id");

        /// <summary>分类</summary>
        public static readonly Field Category = FindByName("Category");

        /// <summary>名称</summary>
        public static readonly Field Name = FindByName("Name");

        /// <summary>显示名</summary>
        public static readonly Field DisplayName = FindByName("DisplayName");

        /// <summary>启用</summary>
        public static readonly Field Enable = FindByName("Enable");

        /// <summary>路径。全路径</summary>
        public static readonly Field Url = FindByName("Url");

        /// <summary>控制器。控制器类型全名</summary>
        public static readonly Field Controller = FindByName("Controller");

        /// <summary>表名</summary>
        public static readonly Field TableName = FindByName("TableName");

        /// <summary>连接名</summary>
        public static readonly Field ConnName = FindByName("ConnName");

        /// <summary>仅插入。日志型数据</summary>
        public static readonly Field InsertOnly = FindByName("InsertOnly");

        /// <summary>说明</summary>
        public static readonly Field Description = FindByName("Description");

        /// <summary>创建者</summary>
        public static readonly Field CreateUserId = FindByName("CreateUserId");

        /// <summary>创建时间</summary>
        public static readonly Field CreateTime = FindByName("CreateTime");

        /// <summary>创建地址</summary>
        public static readonly Field CreateIP = FindByName("CreateIP");

        /// <summary>更新者</summary>
        public static readonly Field UpdateUserId = FindByName("UpdateUserId");

        /// <summary>更新时间</summary>
        public static readonly Field UpdateTime = FindByName("UpdateTime");

        /// <summary>更新地址</summary>
        public static readonly Field UpdateIP = FindByName("UpdateIP");

        static Field FindByName(String name) => Meta.Table.FindByName(name);
    }

    /// <summary>取得模型表字段名称的快捷方式</summary>
    public partial class __
    {
        /// <summary>编号</summary>
        public const String Id = "Id";

        /// <summary>分类</summary>
        public const String Category = "Category";

        /// <summary>名称</summary>
        public const String Name = "Name";

        /// <summary>显示名</summary>
        public const String DisplayName = "DisplayName";

        /// <summary>启用</summary>
        public const String Enable = "Enable";

        /// <summary>路径。全路径</summary>
        public const String Url = "Url";

        /// <summary>控制器。控制器类型全名</summary>
        public const String Controller = "Controller";

        /// <summary>表名</summary>
        public const String TableName = "TableName";

        /// <summary>连接名</summary>
        public const String ConnName = "ConnName";

        /// <summary>仅插入。日志型数据</summary>
        public const String InsertOnly = "InsertOnly";

        /// <summary>说明</summary>
        public const String Description = "Description";

        /// <summary>创建者</summary>
        public const String CreateUserId = "CreateUserId";

        /// <summary>创建时间</summary>
        public const String CreateTime = "CreateTime";

        /// <summary>创建地址</summary>
        public const String CreateIP = "CreateIP";

        /// <summary>更新者</summary>
        public const String UpdateUserId = "UpdateUserId";

        /// <summary>更新时间</summary>
        public const String UpdateTime = "UpdateTime";

        /// <summary>更新地址</summary>
        public const String UpdateIP = "UpdateIP";
    }
    #endregion
}
