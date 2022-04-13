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
    /// <summary>附件。用于记录各系统模块使用的文件，可以是Local/NAS/OSS等</summary>
    [Serializable]
    [DataObject]
    [Description("附件。用于记录各系统模块使用的文件，可以是Local/NAS/OSS等")]
    [BindIndex("IX_Attachment_Category_Key", false, "Category,Key")]
    [BindIndex("IX_Attachment_CreateTime", false, "CreateTime")]
    [BindTable("Attachment", Description = "附件。用于记录各系统模块使用的文件，可以是Local/NAS/OSS等", ConnName = "Cube", DbType = DatabaseType.None)]
    public partial class Attachment
    {
        #region 属性
        private Int64 _Id;
        /// <summary>编号</summary>
        [DisplayName("编号")]
        [Description("编号")]
        [DataObjectField(true, false, false, 0)]
        [BindColumn("Id", "编号", "")]
        public Int64 Id { get => _Id; set { if (OnPropertyChanging("Id", value)) { _Id = value; OnPropertyChanged("Id"); } } }

        private String _Category;
        /// <summary>业务分类</summary>
        [DisplayName("业务分类")]
        [Description("业务分类")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("Category", "业务分类", "")]
        public String Category { get => _Category; set { if (OnPropertyChanging("Category", value)) { _Category = value; OnPropertyChanged("Category"); } } }

        private String _Key;
        /// <summary>业务主键</summary>
        [DisplayName("业务主键")]
        [Description("业务主键")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("Key", "业务主键", "")]
        public String Key { get => _Key; set { if (OnPropertyChanging("Key", value)) { _Key = value; OnPropertyChanged("Key"); } } }

        private String _Title;
        /// <summary>标题。业务内容作为附件标题</summary>
        [DisplayName("标题")]
        [Description("标题。业务内容作为附件标题")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("Title", "标题。业务内容作为附件标题", "")]
        public String Title { get => _Title; set { if (OnPropertyChanging("Title", value)) { _Title = value; OnPropertyChanged("Title"); } } }

        private String _FileName;
        /// <summary>文件名。原始文件名</summary>
        [DisplayName("文件名")]
        [Description("文件名。原始文件名")]
        [DataObjectField(false, false, false, 50)]
        [BindColumn("FileName", "文件名。原始文件名", "", Master = true)]
        public String FileName { get => _FileName; set { if (OnPropertyChanging("FileName", value)) { _FileName = value; OnPropertyChanged("FileName"); } } }

        private Int64 _Size;
        /// <summary>文件大小</summary>
        [DisplayName("文件大小")]
        [Description("文件大小")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("Size", "文件大小", "")]
        public Int64 Size { get => _Size; set { if (OnPropertyChanging("Size", value)) { _Size = value; OnPropertyChanged("Size"); } } }

        private String _ContentType;
        /// <summary>内容类型</summary>
        [DisplayName("内容类型")]
        [Description("内容类型")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("ContentType", "内容类型", "")]
        public String ContentType { get => _ContentType; set { if (OnPropertyChanging("ContentType", value)) { _ContentType = value; OnPropertyChanged("ContentType"); } } }

        private String _Path;
        /// <summary>路径。本地路径或OSS路径</summary>
        [DisplayName("路径")]
        [Description("路径。本地路径或OSS路径")]
        [DataObjectField(false, false, true, 200)]
        [BindColumn("Path", "路径。本地路径或OSS路径", "")]
        public String Path { get => _Path; set { if (OnPropertyChanging("Path", value)) { _Path = value; OnPropertyChanged("Path"); } } }

        private String _Hash;
        /// <summary>哈希。MD5</summary>
        [DisplayName("哈希")]
        [Description("哈希。MD5")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("Hash", "哈希。MD5", "")]
        public String Hash { get => _Hash; set { if (OnPropertyChanging("Hash", value)) { _Hash = value; OnPropertyChanged("Hash"); } } }

        private Boolean _Enable;
        /// <summary>启用</summary>
        [DisplayName("启用")]
        [Description("启用")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("Enable", "启用", "")]
        public Boolean Enable { get => _Enable; set { if (OnPropertyChanging("Enable", value)) { _Enable = value; OnPropertyChanged("Enable"); } } }

        private String _Source;
        /// <summary>来源。用于远程抓取的附件来源</summary>
        [DisplayName("来源")]
        [Description("来源。用于远程抓取的附件来源")]
        [DataObjectField(false, false, true, 500)]
        [BindColumn("Source", "来源。用于远程抓取的附件来源", "")]
        public String Source { get => _Source; set { if (OnPropertyChanging("Source", value)) { _Source = value; OnPropertyChanged("Source"); } } }

        private String _CreateUser;
        /// <summary>创建者</summary>
        [DisplayName("创建者")]
        [Description("创建者")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("CreateUser", "创建者", "")]
        public String CreateUser { get => _CreateUser; set { if (OnPropertyChanging("CreateUser", value)) { _CreateUser = value; OnPropertyChanged("CreateUser"); } } }

        private Int32 _CreateUserID;
        /// <summary>创建用户</summary>
        [DisplayName("创建用户")]
        [Description("创建用户")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("CreateUserID", "创建用户", "")]
        public Int32 CreateUserID { get => _CreateUserID; set { if (OnPropertyChanging("CreateUserID", value)) { _CreateUserID = value; OnPropertyChanged("CreateUserID"); } } }

        private String _CreateIP;
        /// <summary>创建地址</summary>
        [DisplayName("创建地址")]
        [Description("创建地址")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("CreateIP", "创建地址", "")]
        public String CreateIP { get => _CreateIP; set { if (OnPropertyChanging("CreateIP", value)) { _CreateIP = value; OnPropertyChanged("CreateIP"); } } }

        private DateTime _CreateTime;
        /// <summary>创建时间</summary>
        [DisplayName("创建时间")]
        [Description("创建时间")]
        [DataObjectField(false, false, true, 0)]
        [BindColumn("CreateTime", "创建时间", "")]
        public DateTime CreateTime { get => _CreateTime; set { if (OnPropertyChanging("CreateTime", value)) { _CreateTime = value; OnPropertyChanged("CreateTime"); } } }

        private String _UpdateUser;
        /// <summary>更新者</summary>
        [DisplayName("更新者")]
        [Description("更新者")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("UpdateUser", "更新者", "")]
        public String UpdateUser { get => _UpdateUser; set { if (OnPropertyChanging("UpdateUser", value)) { _UpdateUser = value; OnPropertyChanged("UpdateUser"); } } }

        private Int32 _UpdateUserID;
        /// <summary>更新用户</summary>
        [DisplayName("更新用户")]
        [Description("更新用户")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("UpdateUserID", "更新用户", "")]
        public Int32 UpdateUserID { get => _UpdateUserID; set { if (OnPropertyChanging("UpdateUserID", value)) { _UpdateUserID = value; OnPropertyChanged("UpdateUserID"); } } }

        private String _UpdateIP;
        /// <summary>更新地址</summary>
        [DisplayName("更新地址")]
        [Description("更新地址")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("UpdateIP", "更新地址", "")]
        public String UpdateIP { get => _UpdateIP; set { if (OnPropertyChanging("UpdateIP", value)) { _UpdateIP = value; OnPropertyChanged("UpdateIP"); } } }

        private DateTime _UpdateTime;
        /// <summary>更新时间</summary>
        [DisplayName("更新时间")]
        [Description("更新时间")]
        [DataObjectField(false, false, true, 0)]
        [BindColumn("UpdateTime", "更新时间", "")]
        public DateTime UpdateTime { get => _UpdateTime; set { if (OnPropertyChanging("UpdateTime", value)) { _UpdateTime = value; OnPropertyChanged("UpdateTime"); } } }

        private String _Remark;
        /// <summary>内容</summary>
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
                    case "Category": return _Category;
                    case "Key": return _Key;
                    case "Title": return _Title;
                    case "FileName": return _FileName;
                    case "Size": return _Size;
                    case "ContentType": return _ContentType;
                    case "Path": return _Path;
                    case "Hash": return _Hash;
                    case "Enable": return _Enable;
                    case "Source": return _Source;
                    case "CreateUser": return _CreateUser;
                    case "CreateUserID": return _CreateUserID;
                    case "CreateIP": return _CreateIP;
                    case "CreateTime": return _CreateTime;
                    case "UpdateUser": return _UpdateUser;
                    case "UpdateUserID": return _UpdateUserID;
                    case "UpdateIP": return _UpdateIP;
                    case "UpdateTime": return _UpdateTime;
                    case "Remark": return _Remark;
                    default: return base[name];
                }
            }
            set
            {
                switch (name)
                {
                    case "Id": _Id = value.ToLong(); break;
                    case "Category": _Category = Convert.ToString(value); break;
                    case "Key": _Key = Convert.ToString(value); break;
                    case "Title": _Title = Convert.ToString(value); break;
                    case "FileName": _FileName = Convert.ToString(value); break;
                    case "Size": _Size = value.ToLong(); break;
                    case "ContentType": _ContentType = Convert.ToString(value); break;
                    case "Path": _Path = Convert.ToString(value); break;
                    case "Hash": _Hash = Convert.ToString(value); break;
                    case "Enable": _Enable = value.ToBoolean(); break;
                    case "Source": _Source = Convert.ToString(value); break;
                    case "CreateUser": _CreateUser = Convert.ToString(value); break;
                    case "CreateUserID": _CreateUserID = value.ToInt(); break;
                    case "CreateIP": _CreateIP = Convert.ToString(value); break;
                    case "CreateTime": _CreateTime = value.ToDateTime(); break;
                    case "UpdateUser": _UpdateUser = Convert.ToString(value); break;
                    case "UpdateUserID": _UpdateUserID = value.ToInt(); break;
                    case "UpdateIP": _UpdateIP = Convert.ToString(value); break;
                    case "UpdateTime": _UpdateTime = value.ToDateTime(); break;
                    case "Remark": _Remark = Convert.ToString(value); break;
                    default: base[name] = value; break;
                }
            }
        }
        #endregion

        #region 字段名
        /// <summary>取得附件字段信息的快捷方式</summary>
        public partial class _
        {
            /// <summary>编号</summary>
            public static readonly Field Id = FindByName("Id");

            /// <summary>业务分类</summary>
            public static readonly Field Category = FindByName("Category");

            /// <summary>业务主键</summary>
            public static readonly Field Key = FindByName("Key");

            /// <summary>标题。业务内容作为附件标题</summary>
            public static readonly Field Title = FindByName("Title");

            /// <summary>文件名。原始文件名</summary>
            public static readonly Field FileName = FindByName("FileName");

            /// <summary>文件大小</summary>
            public static readonly Field Size = FindByName("Size");

            /// <summary>内容类型</summary>
            public static readonly Field ContentType = FindByName("ContentType");

            /// <summary>路径。本地路径或OSS路径</summary>
            public static readonly Field Path = FindByName("Path");

            /// <summary>哈希。MD5</summary>
            public static readonly Field Hash = FindByName("Hash");

            /// <summary>启用</summary>
            public static readonly Field Enable = FindByName("Enable");

            /// <summary>来源。用于远程抓取的附件来源</summary>
            public static readonly Field Source = FindByName("Source");

            /// <summary>创建者</summary>
            public static readonly Field CreateUser = FindByName("CreateUser");

            /// <summary>创建用户</summary>
            public static readonly Field CreateUserID = FindByName("CreateUserID");

            /// <summary>创建地址</summary>
            public static readonly Field CreateIP = FindByName("CreateIP");

            /// <summary>创建时间</summary>
            public static readonly Field CreateTime = FindByName("CreateTime");

            /// <summary>更新者</summary>
            public static readonly Field UpdateUser = FindByName("UpdateUser");

            /// <summary>更新用户</summary>
            public static readonly Field UpdateUserID = FindByName("UpdateUserID");

            /// <summary>更新地址</summary>
            public static readonly Field UpdateIP = FindByName("UpdateIP");

            /// <summary>更新时间</summary>
            public static readonly Field UpdateTime = FindByName("UpdateTime");

            /// <summary>内容</summary>
            public static readonly Field Remark = FindByName("Remark");

            static Field FindByName(String name) => Meta.Table.FindByName(name);
        }

        /// <summary>取得附件字段名称的快捷方式</summary>
        public partial class __
        {
            /// <summary>编号</summary>
            public const String Id = "Id";

            /// <summary>业务分类</summary>
            public const String Category = "Category";

            /// <summary>业务主键</summary>
            public const String Key = "Key";

            /// <summary>标题。业务内容作为附件标题</summary>
            public const String Title = "Title";

            /// <summary>文件名。原始文件名</summary>
            public const String FileName = "FileName";

            /// <summary>文件大小</summary>
            public const String Size = "Size";

            /// <summary>内容类型</summary>
            public const String ContentType = "ContentType";

            /// <summary>路径。本地路径或OSS路径</summary>
            public const String Path = "Path";

            /// <summary>哈希。MD5</summary>
            public const String Hash = "Hash";

            /// <summary>启用</summary>
            public const String Enable = "Enable";

            /// <summary>来源。用于远程抓取的附件来源</summary>
            public const String Source = "Source";

            /// <summary>创建者</summary>
            public const String CreateUser = "CreateUser";

            /// <summary>创建用户</summary>
            public const String CreateUserID = "CreateUserID";

            /// <summary>创建地址</summary>
            public const String CreateIP = "CreateIP";

            /// <summary>创建时间</summary>
            public const String CreateTime = "CreateTime";

            /// <summary>更新者</summary>
            public const String UpdateUser = "UpdateUser";

            /// <summary>更新用户</summary>
            public const String UpdateUserID = "UpdateUserID";

            /// <summary>更新地址</summary>
            public const String UpdateIP = "UpdateIP";

            /// <summary>更新时间</summary>
            public const String UpdateTime = "UpdateTime";

            /// <summary>内容</summary>
            public const String Remark = "Remark";
        }
        #endregion
    }
}