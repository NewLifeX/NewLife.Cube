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
    /// <summary>定时作业。定时执行任务</summary>
    [Serializable]
    [DataObject]
    [Description("定时作业。定时执行任务")]
    [BindIndex("IU_CronJob_Name", true, "Name")]
    [BindTable("CronJob", Description = "定时作业。定时执行任务", ConnName = "Cube", DbType = DatabaseType.None)]
    public partial class CronJob
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

        private String _Cron;
        /// <summary>Cron表达式。用于定时执行的Cron表达式</summary>
        [DisplayName("Cron表达式")]
        [Description("Cron表达式。用于定时执行的Cron表达式")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("Cron", "Cron表达式。用于定时执行的Cron表达式", "")]
        public String Cron { get => _Cron; set { if (OnPropertyChanging("Cron", value)) { _Cron = value; OnPropertyChanged("Cron"); } } }

        private String _Method;
        /// <summary>命令。作业方法全名，含命名空间和类名，静态方法，包含一个String参数</summary>
        [DisplayName("命令")]
        [Description("命令。作业方法全名，含命名空间和类名，静态方法，包含一个String参数")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("Method", "命令。作业方法全名，含命名空间和类名，静态方法，包含一个String参数", "")]
        public String Method { get => _Method; set { if (OnPropertyChanging("Method", value)) { _Method = value; OnPropertyChanged("Method"); } } }

        private String _Argument;
        /// <summary>参数。方法参数，时间日期、网址、SQL等</summary>
        [DisplayName("参数")]
        [Description("参数。方法参数，时间日期、网址、SQL等")]
        [DataObjectField(false, false, true, 2000)]
        [BindColumn("Argument", "参数。方法参数，时间日期、网址、SQL等", "")]
        public String Argument { get => _Argument; set { if (OnPropertyChanging("Argument", value)) { _Argument = value; OnPropertyChanged("Argument"); } } }

        private Boolean _Enable;
        /// <summary>启用</summary>
        [DisplayName("启用")]
        [Description("启用")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("Enable", "启用", "")]
        public Boolean Enable { get => _Enable; set { if (OnPropertyChanging("Enable", value)) { _Enable = value; OnPropertyChanged("Enable"); } } }

        private DateTime _LastTime;
        /// <summary>最后时间。最后一次执行作业的时间</summary>
        [DisplayName("最后时间")]
        [Description("最后时间。最后一次执行作业的时间")]
        [DataObjectField(false, false, true, 0)]
        [BindColumn("LastTime", "最后时间。最后一次执行作业的时间", "")]
        public DateTime LastTime { get => _LastTime; set { if (OnPropertyChanging("LastTime", value)) { _LastTime = value; OnPropertyChanged("LastTime"); } } }

        private DateTime _NextTime;
        /// <summary>下一次时间。下一次执行作业的时间</summary>
        [DisplayName("下一次时间")]
        [Description("下一次时间。下一次执行作业的时间")]
        [DataObjectField(false, false, true, 0)]
        [BindColumn("NextTime", "下一次时间。下一次执行作业的时间", "")]
        public DateTime NextTime { get => _NextTime; set { if (OnPropertyChanging("NextTime", value)) { _NextTime = value; OnPropertyChanged("NextTime"); } } }

        private String _Remark;
        /// <summary>内容</summary>
        [DisplayName("内容")]
        [Description("内容")]
        [DataObjectField(false, false, true, 500)]
        [BindColumn("Remark", "内容", "")]
        public String Remark { get => _Remark; set { if (OnPropertyChanging("Remark", value)) { _Remark = value; OnPropertyChanged("Remark"); } } }

        private Int32 _CreateUserID;
        /// <summary>创建者</summary>
        [DisplayName("创建者")]
        [Description("创建者")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("CreateUserID", "创建者", "")]
        public Int32 CreateUserID { get => _CreateUserID; set { if (OnPropertyChanging("CreateUserID", value)) { _CreateUserID = value; OnPropertyChanged("CreateUserID"); } } }

        private DateTime _CreateTime;
        /// <summary>创建时间</summary>
        [DisplayName("创建时间")]
        [Description("创建时间")]
        [DataObjectField(false, false, true, 0)]
        [BindColumn("CreateTime", "创建时间", "")]
        public DateTime CreateTime { get => _CreateTime; set { if (OnPropertyChanging("CreateTime", value)) { _CreateTime = value; OnPropertyChanged("CreateTime"); } } }

        private String _CreateIP;
        /// <summary>创建地址</summary>
        [DisplayName("创建地址")]
        [Description("创建地址")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("CreateIP", "创建地址", "")]
        public String CreateIP { get => _CreateIP; set { if (OnPropertyChanging("CreateIP", value)) { _CreateIP = value; OnPropertyChanged("CreateIP"); } } }

        private Int32 _UpdateUserID;
        /// <summary>更新者</summary>
        [DisplayName("更新者")]
        [Description("更新者")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("UpdateUserID", "更新者", "")]
        public Int32 UpdateUserID { get => _UpdateUserID; set { if (OnPropertyChanging("UpdateUserID", value)) { _UpdateUserID = value; OnPropertyChanged("UpdateUserID"); } } }

        private DateTime _UpdateTime;
        /// <summary>更新时间</summary>
        [DisplayName("更新时间")]
        [Description("更新时间")]
        [DataObjectField(false, false, true, 0)]
        [BindColumn("UpdateTime", "更新时间", "")]
        public DateTime UpdateTime { get => _UpdateTime; set { if (OnPropertyChanging("UpdateTime", value)) { _UpdateTime = value; OnPropertyChanged("UpdateTime"); } } }

        private String _UpdateIP;
        /// <summary>更新地址</summary>
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
            get
            {
                switch (name)
                {
                    case "Id": return _Id;
                    case "Name": return _Name;
                    case "DisplayName": return _DisplayName;
                    case "Cron": return _Cron;
                    case "Method": return _Method;
                    case "Argument": return _Argument;
                    case "Enable": return _Enable;
                    case "LastTime": return _LastTime;
                    case "NextTime": return _NextTime;
                    case "Remark": return _Remark;
                    case "CreateUserID": return _CreateUserID;
                    case "CreateTime": return _CreateTime;
                    case "CreateIP": return _CreateIP;
                    case "UpdateUserID": return _UpdateUserID;
                    case "UpdateTime": return _UpdateTime;
                    case "UpdateIP": return _UpdateIP;
                    default: return base[name];
                }
            }
            set
            {
                switch (name)
                {
                    case "Id": _Id = value.ToInt(); break;
                    case "Name": _Name = Convert.ToString(value); break;
                    case "DisplayName": _DisplayName = Convert.ToString(value); break;
                    case "Cron": _Cron = Convert.ToString(value); break;
                    case "Method": _Method = Convert.ToString(value); break;
                    case "Argument": _Argument = Convert.ToString(value); break;
                    case "Enable": _Enable = value.ToBoolean(); break;
                    case "LastTime": _LastTime = value.ToDateTime(); break;
                    case "NextTime": _NextTime = value.ToDateTime(); break;
                    case "Remark": _Remark = Convert.ToString(value); break;
                    case "CreateUserID": _CreateUserID = value.ToInt(); break;
                    case "CreateTime": _CreateTime = value.ToDateTime(); break;
                    case "CreateIP": _CreateIP = Convert.ToString(value); break;
                    case "UpdateUserID": _UpdateUserID = value.ToInt(); break;
                    case "UpdateTime": _UpdateTime = value.ToDateTime(); break;
                    case "UpdateIP": _UpdateIP = Convert.ToString(value); break;
                    default: base[name] = value; break;
                }
            }
        }
        #endregion

        #region 字段名
        /// <summary>取得定时作业字段信息的快捷方式</summary>
        public partial class _
        {
            /// <summary>编号</summary>
            public static readonly Field Id = FindByName("Id");

            /// <summary>名称</summary>
            public static readonly Field Name = FindByName("Name");

            /// <summary>显示名</summary>
            public static readonly Field DisplayName = FindByName("DisplayName");

            /// <summary>Cron表达式。用于定时执行的Cron表达式</summary>
            public static readonly Field Cron = FindByName("Cron");

            /// <summary>命令。作业方法全名，含命名空间和类名，静态方法，包含一个String参数</summary>
            public static readonly Field Method = FindByName("Method");

            /// <summary>参数。方法参数，时间日期、网址、SQL等</summary>
            public static readonly Field Argument = FindByName("Argument");

            /// <summary>启用</summary>
            public static readonly Field Enable = FindByName("Enable");

            /// <summary>最后时间。最后一次执行作业的时间</summary>
            public static readonly Field LastTime = FindByName("LastTime");

            /// <summary>下一次时间。下一次执行作业的时间</summary>
            public static readonly Field NextTime = FindByName("NextTime");

            /// <summary>内容</summary>
            public static readonly Field Remark = FindByName("Remark");

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

            static Field FindByName(String name) => Meta.Table.FindByName(name);
        }

        /// <summary>取得定时作业字段名称的快捷方式</summary>
        public partial class __
        {
            /// <summary>编号</summary>
            public const String Id = "Id";

            /// <summary>名称</summary>
            public const String Name = "Name";

            /// <summary>显示名</summary>
            public const String DisplayName = "DisplayName";

            /// <summary>Cron表达式。用于定时执行的Cron表达式</summary>
            public const String Cron = "Cron";

            /// <summary>命令。作业方法全名，含命名空间和类名，静态方法，包含一个String参数</summary>
            public const String Method = "Method";

            /// <summary>参数。方法参数，时间日期、网址、SQL等</summary>
            public const String Argument = "Argument";

            /// <summary>启用</summary>
            public const String Enable = "Enable";

            /// <summary>最后时间。最后一次执行作业的时间</summary>
            public const String LastTime = "LastTime";

            /// <summary>下一次时间。下一次执行作业的时间</summary>
            public const String NextTime = "NextTime";

            /// <summary>内容</summary>
            public const String Remark = "Remark";

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
        }
        #endregion
    }
}