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

namespace NewLife.School.Entity;

/// <summary>学生</summary>
[Serializable]
[DataObject]
[Description("学生")]
[BindIndex("IX_Student_TenantId_ClassId", false, "TenantId,ClassId")]
[BindIndex("IX_Student_ClassId", false, "ClassId")]
[BindTable("Student", Description = "学生", ConnName = "School", DbType = DatabaseType.SqlServer)]
public partial class Student : IStudent, IEntity<IStudent>
{
    #region 属性
    private Int32 _Id;
    /// <summary>编号</summary>
    [DisplayName("编号")]
    [Description("编号")]
    [DataObjectField(true, true, false, 0)]
    [BindColumn("Id", "编号", "")]
    public Int32 Id { get => _Id; set { if (OnPropertyChanging("Id", value)) { _Id = value; OnPropertyChanged("Id"); } } }

    private Int32 _TenantId;
    /// <summary>租户</summary>
    [DisplayName("租户")]
    [Description("租户")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("TenantId", "租户", "")]
    public Int32 TenantId { get => _TenantId; set { if (OnPropertyChanging("TenantId", value)) { _TenantId = value; OnPropertyChanged("TenantId"); } } }

    private Int32 _ClassId;
    /// <summary>班级</summary>
    [Category("基本信息")]
    [DisplayName("班级")]
    [Description("班级")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("ClassId", "班级", "")]
    public Int32 ClassId { get => _ClassId; set { if (OnPropertyChanging("ClassId", value)) { _ClassId = value; OnPropertyChanged("ClassId"); } } }

    private String _Name;
    /// <summary>名称</summary>
    [Category("基本信息")]
    [DisplayName("名称")]
    [Description("名称")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("Name", "名称", "", Master = true)]
    public String Name { get => _Name; set { if (OnPropertyChanging("Name", value)) { _Name = value; OnPropertyChanged("Name"); } } }

    private XCode.Membership.SexKinds _Sex;
    /// <summary>性别</summary>
    [Category("基本信息")]
    [DisplayName("性别")]
    [Description("性别")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("Sex", "性别", "")]
    public XCode.Membership.SexKinds Sex { get => _Sex; set { if (OnPropertyChanging("Sex", value)) { _Sex = value; OnPropertyChanged("Sex"); } } }

    private Int32 _Age;
    /// <summary>年龄</summary>
    [Category("基本信息")]
    [DisplayName("年龄")]
    [Description("年龄")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("Age", "年龄", "")]
    public Int32 Age { get => _Age; set { if (OnPropertyChanging("Age", value)) { _Age = value; OnPropertyChanged("Age"); } } }

    private String _Mobile;
    /// <summary>手机</summary>
    [Category("基本信息")]
    [DisplayName("手机")]
    [Description("手机")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("Mobile", "手机", "")]
    public String Mobile { get => _Mobile; set { if (OnPropertyChanging("Mobile", value)) { _Mobile = value; OnPropertyChanged("Mobile"); } } }

    private String _Address;
    /// <summary>地址</summary>
    [Category("基本信息")]
    [DisplayName("地址")]
    [Description("地址")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("Address", "地址", "")]
    public String Address { get => _Address; set { if (OnPropertyChanging("Address", value)) { _Address = value; OnPropertyChanged("Address"); } } }

    private Boolean _Enable;
    /// <summary>启用</summary>
    [Category("基本信息")]
    [DisplayName("启用")]
    [Description("启用")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("Enable", "启用", "")]
    public Boolean Enable { get => _Enable; set { if (OnPropertyChanging("Enable", value)) { _Enable = value; OnPropertyChanged("Enable"); } } }

    private String _Avatar;
    /// <summary>头像</summary>
    [Category("基本信息")]
    [DisplayName("头像")]
    [Description("头像")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("Avatar", "头像", "", ItemType = "image")]
    public String Avatar { get => _Avatar; set { if (OnPropertyChanging("Avatar", value)) { _Avatar = value; OnPropertyChanged("Avatar"); } } }

    private Double _Weight;
    /// <summary>体重。小数</summary>
    [DisplayName("体重")]
    [Description("体重。小数")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("Weight", "体重。小数", "", Precision = 0, Scale = 2)]
    public Double Weight { get => _Weight; set { if (OnPropertyChanging("Weight", value)) { _Weight = value; OnPropertyChanged("Weight"); } } }

    private Decimal _Amount;
    /// <summary>存款。小数</summary>
    [DisplayName("存款")]
    [Description("存款。小数")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("Amount", "存款。小数", "", Precision = 0, Scale = 3)]
    public Decimal Amount { get => _Amount; set { if (OnPropertyChanging("Amount", value)) { _Amount = value; OnPropertyChanged("Amount"); } } }

    private Int32 _CreateUserID;
    /// <summary>创建者</summary>
    [Category("扩展信息")]
    [DisplayName("创建者")]
    [Description("创建者")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("CreateUserID", "创建者", "")]
    public Int32 CreateUserID { get => _CreateUserID; set { if (OnPropertyChanging("CreateUserID", value)) { _CreateUserID = value; OnPropertyChanged("CreateUserID"); } } }

