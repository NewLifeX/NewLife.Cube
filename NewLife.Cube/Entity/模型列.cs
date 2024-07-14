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

/// <summary>模型列。实体表的数据列</summary>
[Serializable]
[DataObject]
[Description("模型列。实体表的数据列")]
[BindIndex("IU_ModelColumn_TableId_Name", true, "TableId,Name")]
[BindTable("ModelColumn", Description = "模型列。实体表的数据列", ConnName = "Cube", DbType = DatabaseType.None)]
public partial class ModelColumn
{
    #region 属性
    private Int32 _Id;
    /// <summary>编号</summary>
    [DisplayName("编号")]
    [Description("编号")]
    [DataObjectField(true, true, false, 0)]
    [BindColumn("Id", "编号", "")]
    public Int32 Id { get => _Id; set { if (OnPropertyChanging("Id", value)) { _Id = value; OnPropertyChanged("Id"); } } }

    private Int32 _TableId;
    /// <summary>模型表</summary>
    [DisplayName("模型表")]
    [Description("模型表")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("TableId", "模型表", "")]
    public Int32 TableId { get => _TableId; set { if (OnPropertyChanging("TableId", value)) { _TableId = value; OnPropertyChanged("TableId"); } } }

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

    private String _DataType;
    /// <summary>数据类型</summary>
    [DisplayName("数据类型")]
    [Description("数据类型")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("DataType", "数据类型", "")]
    public String DataType { get => _DataType; set { if (OnPropertyChanging("DataType", value)) { _DataType = value; OnPropertyChanged("DataType"); } } }

    private String _ItemType;
    /// <summary>元素类型。image,file,html,singleSelect,multipleSelect</summary>
    [DisplayName("元素类型")]
    [Description("元素类型。image,file,html,singleSelect,multipleSelect")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("ItemType", "元素类型。image,file,html,singleSelect,multipleSelect", "")]
    public String ItemType { get => _ItemType; set { if (OnPropertyChanging("ItemType", value)) { _ItemType = value; OnPropertyChanged("ItemType"); } } }

    private Boolean _PrimaryKey;
    /// <summary>主键</summary>
    [DisplayName("主键")]
    [Description("主键")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("PrimaryKey", "主键", "")]
    public Boolean PrimaryKey { get => _PrimaryKey; set { if (OnPropertyChanging("PrimaryKey", value)) { _PrimaryKey = value; OnPropertyChanged("PrimaryKey"); } } }

    private Boolean _Master;
    /// <summary>主字段。主字段作为业务主要字段，代表当前数据行意义</summary>
    [DisplayName("主字段")]
    [Description("主字段。主字段作为业务主要字段，代表当前数据行意义")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("Master", "主字段。主字段作为业务主要字段，代表当前数据行意义", "")]
    public Boolean Master { get => _Master; set { if (OnPropertyChanging("Master", value)) { _Master = value; OnPropertyChanged("Master"); } } }

    private Int32 _Length;
    /// <summary>长度</summary>
    [DisplayName("长度")]
    [Description("长度")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("Length", "长度", "")]
    public Int32 Length { get => _Length; set { if (OnPropertyChanging("Length", value)) { _Length = value; OnPropertyChanged("Length"); } } }

    private Boolean _Nullable;
    /// <summary>允许空</summary>
    [DisplayName("允许空")]
    [Description("允许空")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("Nullable", "允许空", "")]
    public Boolean Nullable { get => _Nullable; set { if (OnPropertyChanging("Nullable", value)) { _Nullable = value; OnPropertyChanged("Nullable"); } } }

    private Boolean _IsDataObjectField;
    /// <summary>数据字段</summary>
    [DisplayName("数据字段")]
    [Description("数据字段")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("IsDataObjectField", "数据字段", "")]
    public Boolean IsDataObjectField { get => _IsDataObjectField; set { if (OnPropertyChanging("IsDataObjectField", value)) { _IsDataObjectField = value; OnPropertyChanged("IsDataObjectField"); } } }

