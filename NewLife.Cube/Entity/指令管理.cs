using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Web.Script.Serialization;
using System.Xml.Serialization;
using XCode;
using XCode.Configuration;
using XCode.DataAccessLayer;

namespace NewLife.Cube.Entity
{
    /// <summary>指令管理</summary>
    [Serializable]
    [DataObject]
    [Description("指令管理")]
    [BindIndex("IU_OrderManager_Code", true, "Code")]
    [BindTable("OrderManager", Description = "指令管理", ConnName = "Cube", DbType = DatabaseType.None)]
    public partial class OrderManager
    {
        #region 属性
        private Int32 _Id;
        /// <summary>编号</summary>
        [DisplayName("编号")]
        [Description("编号")]
        [DataObjectField(true, true, false, 0)]
        [BindColumn("Id", "编号", "")]
        public Int32 Id { get => _Id; set { if (OnPropertyChanging("Id", value)) { _Id = value; OnPropertyChanged("Id"); } } }

        private String _Name;
        /// <summary>指令名称</summary>
        [DisplayName("指令名称")]
        [Description("指令名称")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("Name", "指令名称", "", Master = true)]
        public String Name { get => _Name; set { if (OnPropertyChanging("Name", value)) { _Name = value; OnPropertyChanged("Name"); } } }

        private String _Code;
        /// <summary>指令编号</summary>
        [DisplayName("指令编号")]
        [Description("指令编号")]
        [DataObjectField(false, false, true, 100)]
        [BindColumn("Code", "指令编号", "")]
        public String Code { get => _Code; set { if (OnPropertyChanging("Code", value)) { _Code = value; OnPropertyChanged("Code"); } } }

        private String _OptCategory;
        /// <summary>操作类型</summary>
        [DisplayName("操作类型")]
        [Description("操作类型")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("OptCategory", "操作类型", "")]
        public String OptCategory { get => _OptCategory; set { if (OnPropertyChanging("OptCategory", value)) { _OptCategory = value; OnPropertyChanged("OptCategory"); } } }

        private Boolean _Enable;
        /// <summary>启用</summary>
        [DisplayName("启用")]
        [Description("启用")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("Enable", "启用", "")]
        public Boolean Enable { get => _Enable; set { if (OnPropertyChanging("Enable", value)) { _Enable = value; OnPropertyChanged("Enable"); } } }

        private String _Data;
        /// <summary>数据,进行后续操作依赖值</summary>
        [DisplayName("数据")]
        [Description("数据,进行后续操作依赖值")]
        [DataObjectField(false, false, true, 150)]
        [BindColumn("Data", "数据,进行后续操作依赖值", "")]
        public String Data { get => _Data; set { if (OnPropertyChanging("Data", value)) { _Data = value; OnPropertyChanged("Data"); } } }

        private String _DataType;
        /// <summary>数据类型,String、Int、Double、Decimal等</summary>
        [DisplayName("数据类型")]
        [Description("数据类型,String、Int、Double、Decimal等")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("DataType", "数据类型,String、Int、Double、Decimal等", "")]
        public String DataType { get => _DataType; set { if (OnPropertyChanging("DataType", value)) { _DataType = value; OnPropertyChanged("DataType"); } } }

        private String _Url;
        /// <summary>请求地址</summary>
        [DisplayName("请求地址")]
        [Description("请求地址")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("Url", "请求地址", "")]
        public String Url { get => _Url; set { if (OnPropertyChanging("Url", value)) { _Url = value; OnPropertyChanged("Url"); } } }

        private String _Method;
        /// <summary>请求方式,GET、POST、PUT、DELETE</summary>
        [DisplayName("请求方式")]
        [Description("请求方式,GET、POST、PUT、DELETE")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("Method", "请求方式,GET、POST、PUT、DELETE", "")]
        public String Method { get => _Method; set { if (OnPropertyChanging("Method", value)) { _Method = value; OnPropertyChanged("Method"); } } }

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

        private String _Remark;
        /// <summary>内容</summary>
        [Category("扩展")]
        [DisplayName("内容")]
        [Description("内容")]
        [DataObjectField(false, false, true, 500)]
        [BindColumn("Remark", "内容", "")]
        public String Remark { get => _Remark; set { if (OnPropertyChanging("Remark", value)) { _Remark = value; OnPropertyChanged("Remark"); } } }
        #endregion

