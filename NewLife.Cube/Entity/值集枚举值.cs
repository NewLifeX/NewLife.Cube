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

/// <summary>值集枚举值。枚举型值集的可选值列表</summary>
[Serializable]
[DataObject]
[Description("值集枚举值。枚举型值集的可选值列表")]
[BindIndex("IU_LovEnumItem_LovDefId_Value", true, "LovDefId,Value")]
[BindIndex("IX_LovEnumItem_LovDefId_Sort", false, "LovDefId,Sort")]
[BindTable("LovEnumItem", Description = "值集枚举值。枚举型值集的可选值列表", ConnName = "Cube", DbType = DatabaseType.None)]
public partial class LovEnumItem : IEntity<LovEnumItemModel>
{
    #region 属性
    private Int32 _Id;
    /// <summary>编号</summary>
    [DisplayName("编号")]
    [Description("编号")]
    [DataObjectField(true, true, false, 0)]
    [BindColumn("Id", "编号", "")]
    public Int32 Id { get => _Id; set { if (OnPropertyChanging("Id", value)) { _Id = value; OnPropertyChanged("Id"); } } }

    private Int32 _LovDefId;
    /// <summary>值集定义。关联LovDefinition</summary>
    [DisplayName("值集定义")]
    [Description("值集定义。关联LovDefinition")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("LovDefId", "值集定义。关联LovDefinition", "")]
    public Int32 LovDefId { get => _LovDefId; set { if (OnPropertyChanging("LovDefId", value)) { _LovDefId = value; OnPropertyChanged("LovDefId"); } } }

    private String _Value;
    /// <summary>枚举值。实际存储的值</summary>
    [DisplayName("枚举值")]
    [Description("枚举值。实际存储的值")]
    [DataObjectField(false, false, false, 50)]
    [BindColumn("Value", "枚举值。实际存储的值", "")]
    public String Value { get => _Value; set { if (OnPropertyChanging("Value", value)) { _Value = value; OnPropertyChanged("Value"); } } }

    private String _Label;
    /// <summary>显示文本。枚举值对应的显示名称</summary>
    [DisplayName("显示文本")]
    [Description("显示文本。枚举值对应的显示名称")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("Label", "显示文本。枚举值对应的显示名称", "")]
    public String Label { get => _Label; set { if (OnPropertyChanging("Label", value)) { _Label = value; OnPropertyChanged("Label"); } } }

    private Int32 _Sort;
    /// <summary>排序</summary>
    [DisplayName("排序")]
    [Description("排序")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("Sort", "排序", "")]
    public Int32 Sort { get => _Sort; set { if (OnPropertyChanging("Sort", value)) { _Sort = value; OnPropertyChanged("Sort"); } } }

    private Boolean _Enabled;
    /// <summary>启用</summary>
    [DisplayName("启用")]
    [Description("启用")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("Enabled", "启用", "")]
    public Boolean Enabled { get => _Enabled; set { if (OnPropertyChanging("Enabled", value)) { _Enabled = value; OnPropertyChanged("Enabled"); } } }