    private String _Description;
    /// <summary>说明</summary>
    [DisplayName("说明")]
    [Description("说明")]
    [DataObjectField(false, false, true, 500)]
    [BindColumn("Description", "说明", "")]
    public String Description { get => _Description; set { if (OnPropertyChanging("Description", value)) { _Description = value; OnPropertyChanged("Description"); } } }

    private Boolean _ShowInList;
    /// <summary>列表页显示</summary>
    [DisplayName("列表页显示")]
    [Description("列表页显示")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("ShowInList", "列表页显示", "")]
    public Boolean ShowInList { get => _ShowInList; set { if (OnPropertyChanging("ShowInList", value)) { _ShowInList = value; OnPropertyChanged("ShowInList"); } } }

    private Boolean _ShowInAddForm;
    /// <summary>添加表单页显示</summary>
    [DisplayName("添加表单页显示")]
    [Description("添加表单页显示")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("ShowInAddForm", "添加表单页显示", "")]
    public Boolean ShowInAddForm { get => _ShowInAddForm; set { if (OnPropertyChanging("ShowInAddForm", value)) { _ShowInAddForm = value; OnPropertyChanged("ShowInAddForm"); } } }

    private Boolean _ShowInEditForm;
    /// <summary>编辑表单页显示</summary>
    [DisplayName("编辑表单页显示")]
    [Description("编辑表单页显示")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("ShowInEditForm", "编辑表单页显示", "")]
    public Boolean ShowInEditForm { get => _ShowInEditForm; set { if (OnPropertyChanging("ShowInEditForm", value)) { _ShowInEditForm = value; OnPropertyChanged("ShowInEditForm"); } } }

    private Boolean _ShowInDetailForm;
    /// <summary>详情表单页显示</summary>
    [DisplayName("详情表单页显示")]
    [Description("详情表单页显示")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("ShowInDetailForm", "详情表单页显示", "")]
    public Boolean ShowInDetailForm { get => _ShowInDetailForm; set { if (OnPropertyChanging("ShowInDetailForm", value)) { _ShowInDetailForm = value; OnPropertyChanged("ShowInDetailForm"); } } }

    private Boolean _ShowInSearch;
    /// <summary>搜索显示</summary>
    [DisplayName("搜索显示")]
    [Description("搜索显示")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("ShowInSearch", "搜索显示", "")]
    public Boolean ShowInSearch { get => _ShowInSearch; set { if (OnPropertyChanging("ShowInSearch", value)) { _ShowInSearch = value; OnPropertyChanged("ShowInSearch"); } } }

    private Int32 _Sort;
    /// <summary>排序</summary>
    [DisplayName("排序")]
    [Description("排序")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("Sort", "排序", "")]
    public Int32 Sort { get => _Sort; set { if (OnPropertyChanging("Sort", value)) { _Sort = value; OnPropertyChanged("Sort"); } } }

    private String _Width;
    /// <summary>宽度</summary>
    [DisplayName("宽度")]
    [Description("宽度")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("Width", "宽度", "")]
    public String Width { get => _Width; set { if (OnPropertyChanging("Width", value)) { _Width = value; OnPropertyChanged("Width"); } } }

    private String _CellText;
    /// <summary>单元格文字</summary>
    [DisplayName("单元格文字")]
    [Description("单元格文字")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("CellText", "单元格文字", "")]
    public String CellText { get => _CellText; set { if (OnPropertyChanging("CellText", value)) { _CellText = value; OnPropertyChanged("CellText"); } } }

    private String _CellTitle;
    /// <summary>单元格标题。数据单元格上的提示文字</summary>
    [DisplayName("单元格标题")]
    [Description("单元格标题。数据单元格上的提示文字")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("CellTitle", "单元格标题。数据单元格上的提示文字", "")]
    public String CellTitle { get => _CellTitle; set { if (OnPropertyChanging("CellTitle", value)) { _CellTitle = value; OnPropertyChanged("CellTitle"); } } }

