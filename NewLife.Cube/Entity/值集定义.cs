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

/// <summary>值集定义。值和列表的值集定义，支持枚举型和列表型</summary>
[Serializable]
[DataObject]
[Description("值集定义。值和列表的值集定义，支持枚举型和列表型")]
[BindIndex("IU_LovDefinition_LovCode", true, "LovCode")]
[BindIndex("IX_LovDefinition_Type_Enabled", false, "Type,Enabled")]
[BindTable("LovDefinition", Description = "值集定义。值和列表的值集定义，支持枚举型和列表型", ConnName = "Cube", DbType = DatabaseType.None)]
public partial class LovDefinition : IEntity<LovDefinitionModel>
{
    #region 属性
    private Int32 _Id;
    /// <summary>编号</summary>
    [DisplayName("编号")]
    [Description("编号")]
    [DataObjectField(true, true, false, 0)]
    [BindColumn("Id", "编号", "")]
    public Int32 Id { get => _Id; set { if (OnPropertyChanging("Id", value)) { _Id = value; OnPropertyChanged("Id"); } } }

    private String _LovCode;
    /// <summary>值集编码。带前缀的完整编码，如 Enum.ProcessCard.EnableStatus / List.User</summary>
    [DisplayName("值集编码")]
    [Description("值集编码。带前缀的完整编码，如 Enum.ProcessCard.EnableStatus / List.User")]
    [DataObjectField(false, false, false, 50)]
    [BindColumn("LovCode", "值集编码。带前缀的完整编码，如 Enum.ProcessCard.EnableStatus / List.User", "", Master = true)]
    public String LovCode { get => _LovCode; set { if (OnPropertyChanging("LovCode", value)) { _LovCode = value; OnPropertyChanged("LovCode"); } } }

    private String _Name;
    /// <summary>显示名称</summary>
    [DisplayName("显示名称")]
    [Description("显示名称")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("Name", "显示名称", "")]
    public String Name { get => _Name; set { if (OnPropertyChanging("Name", value)) { _Name = value; OnPropertyChanged("Name"); } } }

    private String _Type;
    /// <summary>值集类型。ENUM=枚举型/LIST=列表型，须与LovCode前缀一致</summary>
    [DisplayName("值集类型")]
    [Description("值集类型。ENUM=枚举型/LIST=列表型，须与LovCode前缀一致")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("Type", "值集类型。ENUM=枚举型/LIST=列表型，须与LovCode前缀一致", "")]
    public String Type { get => _Type; set { if (OnPropertyChanging("Type", value)) { _Type = value; OnPropertyChanged("Type"); } } }

    private String _ValueField;
    /// <summary>值字段。列表型的值字段名</summary>
    [DisplayName("值字段")]
    [Description("值字段。列表型的值字段名")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("ValueField", "值字段。列表型的值字段名", "")]
    public String ValueField { get => _ValueField; set { if (OnPropertyChanging("ValueField", value)) { _ValueField = value; OnPropertyChanged("ValueField"); } } }

    private String _LabelField;
    /// <summary>标签字段。列表型的标签字段名</summary>
    [DisplayName("标签字段")]
    [Description("标签字段。列表型的标签字段名")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("LabelField", "标签字段。列表型的标签字段名", "")]
    public String LabelField { get => _LabelField; set { if (OnPropertyChanging("LabelField", value)) { _LabelField = value; OnPropertyChanged("LabelField"); } } }

    private String _Source;
    /// <summary>来源。AUTO=自动注册/MANUAL=手工管理</summary>
    [DisplayName("来源")]
    [Description("来源。AUTO=自动注册/MANUAL=手工管理")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("Source", "来源。AUTO=自动注册/MANUAL=手工管理", "")]
    public String Source { get => _Source; set { if (OnPropertyChanging("Source", value)) { _Source = value; OnPropertyChanged("Source"); } } }

    private Boolean _Enabled;
    /// <summary>启用</summary>
    [DisplayName("启用")]
    [Description("启用")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("Enabled", "启用", "")]
    public Boolean Enabled { get => _Enabled; set { if (OnPropertyChanging("Enabled", value)) { _Enabled = value; OnPropertyChanged("Enabled"); } } }

    private Int32 _CreateUserID;
    /// <summary>创建用户</summary>
    [Category("扩展")]
    [DisplayName("创建用户")]
    [Description("创建用户")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("CreateUserID", "创建用户", "")]
    public Int32 CreateUserID { get => _CreateUserID; set { if (OnPropertyChanging("CreateUserID", value)) { _CreateUserID = value; OnPropertyChanged("CreateUserID"); } } }

    private String _CreateIP;
    /// <summary>创建地址</summary>
    [Category("扩展")]
    [DisplayName("创建地址")]
    [Description("创建地址")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("CreateIP", "创建地址", "")]
    public String CreateIP { get => _CreateIP; set { if (OnPropertyChanging("CreateIP", value)) { _CreateIP = value; OnPropertyChanged("CreateIP"); } } }

