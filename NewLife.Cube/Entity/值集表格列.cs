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

/// <summary>值集表格列。列表型值集的列字段定义</summary>
[Serializable]
[DataObject]
[Description("值集表格列。列表型值集的列字段定义")]
[BindIndex("IU_LovTableColumn_LovDefId_Field", true, "LovDefId,Field")]
[BindIndex("IX_LovTableColumn_LovDefId_Sort", false, "LovDefId,Sort")]
[BindTable("LovTableColumn", Description = "值集表格列。列表型值集的列字段定义", ConnName = "Cube", DbType = DatabaseType.None)]
public partial class LovTableColumn : IEntity<LovTableColumnModel>
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

    private String _Field;
    /// <summary>字段名。数据字段名</summary>
    [DisplayName("字段名")]
    [Description("字段名。数据字段名")]
    [DataObjectField(false, false, false, 50)]
    [BindColumn("Field", "字段名。数据字段名", "")]
    public String Field { get => _Field; set { if (OnPropertyChanging("Field", value)) { _Field = value; OnPropertyChanged("Field"); } } }

    private String _Title;
    /// <summary>显示标题</summary>
    [DisplayName("显示标题")]
    [Description("显示标题")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("Title", "显示标题", "", Master = true)]
    public String Title { get => _Title; set { if (OnPropertyChanging("Title", value)) { _Title = value; OnPropertyChanged("Title"); } } }

    private Int32 _Width;
    /// <summary>列宽</summary>
    [DisplayName("列宽")]
    [Description("列宽")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("Width", "列宽", "")]
    public Int32 Width { get => _Width; set { if (OnPropertyChanging("Width", value)) { _Width = value; OnPropertyChanged("Width"); } } }

    private String _Align;
    /// <summary>对齐方式。left/center/right</summary>
    [DisplayName("对齐方式")]
    [Description("对齐方式。left/center/right")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("Align", "对齐方式。left/center/right", "")]
    public String Align { get => _Align; set { if (OnPropertyChanging("Align", value)) { _Align = value; OnPropertyChanged("Align"); } } }

    private Boolean _Sortable;
    /// <summary>是否可排序</summary>
    [DisplayName("是否可排序")]
    [Description("是否可排序")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("Sortable", "是否可排序", "")]
    public Boolean Sortable { get => _Sortable; set { if (OnPropertyChanging("Sortable", value)) { _Sortable = value; OnPropertyChanged("Sortable"); } } }

    private String _RefLovCode;
    /// <summary>关联值集。该列原始值需翻译为此值集的显示文本</summary>
    [DisplayName("关联值集")]
    [Description("关联值集。该列原始值需翻译为此值集的显示文本")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("RefLovCode", "关联值集。该列原始值需翻译为此值集的显示文本", "")]
    public String RefLovCode { get => _RefLovCode; set { if (OnPropertyChanging("RefLovCode", value)) { _RefLovCode = value; OnPropertyChanged("RefLovCode"); } } }

    private String _FormatType;
    /// <summary>格式化类型。与RefLovCode互斥，如 date/amount</summary>
    [DisplayName("格式化类型")]
    [Description("格式化类型。与RefLovCode互斥，如 date/amount")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("FormatType", "格式化类型。与RefLovCode互斥，如 date/amount", "")]
    public String FormatType { get => _FormatType; set { if (OnPropertyChanging("FormatType", value)) { _FormatType = value; OnPropertyChanged("FormatType"); } } }

    private Int32 _Sort;
    /// <summary>排序</summary>
    [DisplayName("排序")]
    [Description("排序")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("Sort", "排序", "")]
    public Int32 Sort { get => _Sort; set { if (OnPropertyChanging("Sort", value)) { _Sort = value; OnPropertyChanged("Sort"); } } }

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
    public void Copy(LovTableColumnModel model)
    {
        Id = model.Id;
        LovDefId = model.LovDefId;
        Field = model.Field;
        Title = model.Title;
        Width = model.Width;
        Align = model.Align;
        Sortable = model.Sortable;
        RefLovCode = model.RefLovCode;
        FormatType = model.FormatType;
        Sort = model.Sort;
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
            "Field" => _Field,
            "Title" => _Title,
            "Width" => _Width,
            "Align" => _Align,
            "Sortable" => _Sortable,
            "RefLovCode" => _RefLovCode,
            "FormatType" => _FormatType,
            "Sort" => _Sort,
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
                case "Field": _Field = Convert.ToString(value); break;
                case "Title": _Title = Convert.ToString(value); break;
                case "Width": _Width = value.ToInt(); break;
                case "Align": _Align = Convert.ToString(value); break;
                case "Sortable": _Sortable = value.ToBoolean(); break;
                case "RefLovCode": _RefLovCode = Convert.ToString(value); break;
                case "FormatType": _FormatType = Convert.ToString(value); break;
                case "Sort": _Sort = value.ToInt(); break;
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
    public static LovTableColumn FindById(Int32 id)
    {
        if (id < 0) return null;

        // 实体缓存
        if (Meta.Session.Count < 1000) return Meta.Cache.Find(e => e.Id == id);

        // 单对象缓存
        return Meta.SingleCache[id];

        //return Find(_.Id == id);
    }

    /// <summary>根据值集定义、字段名查找</summary>
    /// <param name="lovDefId">值集定义</param>
    /// <param name="field">字段名</param>
    /// <returns>实体对象</returns>
    public static LovTableColumn FindByLovDefIdAndField(Int32 lovDefId, String field)
    {
        if (lovDefId < 0) return null;
        if (field.IsNullOrEmpty()) return null;

        // 实体缓存
        if (Meta.Session.Count < 1000) return Meta.Cache.Find(e => e.LovDefId == lovDefId && e.Field.EqualIgnoreCase(field));

        return Find(_.LovDefId == lovDefId & _.Field == field);
    }

    /// <summary>根据值集定义查找</summary>
    /// <param name="lovDefId">值集定义</param>
    /// <returns>实体列表</returns>
    public static IList<LovTableColumn> FindAllByLovDefId(Int32 lovDefId)
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
    public static IList<LovTableColumn> FindAllByLovDefIdAndSort(Int32 lovDefId, Int32 sort)
    {
        if (lovDefId < 0) return [];
        if (sort < 0) return [];

        // 实体缓存
        if (Meta.Session.Count < 1000) return Meta.Cache.FindAll(e => e.LovDefId == lovDefId && e.Sort == sort);

        return FindAll(_.LovDefId == lovDefId & _.Sort == sort);
    }
    #endregion

    #region 字段名
    /// <summary>取得值集表格列字段信息的快捷方式</summary>
    public partial class _
    {
        /// <summary>编号</summary>
        public static readonly Field Id = FindByName("Id");

        /// <summary>值集定义。关联LovDefinition</summary>
        public static readonly Field LovDefId = FindByName("LovDefId");

        /// <summary>字段名。数据字段名</summary>
        public static readonly Field Field = FindByName("Field");

        /// <summary>显示标题</summary>
        public static readonly Field Title = FindByName("Title");

        /// <summary>列宽</summary>
        public static readonly Field Width = FindByName("Width");

        /// <summary>对齐方式。left/center/right</summary>
        public static readonly Field Align = FindByName("Align");

        /// <summary>是否可排序</summary>
        public static readonly Field Sortable = FindByName("Sortable");

        /// <summary>关联值集。该列原始值需翻译为此值集的显示文本</summary>
        public static readonly Field RefLovCode = FindByName("RefLovCode");

        /// <summary>格式化类型。与RefLovCode互斥，如 date/amount</summary>
        public static readonly Field FormatType = FindByName("FormatType");

        /// <summary>排序</summary>
        public static readonly Field Sort = FindByName("Sort");

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

    /// <summary>取得值集表格列字段名称的快捷方式</summary>
    public partial class __
    {
        /// <summary>编号</summary>
        public const String Id = "Id";

        /// <summary>值集定义。关联LovDefinition</summary>
        public const String LovDefId = "LovDefId";

        /// <summary>字段名。数据字段名</summary>
        public const String Field = "Field";

        /// <summary>显示标题</summary>
        public const String Title = "Title";

        /// <summary>列宽</summary>
        public const String Width = "Width";

        /// <summary>对齐方式。left/center/right</summary>
        public const String Align = "Align";

        /// <summary>是否可排序</summary>
        public const String Sortable = "Sortable";

        /// <summary>关联值集。该列原始值需翻译为此值集的显示文本</summary>
        public const String RefLovCode = "RefLovCode";

        /// <summary>格式化类型。与RefLovCode互斥，如 date/amount</summary>
        public const String FormatType = "FormatType";

        /// <summary>排序</summary>
        public const String Sort = "Sort";

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