        #region 获取/设置 字段值
        /// <summary>获取/设置 字段值</summary>
        /// <param name="name">字段名</param>
        /// <returns></returns>
        public override Object this[String name]
        {
            get
            {
                switch (name)
                {
                    case "Id": return _Id;
                    case "Name": return _Name;
                    case "Code": return _Code;
                    case "OptCategory": return _OptCategory;
                    case "Enable": return _Enable;
                    case "Data": return _Data;
                    case "DataType": return _DataType;
                    case "Url": return _Url;
                    case "Method": return _Method;
                    case "CreateUserId": return _CreateUserId;
                    case "CreateTime": return _CreateTime;
                    case "CreateIP": return _CreateIP;
                    case "UpdateUserId": return _UpdateUserId;
                    case "UpdateTime": return _UpdateTime;
                    case "UpdateIP": return _UpdateIP;
                    case "Remark": return _Remark;
                    default: return base[name];
                }
            }
            set
            {
                switch (name)
                {
                    case "Id": _Id = value.ToInt(); break;
                    case "Name": _Name = Convert.ToString(value); break;
                    case "Code": _Code = Convert.ToString(value); break;
                    case "OptCategory": _OptCategory = Convert.ToString(value); break;
                    case "Enable": _Enable = value.ToBoolean(); break;
                    case "Data": _Data = Convert.ToString(value); break;
                    case "DataType": _DataType = Convert.ToString(value); break;
                    case "Url": _Url = Convert.ToString(value); break;
                    case "Method": _Method = Convert.ToString(value); break;
                    case "CreateUserId": _CreateUserId = value.ToInt(); break;
                    case "CreateTime": _CreateTime = value.ToDateTime(); break;
                    case "CreateIP": _CreateIP = Convert.ToString(value); break;
                    case "UpdateUserId": _UpdateUserId = value.ToInt(); break;
                    case "UpdateTime": _UpdateTime = value.ToDateTime(); break;
                    case "UpdateIP": _UpdateIP = Convert.ToString(value); break;
                    case "Remark": _Remark = Convert.ToString(value); break;
                    default: base[name] = value; break;
                }
            }
        }
        #endregion

        #region 字段名
        /// <summary>取得指令管理字段信息的快捷方式</summary>
        public partial class _
        {
            /// <summary>编号</summary>
            public static readonly Field Id = FindByName("Id");

            /// <summary>指令名称</summary>
            public static readonly Field Name = FindByName("Name");

            /// <summary>指令编号</summary>
            public static readonly Field Code = FindByName("Code");

            /// <summary>操作类型</summary>
            public static readonly Field OptCategory = FindByName("OptCategory");

            /// <summary>启用</summary>
            public static readonly Field Enable = FindByName("Enable");

            /// <summary>数据,进行后续操作依赖值</summary>
            public static readonly Field Data = FindByName("Data");

            /// <summary>数据类型,String、Int、Double、Decimal等</summary>
            public static readonly Field DataType = FindByName("DataType");

            /// <summary>请求地址</summary>
            public static readonly Field Url = FindByName("Url");

            /// <summary>请求方式,GET、POST、PUT、DELETE</summary>
            public static readonly Field Method = FindByName("Method");

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

            /// <summary>内容</summary>
            public static readonly Field Remark = FindByName("Remark");

            static Field FindByName(String name) => Meta.Table.FindByName(name);
        }

        /// <summary>取得指令管理字段名称的快捷方式</summary>
        public partial class __
        {
            /// <summary>编号</summary>
            public const String Id = "Id";

            /// <summary>指令名称</summary>
            public const String Name = "Name";

            /// <summary>指令编号</summary>
            public const String Code = "Code";

            /// <summary>操作类型</summary>
            public const String OptCategory = "OptCategory";

            /// <summary>启用</summary>
            public const String Enable = "Enable";

            /// <summary>数据,进行后续操作依赖值</summary>
            public const String Data = "Data";

            /// <summary>数据类型,String、Int、Double、Decimal等</summary>
            public const String DataType = "DataType";

            /// <summary>请求地址</summary>
            public const String Url = "Url";

            /// <summary>请求方式,GET、POST、PUT、DELETE</summary>
            public const String Method = "Method";

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

            /// <summary>内容</summary>
            public const String Remark = "Remark";
        }
        #endregion
    }
}