    private DateTime _CreateTime;
    /// <summary>创建时间</summary>
    [Category("扩展")]
    [DisplayName("创建时间")]
    [Description("创建时间")]
    [DataObjectField(false, false, true, 0)]
    [BindColumn("CreateTime", "创建时间", "")]
    public DateTime CreateTime { get => _CreateTime; set { if (OnPropertyChanging("CreateTime", value)) { _CreateTime = value; OnPropertyChanged("CreateTime"); } } }

    private Int32 _UpdateUserID;
    /// <summary>更新用户</summary>
    [Category("扩展")]
    [DisplayName("更新用户")]
    [Description("更新用户")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("UpdateUserID", "更新用户", "")]
    public Int32 UpdateUserID { get => _UpdateUserID; set { if (OnPropertyChanging("UpdateUserID", value)) { _UpdateUserID = value; OnPropertyChanged("UpdateUserID"); } } }

    private String _UpdateIP;
    /// <summary>更新地址</summary>
    [Category("扩展")]
    [DisplayName("更新地址")]
    [Description("更新地址")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("UpdateIP", "更新地址", "")]
    public String UpdateIP { get => _UpdateIP; set { if (OnPropertyChanging("UpdateIP", value)) { _UpdateIP = value; OnPropertyChanged("UpdateIP"); } } }

    private DateTime _UpdateTime;
    /// <summary>更新时间</summary>
    [Category("扩展")]
    [DisplayName("更新时间")]
    [Description("更新时间")]
    [DataObjectField(false, false, true, 0)]
    [BindColumn("UpdateTime", "更新时间", "")]
    public DateTime UpdateTime { get => _UpdateTime; set { if (OnPropertyChanging("UpdateTime", value)) { _UpdateTime = value; OnPropertyChanged("UpdateTime"); } } }

    private String _Remark;
    /// <summary>备注</summary>
    [Category("扩展")]
    [DisplayName("备注")]
    [Description("备注")]
    [DataObjectField(false, false, true, 500)]
    [BindColumn("Remark", "备注", "")]
    public String Remark { get => _Remark; set { if (OnPropertyChanging("Remark", value)) { _Remark = value; OnPropertyChanged("Remark"); } } }
    #endregion