    private String _CellUrl;
    /// <summary>单元格链接。数据单元格的链接</summary>
    [DisplayName("单元格链接")]
    [Description("单元格链接。数据单元格的链接")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("CellUrl", "单元格链接。数据单元格的链接", "")]
    public String CellUrl { get => _CellUrl; set { if (OnPropertyChanging("CellUrl", value)) { _CellUrl = value; OnPropertyChanged("CellUrl"); } } }

    private String _HeaderText;
    /// <summary>头部文字</summary>
    [DisplayName("头部文字")]
    [Description("头部文字")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("HeaderText", "头部文字", "")]
    public String HeaderText { get => _HeaderText; set { if (OnPropertyChanging("HeaderText", value)) { _HeaderText = value; OnPropertyChanged("HeaderText"); } } }

    private String _HeaderTitle;
    /// <summary>头部标题。数据移上去后显示的文字</summary>
    [DisplayName("头部标题")]
    [Description("头部标题。数据移上去后显示的文字")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("HeaderTitle", "头部标题。数据移上去后显示的文字", "")]
    public String HeaderTitle { get => _HeaderTitle; set { if (OnPropertyChanging("HeaderTitle", value)) { _HeaderTitle = value; OnPropertyChanged("HeaderTitle"); } } }

    private String _HeaderUrl;
    /// <summary>头部链接。一般是排序</summary>
    [DisplayName("头部链接")]
    [Description("头部链接。一般是排序")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("HeaderUrl", "头部链接。一般是排序", "")]
    public String HeaderUrl { get => _HeaderUrl; set { if (OnPropertyChanging("HeaderUrl", value)) { _HeaderUrl = value; OnPropertyChanged("HeaderUrl"); } } }

    private String _DataAction;
    /// <summary>数据动作。设为action时走ajax请求</summary>
    [DisplayName("数据动作")]
    [Description("数据动作。设为action时走ajax请求")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("DataAction", "数据动作。设为action时走ajax请求", "")]
    public String DataAction { get => _DataAction; set { if (OnPropertyChanging("DataAction", value)) { _DataAction = value; OnPropertyChanged("DataAction"); } } }

    private String _DataSource;
    /// <summary>多选数据源</summary>
    [DisplayName("多选数据源")]
    [Description("多选数据源")]
    [DataObjectField(false, false, true, 500)]
    [BindColumn("DataSource", "多选数据源", "")]
    public String DataSource { get => _DataSource; set { if (OnPropertyChanging("DataSource", value)) { _DataSource = value; OnPropertyChanged("DataSource"); } } }

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