    private String _Extra;
    /// <summary>扩展数据。JSON格式</summary>
    [DisplayName("扩展数据")]
    [Description("扩展数据。JSON格式")]
    [DataObjectField(false, false, true, 500)]
    [BindColumn("Extra", "扩展数据。JSON格式", "")]
    public String Extra { get => _Extra; set { if (OnPropertyChanging("Extra", value)) { _Extra = value; OnPropertyChanged("Extra"); } } }

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
    public void Copy(LovEnumItemModel model)
    {
        Id = model.Id;
        LovDefId = model.LovDefId;
        Value = model.Value;
        Label = model.Label;
        Sort = model.Sort;
        Enabled = model.Enabled;
        Extra = model.Extra;
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
            "LovDefId" => _LovDefId,
            "Value" => _Value,
            "Label" => _Label,
            "Sort" => _Sort,
            "Enabled" => _Enabled,
            "Extra" => _Extra,
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
                case "LovDefId": _LovDefId = value.ToInt(); break;
                case "Value": _Value = Convert.ToString(value); break;
                case "Label": _Label = Convert.ToString(value); break;
                case "Sort": _Sort = value.ToInt(); break;
                case "Enabled": _Enabled = value.ToBoolean(); break;
                case "Extra": _Extra = Convert.ToString(value); break;
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
    public static LovEnumItem FindById(Int32 id)
    {
        if (id < 0) return null;

        // 实体缓存
        if (Meta.Session.Count < 1000) return Meta.Cache.Find(e => e.Id == id);

        // 单对象缓存
        return Meta.SingleCache[id];

        //return Find(_.Id == id);
    }

    /// <summary>根据值集定义、枚举值查找</summary>
    /// <param name="lovDefId">值集定义</param>
    /// <param name="@value">枚举值</param>
    /// <returns>实体对象</returns>
    public static LovEnumItem FindByLovDefIdAndValue(Int32 lovDefId, String @value)
    {
        if (lovDefId < 0) return null;
        if (@value.IsNullOrEmpty()) return null;

        // 实体缓存
        if (Meta.Session.Count < 1000) return Meta.Cache.Find(e => e.LovDefId == lovDefId && e.Value.EqualIgnoreCase(@value));

        return Find(_.LovDefId == lovDefId & _.Value == @value);
    }

    /// <summary>根据值集定义查找</summary>
    /// <param name="lovDefId">值集定义</param>
    /// <returns>实体列表</returns>
    public static IList<LovEnumItem> FindAllByLovDefId(Int32 lovDefId)
    {
        if (lovDefId < 0) return [];

        // 实体缓存
        if (Meta.Session.Count < 1000) return Meta.Cache.FindAll(e => e.LovDefId == lovDefId);

        return FindAll(_.LovDefId == lovDefId);
    }

    /// <summary>根据值集定义、排序查找</summary>
    /// <param name="lovDefId">值集定义</param>
    /// <param name="sort">排序</param>
    /// <returns>实体列表</returns>
    public static IList<LovEnumItem> FindAllByLovDefIdAndSort(Int32 lovDefId, Int32 sort)
    {
        if (lovDefId < 0) return [];
        if (sort < 0) return [];

        // 实体缓存
        if (Meta.Session.Count < 1000) return Meta.Cache.FindAll(e => e.LovDefId == lovDefId && e.Sort == sort);

        return FindAll(_.LovDefId == lovDefId & _.Sort == sort);
    }
    #endregion

    #region 字段名
    /// <summary>取得值集枚举值字段信息的快捷方式</summary>
    public partial class _
    {
        /// <summary>编号</summary>
        public static readonly Field Id = FindByName("Id");

        /// <summary>值集定义。关联LovDefinition</summary>
        public static readonly Field LovDefId = FindByName("LovDefId");

        /// <summary>枚举值。实际存储的值</summary>
        public static readonly Field Value = FindByName("Value");

        /// <summary>显示文本。枚举值对应的显示名称</summary>
        public static readonly Field Label = FindByName("Label");

        /// <summary>排序</summary>
        public static readonly Field Sort = FindByName("Sort");

        /// <summary>启用</summary>
        public static readonly Field Enabled = FindByName("Enabled");

        /// <summary>扩展数据。JSON格式</summary>
        public static readonly Field Extra = FindByName("Extra");

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

    /// <summary>取得值集枚举值字段名称的快捷方式</summary>
    public partial class __
    {
        /// <summary>编号</summary>
        public const String Id = "Id";

        /// <summary>值集定义。关联LovDefinition</summary>
        public const String LovDefId = "LovDefId";

        /// <summary>枚举值。实际存储的值</summary>
        public const String Value = "Value";

        /// <summary>显示文本。枚举值对应的显示名称</summary>
        public const String Label = "Label";

        /// <summary>排序</summary>
        public const String Sort = "Sort";

        /// <summary>启用</summary>
        public const String Enabled = "Enabled";

        /// <summary>扩展数据。JSON格式</summary>
        public const String Extra = "Extra";

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