    #region 拷贝
    /// <summary>拷贝模型对象</summary>
    /// <param name="model">模型</param>
    public void Copy(LovDefinitionModel model)
    {
        Id = model.Id;
        LovCode = model.LovCode;
        Name = model.Name;
        Type = model.Type;
        ValueField = model.ValueField;
        LabelField = model.LabelField;
        Source = model.Source;
        Enabled = model.Enabled;
        CreateUserID = model.CreateUserID;
        CreateIP = model.CreateIP;
        CreateTime = model.CreateTime;
        UpdateUserID = model.UpdateUserID;
        UpdateIP = model.UpdateIP;
        UpdateTime = model.UpdateTime;
        Remark = model.Remark;
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
            "LovCode" => _LovCode,
            "Name" => _Name,
            "Type" => _Type,
            "ValueField" => _ValueField,
            "LabelField" => _LabelField,
            "Source" => _Source,
            "Enabled" => _Enabled,
            "CreateUserID" => _CreateUserID,
            "CreateIP" => _CreateIP,
            "CreateTime" => _CreateTime,
            "UpdateUserID" => _UpdateUserID,
            "UpdateIP" => _UpdateIP,
            "UpdateTime" => _UpdateTime,
            "Remark" => _Remark,
            _ => base[name]
        };
        set
        {
            switch (name)
            {
                case "Id": _Id = value.ToInt(); break;
                case "LovCode": _LovCode = Convert.ToString(value); break;
                case "Name": _Name = Convert.ToString(value); break;
                case "Type": _Type = Convert.ToString(value); break;
                case "ValueField": _ValueField = Convert.ToString(value); break;
                case "LabelField": _LabelField = Convert.ToString(value); break;
                case "Source": _Source = Convert.ToString(value); break;
                case "Enabled": _Enabled = value.ToBoolean(); break;
                case "CreateUserID": _CreateUserID = value.ToInt(); break;
                case "CreateIP": _CreateIP = Convert.ToString(value); break;
                case "CreateTime": _CreateTime = value.ToDateTime(); break;
                case "UpdateUserID": _UpdateUserID = value.ToInt(); break;
                case "UpdateIP": _UpdateIP = Convert.ToString(value); break;
                case "UpdateTime": _UpdateTime = value.ToDateTime(); break;
                case "Remark": _Remark = Convert.ToString(value); break;
                default: base[name] = value; break;
            }
        }
    }
    #endregion

    #region 关联映射
    #endregion

    #region 扩展查询
    /// <summary>根据编号查找</summary>
    /// <param name="id">编号</param>
    /// <returns>实体对象</returns>
    public static LovDefinition FindById(Int32 id)
    {
        if (id < 0) return null;

        // 实体缓存
        if (Meta.Session.Count < 1000) return Meta.Cache.Find(e => e.Id == id);

        // 单对象缓存
        return Meta.SingleCache[id];

        //return Find(_.Id == id);
    }

    /// <summary>根据值集编码查找</summary>
    /// <param name="lovCode">值集编码</param>
    /// <returns>实体对象</returns>
    public static LovDefinition FindByLovCode(String lovCode)
    {
        if (lovCode.IsNullOrEmpty()) return null;

        // 实体缓存
        if (Meta.Session.Count < 1000) return Meta.Cache.Find(e => e.LovCode.EqualIgnoreCase(lovCode));

        // 单对象缓存
        return Meta.SingleCache.GetItemWithSlaveKey(lovCode) as LovDefinition;

        //return Find(_.LovCode == lovCode);
    }
    #endregion

    #region 高级查询
    /// <summary>高级查询</summary>
    /// <param name="type">值集类型。ENUM=枚举型/LIST=列表型，须与LovCode前缀一致</param>
    /// <param name="enabled">启用</param>
    /// <param name="start">更新时间开始</param>
    /// <param name="end">更新时间结束</param>
    /// <param name="key">关键字</param>
    /// <param name="page">分页参数信息。可携带统计和数据权限扩展查询等信息</param>
    /// <returns>实体列表</returns>
    public static IList<LovDefinition> Search(String type, Boolean? enabled, DateTime start, DateTime end, String key, PageParameter page)
    {
        var exp = new WhereExpression();

        if (!type.IsNullOrEmpty()) exp &= _.Type == type;
        if (enabled != null) exp &= _.Enabled == enabled;
        exp &= _.UpdateTime.Between(start, end);
        if (!key.IsNullOrEmpty()) exp &= SearchWhereByKeys(key);

        return FindAll(exp, page);
    }
    #endregion

    #region 字段名
    /// <summary>取得值集定义字段信息的快捷方式</summary>
    public partial class _
    {
        /// <summary>编号</summary>
        public static readonly Field Id = FindByName("Id");

        /// <summary>值集编码。带前缀的完整编码，如 Enum.ProcessCard.EnableStatus / List.User</summary>
        public static readonly Field LovCode = FindByName("LovCode");

        /// <summary>显示名称</summary>
        public static readonly Field Name = FindByName("Name");

        /// <summary>值集类型。ENUM=枚举型/LIST=列表型，须与LovCode前缀一致</summary>
        public static readonly Field Type = FindByName("Type");

        /// <summary>值字段。列表型的值字段名</summary>
        public static readonly Field ValueField = FindByName("ValueField");

        /// <summary>标签字段。列表型的标签字段名</summary>
        public static readonly Field LabelField = FindByName("LabelField");

        /// <summary>来源。AUTO=自动注册/MANUAL=手工管理</summary>
        public static readonly Field Source = FindByName("Source");

        /// <summary>启用</summary>
        public static readonly Field Enabled = FindByName("Enabled");

        /// <summary>创建用户</summary>
        public static readonly Field CreateUserID = FindByName("CreateUserID");

        /// <summary>创建地址</summary>
        public static readonly Field CreateIP = FindByName("CreateIP");

        /// <summary>创建时间</summary>
        public static readonly Field CreateTime = FindByName("CreateTime");

        /// <summary>更新用户</summary>
        public static readonly Field UpdateUserID = FindByName("UpdateUserID");

        /// <summary>更新地址</summary>
        public static readonly Field UpdateIP = FindByName("UpdateIP");

        /// <summary>更新时间</summary>
        public static readonly Field UpdateTime = FindByName("UpdateTime");

        /// <summary>备注</summary>
        public static readonly Field Remark = FindByName("Remark");

        static Field FindByName(String name) => Meta.Table.FindByName(name);
    }

    /// <summary>取得值集定义字段名称的快捷方式</summary>
    public partial class __
    {
        /// <summary>编号</summary>
        public const String Id = "Id";

        /// <summary>值集编码。带前缀的完整编码，如 Enum.ProcessCard.EnableStatus / List.User</summary>
        public const String LovCode = "LovCode";

        /// <summary>显示名称</summary>
        public const String Name = "Name";

        /// <summary>值集类型。ENUM=枚举型/LIST=列表型，须与LovCode前缀一致</summary>
        public const String Type = "Type";

        /// <summary>值字段。列表型的值字段名</summary>
        public const String ValueField = "ValueField";

        /// <summary>标签字段。列表型的标签字段名</summary>
        public const String LabelField = "LabelField";

        /// <summary>来源。AUTO=自动注册/MANUAL=手工管理</summary>
        public const String Source = "Source";

        /// <summary>启用</summary>
        public const String Enabled = "Enabled";

        /// <summary>创建用户</summary>
        public const String CreateUserID = "CreateUserID";

        /// <summary>创建地址</summary>
        public const String CreateIP = "CreateIP";

        /// <summary>创建时间</summary>
        public const String CreateTime = "CreateTime";

        /// <summary>更新用户</summary>
        public const String UpdateUserID = "UpdateUserID";

        /// <summary>更新地址</summary>
        public const String UpdateIP = "UpdateIP";

        /// <summary>更新时间</summary>
        public const String UpdateTime = "UpdateTime";

        /// <summary>备注</summary>
        public const String Remark = "Remark";
    }
    #endregion
}