    #region 获取/设置 字段值
    /// <summary>获取/设置 字段值</summary>
    /// <param name="name">字段名</param>
    /// <returns></returns>
    public override Object this[String name]
    {
        get => name switch
        {
            "Id" => _Id,
            "TableId" => _TableId,
            "Name" => _Name,
            "DisplayName" => _DisplayName,
            "Enable" => _Enable,
            "DataType" => _DataType,
            "ItemType" => _ItemType,
            "PrimaryKey" => _PrimaryKey,
            "Master" => _Master,
            "Length" => _Length,
            "Nullable" => _Nullable,
            "IsDataObjectField" => _IsDataObjectField,
            "Description" => _Description,
            "ShowInList" => _ShowInList,
            "ShowInAddForm" => _ShowInAddForm,
            "ShowInEditForm" => _ShowInEditForm,
            "ShowInDetailForm" => _ShowInDetailForm,
            "ShowInSearch" => _ShowInSearch,
            "Sort" => _Sort,
            "Width" => _Width,
            "CellText" => _CellText,
            "CellTitle" => _CellTitle,
            "CellUrl" => _CellUrl,
            "HeaderText" => _HeaderText,
            "HeaderTitle" => _HeaderTitle,
            "HeaderUrl" => _HeaderUrl,
            "DataAction" => _DataAction,
            "DataSource" => _DataSource,
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
                case "TableId": _TableId = value.ToInt(); break;
                case "Name": _Name = Convert.ToString(value); break;
                case "DisplayName": _DisplayName = Convert.ToString(value); break;
                case "Enable": _Enable = value.ToBoolean(); break;
                case "DataType": _DataType = Convert.ToString(value); break;
                case "ItemType": _ItemType = Convert.ToString(value); break;
                case "PrimaryKey": _PrimaryKey = value.ToBoolean(); break;
                case "Master": _Master = value.ToBoolean(); break;
                case "Length": _Length = value.ToInt(); break;
                case "Nullable": _Nullable = value.ToBoolean(); break;
                case "IsDataObjectField": _IsDataObjectField = value.ToBoolean(); break;
                case "Description": _Description = Convert.ToString(value); break;
                case "ShowInList": _ShowInList = value.ToBoolean(); break;
                case "ShowInAddForm": _ShowInAddForm = value.ToBoolean(); break;
                case "ShowInEditForm": _ShowInEditForm = value.ToBoolean(); break;
                case "ShowInDetailForm": _ShowInDetailForm = value.ToBoolean(); break;
                case "ShowInSearch": _ShowInSearch = value.ToBoolean(); break;
                case "Sort": _Sort = value.ToInt(); break;
                case "Width": _Width = Convert.ToString(value); break;
                case "CellText": _CellText = Convert.ToString(value); break;
                case "CellTitle": _CellTitle = Convert.ToString(value); break;
                case "CellUrl": _CellUrl = Convert.ToString(value); break;
                case "HeaderText": _HeaderText = Convert.ToString(value); break;
                case "HeaderTitle": _HeaderTitle = Convert.ToString(value); break;
                case "HeaderUrl": _HeaderUrl = Convert.ToString(value); break;
                case "DataAction": _DataAction = Convert.ToString(value); break;
                case "DataSource": _DataSource = Convert.ToString(value); break;
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
    #endregion

    #region 字段名
    /// <summary>取得模型列字段信息的快捷方式</summary>
    public partial class _
    {
        /// <summary>编号</summary>
        public static readonly Field Id = FindByName("Id");

        /// <summary>模型表</summary>
        public static readonly Field TableId = FindByName("TableId");

        /// <summary>名称</summary>
        public static readonly Field Name = FindByName("Name");

        /// <summary>显示名</summary>
        public static readonly Field DisplayName = FindByName("DisplayName");

        /// <summary>启用</summary>
        public static readonly Field Enable = FindByName("Enable");

        /// <summary>数据类型</summary>
        public static readonly Field DataType = FindByName("DataType");

        /// <summary>元素类型。image,file,html,singleSelect,multipleSelect</summary>
        public static readonly Field ItemType = FindByName("ItemType");

        /// <summary>主键</summary>
        public static readonly Field PrimaryKey = FindByName("PrimaryKey");

        /// <summary>主字段。主字段作为业务主要字段，代表当前数据行意义</summary>
        public static readonly Field Master = FindByName("Master");

        /// <summary>长度</summary>
        public static readonly Field Length = FindByName("Length");

        /// <summary>允许空</summary>
        public static readonly Field Nullable = FindByName("Nullable");

        /// <summary>数据字段</summary>
        public static readonly Field IsDataObjectField = FindByName("IsDataObjectField");

        /// <summary>说明</summary>
        public static readonly Field Description = FindByName("Description");

        /// <summary>列表页显示</summary>
        public static readonly Field ShowInList = FindByName("ShowInList");

        /// <summary>添加表单页显示</summary>
        public static readonly Field ShowInAddForm = FindByName("ShowInAddForm");

        /// <summary>编辑表单页显示</summary>
        public static readonly Field ShowInEditForm = FindByName("ShowInEditForm");

        /// <summary>详情表单页显示</summary>
        public static readonly Field ShowInDetailForm = FindByName("ShowInDetailForm");

        /// <summary>搜索显示</summary>
        public static readonly Field ShowInSearch = FindByName("ShowInSearch");

        /// <summary>排序</summary>
        public static readonly Field Sort = FindByName("Sort");

        /// <summary>宽度</summary>
        public static readonly Field Width = FindByName("Width");

        /// <summary>单元格文字</summary>
        public static readonly Field CellText = FindByName("CellText");

        /// <summary>单元格标题。数据单元格上的提示文字</summary>
        public static readonly Field CellTitle = FindByName("CellTitle");

        /// <summary>单元格链接。数据单元格的链接</summary>
        public static readonly Field CellUrl = FindByName("CellUrl");

        /// <summary>头部文字</summary>
        public static readonly Field HeaderText = FindByName("HeaderText");

        /// <summary>头部标题。数据移上去后显示的文字</summary>
        public static readonly Field HeaderTitle = FindByName("HeaderTitle");

        /// <summary>头部链接。一般是排序</summary>
        public static readonly Field HeaderUrl = FindByName("HeaderUrl");

        /// <summary>数据动作。设为action时走ajax请求</summary>
        public static readonly Field DataAction = FindByName("DataAction");

        /// <summary>多选数据源</summary>
        public static readonly Field DataSource = FindByName("DataSource");

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

    /// <summary>取得模型列字段名称的快捷方式</summary>
    public partial class __
    {
        /// <summary>编号</summary>
        public const String Id = "Id";

        /// <summary>模型表</summary>
        public const String TableId = "TableId";

        /// <summary>名称</summary>
        public const String Name = "Name";

        /// <summary>显示名</summary>
        public const String DisplayName = "DisplayName";

        /// <summary>启用</summary>
        public const String Enable = "Enable";

        /// <summary>数据类型</summary>
        public const String DataType = "DataType";

        /// <summary>元素类型。image,file,html,singleSelect,multipleSelect</summary>
        public const String ItemType = "ItemType";

        /// <summary>主键</summary>
        public const String PrimaryKey = "PrimaryKey";

        /// <summary>主字段。主字段作为业务主要字段，代表当前数据行意义</summary>
        public const String Master = "Master";

        /// <summary>长度</summary>
        public const String Length = "Length";

        /// <summary>允许空</summary>
        public const String Nullable = "Nullable";

        /// <summary>数据字段</summary>
        public const String IsDataObjectField = "IsDataObjectField";

        /// <summary>说明</summary>
        public const String Description = "Description";

        /// <summary>列表页显示</summary>
        public const String ShowInList = "ShowInList";

        /// <summary>添加表单页显示</summary>
        public const String ShowInAddForm = "ShowInAddForm";

        /// <summary>编辑表单页显示</summary>
        public const String ShowInEditForm = "ShowInEditForm";

        /// <summary>详情表单页显示</summary>
        public const String ShowInDetailForm = "ShowInDetailForm";

        /// <summary>搜索显示</summary>
        public const String ShowInSearch = "ShowInSearch";

        /// <summary>排序</summary>
        public const String Sort = "Sort";

        /// <summary>宽度</summary>
        public const String Width = "Width";

        /// <summary>单元格文字</summary>
        public const String CellText = "CellText";

        /// <summary>单元格标题。数据单元格上的提示文字</summary>
        public const String CellTitle = "CellTitle";

        /// <summary>单元格链接。数据单元格的链接</summary>
        public const String CellUrl = "CellUrl";

        /// <summary>头部文字</summary>
        public const String HeaderText = "HeaderText";

        /// <summary>头部标题。数据移上去后显示的文字</summary>
        public const String HeaderTitle = "HeaderTitle";

        /// <summary>头部链接。一般是排序</summary>
        public const String HeaderUrl = "HeaderUrl";

        /// <summary>数据动作。设为action时走ajax请求</summary>
        public const String DataAction = "DataAction";

        /// <summary>多选数据源</summary>
        public const String DataSource = "DataSource";

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