    private DateTime _CreateTime;
    /// <summary>创建时间</summary>
    [Category("扩展信息")]
    [DisplayName("创建时间")]
    [Description("创建时间")]
    [DataObjectField(false, false, true, 0)]
    [BindColumn("CreateTime", "创建时间", "")]
    public DateTime CreateTime { get => _CreateTime; set { if (OnPropertyChanging("CreateTime", value)) { _CreateTime = value; OnPropertyChanged("CreateTime"); } } }

    private String _CreateIP;
    /// <summary>创建地址</summary>
    [Category("扩展信息")]
    [DisplayName("创建地址")]
    [Description("创建地址")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("CreateIP", "创建地址", "")]
    public String CreateIP { get => _CreateIP; set { if (OnPropertyChanging("CreateIP", value)) { _CreateIP = value; OnPropertyChanged("CreateIP"); } } }

    private Int32 _UpdateUserID;
    /// <summary>更新者</summary>
    [Category("扩展信息")]
    [DisplayName("更新者")]
    [Description("更新者")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("UpdateUserID", "更新者", "")]
    public Int32 UpdateUserID { get => _UpdateUserID; set { if (OnPropertyChanging("UpdateUserID", value)) { _UpdateUserID = value; OnPropertyChanged("UpdateUserID"); } } }

    private DateTime _UpdateTime;
    /// <summary>更新时间</summary>
    [Category("扩展信息")]
    [DisplayName("更新时间")]
    [Description("更新时间")]
    [DataObjectField(false, false, true, 0)]
    [BindColumn("UpdateTime", "更新时间", "")]
    public DateTime UpdateTime { get => _UpdateTime; set { if (OnPropertyChanging("UpdateTime", value)) { _UpdateTime = value; OnPropertyChanged("UpdateTime"); } } }

    private String _UpdateIP;
    /// <summary>更新地址</summary>
    [Category("扩展信息")]
    [DisplayName("更新地址")]
    [Description("更新地址")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("UpdateIP", "更新地址", "")]
    public String UpdateIP { get => _UpdateIP; set { if (OnPropertyChanging("UpdateIP", value)) { _UpdateIP = value; OnPropertyChanged("UpdateIP"); } } }

    private String _Remark;
    /// <summary>备注</summary>
    [Category("扩展信息")]
    [DisplayName("备注")]
    [Description("备注")]
    [DataObjectField(false, false, true, 200)]
    [BindColumn("Remark", "备注", "")]
    public String Remark { get => _Remark; set { if (OnPropertyChanging("Remark", value)) { _Remark = value; OnPropertyChanged("Remark"); } } }
    #endregion

    #region 拷贝
    /// <summary>拷贝模型对象</summary>
    /// <param name="model">模型</param>
    public void Copy(IStudent model)
    {
        Id = model.Id;
        TenantId = model.TenantId;
        ClassId = model.ClassId;
        Name = model.Name;
        Sex = model.Sex;
        Age = model.Age;
        Mobile = model.Mobile;
        Address = model.Address;
        Enable = model.Enable;
        Avatar = model.Avatar;
        Weight = model.Weight;
        Amount = model.Amount;
        CreateUserID = model.CreateUserID;
        CreateTime = model.CreateTime;
        CreateIP = model.CreateIP;
        UpdateUserID = model.UpdateUserID;
        UpdateTime = model.UpdateTime;
        UpdateIP = model.UpdateIP;
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
            "TenantId" => _TenantId,
            "ClassId" => _ClassId,
            "Name" => _Name,
            "Sex" => _Sex,
            "Age" => _Age,
            "Mobile" => _Mobile,
            "Address" => _Address,
            "Enable" => _Enable,
            "Avatar" => _Avatar,
            "Weight" => _Weight,
            "Amount" => _Amount,
            "CreateUserID" => _CreateUserID,
            "CreateTime" => _CreateTime,
            "CreateIP" => _CreateIP,
            "UpdateUserID" => _UpdateUserID,
            "UpdateTime" => _UpdateTime,
            "UpdateIP" => _UpdateIP,
            "Remark" => _Remark,
            _ => base[name]
        };
        set
        {
            switch (name)
            {
                case "Id": _Id = value.ToInt(); break;
                case "TenantId": _TenantId = value.ToInt(); break;
                case "ClassId": _ClassId = value.ToInt(); break;
                case "Name": _Name = Convert.ToString(value); break;
                case "Sex": _Sex = (XCode.Membership.SexKinds)value.ToInt(); break;
                case "Age": _Age = value.ToInt(); break;
                case "Mobile": _Mobile = Convert.ToString(value); break;
                case "Address": _Address = Convert.ToString(value); break;
                case "Enable": _Enable = value.ToBoolean(); break;
                case "Avatar": _Avatar = Convert.ToString(value); break;
                case "Weight": _Weight = value.ToDouble(); break;
                case "Amount": _Amount = Convert.ToDecimal(value); break;
                case "CreateUserID": _CreateUserID = value.ToInt(); break;
                case "CreateTime": _CreateTime = value.ToDateTime(); break;
                case "CreateIP": _CreateIP = Convert.ToString(value); break;
                case "UpdateUserID": _UpdateUserID = value.ToInt(); break;
                case "UpdateTime": _UpdateTime = value.ToDateTime(); break;
                case "UpdateIP": _UpdateIP = Convert.ToString(value); break;
                case "Remark": _Remark = Convert.ToString(value); break;
                default: base[name] = value; break;
            }
        }
    }
    #endregion

    #region 关联映射
    /// <summary>租户</summary>
    [XmlIgnore, IgnoreDataMember, ScriptIgnore]
    public XCode.Membership.Tenant Tenant => Extends.Get(nameof(Tenant), k => XCode.Membership.Tenant.FindById(TenantId));

    /// <summary>租户</summary>
    [Map(nameof(TenantId), typeof(XCode.Membership.Tenant), "Id")]
    public String TenantName => Tenant?.Name;

    /// <summary>班级</summary>
    [XmlIgnore, IgnoreDataMember, ScriptIgnore]
    public Class Class => Extends.Get(nameof(Class), k => Class.FindById(ClassId));

    /// <summary>班级</summary>
    [Map(nameof(ClassId), typeof(Class), "Id")]
    [Category("基本信息")]
    public String ClassName => Class?.ToString();

    #endregion

    #region 字段名
    /// <summary>取得学生字段信息的快捷方式</summary>
    public partial class _
    {
        /// <summary>编号</summary>
        public static readonly Field Id = FindByName("Id");

        /// <summary>租户</summary>
        public static readonly Field TenantId = FindByName("TenantId");

        /// <summary>班级</summary>
        public static readonly Field ClassId = FindByName("ClassId");

        /// <summary>名称</summary>
        public static readonly Field Name = FindByName("Name");

        /// <summary>性别</summary>
        public static readonly Field Sex = FindByName("Sex");

        /// <summary>年龄</summary>
        public static readonly Field Age = FindByName("Age");

        /// <summary>手机</summary>
        public static readonly Field Mobile = FindByName("Mobile");

        /// <summary>地址</summary>
        public static readonly Field Address = FindByName("Address");

        /// <summary>启用</summary>
        public static readonly Field Enable = FindByName("Enable");

        /// <summary>头像</summary>
        public static readonly Field Avatar = FindByName("Avatar");

        /// <summary>体重。小数</summary>
        public static readonly Field Weight = FindByName("Weight");

        /// <summary>存款。小数</summary>
        public static readonly Field Amount = FindByName("Amount");

        /// <summary>创建者</summary>
        public static readonly Field CreateUserID = FindByName("CreateUserID");

        /// <summary>创建时间</summary>
        public static readonly Field CreateTime = FindByName("CreateTime");

        /// <summary>创建地址</summary>
        public static readonly Field CreateIP = FindByName("CreateIP");

        /// <summary>更新者</summary>
        public static readonly Field UpdateUserID = FindByName("UpdateUserID");

        /// <summary>更新时间</summary>
        public static readonly Field UpdateTime = FindByName("UpdateTime");

        /// <summary>更新地址</summary>
        public static readonly Field UpdateIP = FindByName("UpdateIP");

        /// <summary>备注</summary>
        public static readonly Field Remark = FindByName("Remark");

        static Field FindByName(String name) => Meta.Table.FindByName(name);
    }

    /// <summary>取得学生字段名称的快捷方式</summary>
    public partial class __
    {
        /// <summary>编号</summary>
        public const String Id = "Id";

        /// <summary>租户</summary>
        public const String TenantId = "TenantId";

        /// <summary>班级</summary>
        public const String ClassId = "ClassId";

        /// <summary>名称</summary>
        public const String Name = "Name";

        /// <summary>性别</summary>
        public const String Sex = "Sex";

        /// <summary>年龄</summary>
        public const String Age = "Age";

        /// <summary>手机</summary>
        public const String Mobile = "Mobile";

        /// <summary>地址</summary>
        public const String Address = "Address";

        /// <summary>启用</summary>
        public const String Enable = "Enable";

        /// <summary>头像</summary>
        public const String Avatar = "Avatar";

        /// <summary>体重。小数</summary>
        public const String Weight = "Weight";

        /// <summary>存款。小数</summary>
        public const String Amount = "Amount";

        /// <summary>创建者</summary>
        public const String CreateUserID = "CreateUserID";

        /// <summary>创建时间</summary>
        public const String CreateTime = "CreateTime";

        /// <summary>创建地址</summary>
        public const String CreateIP = "CreateIP";

        /// <summary>更新者</summary>
        public const String UpdateUserID = "UpdateUserID";

        /// <summary>更新时间</summary>
        public const String UpdateTime = "UpdateTime";

        /// <summary>更新地址</summary>
        public const String UpdateIP = "UpdateIP";

        /// <summary>备注</summary>
        public const String Remark = "Remark";
    }
    #endregion
}
