﻿using System;
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

/// <summary>附件。用于记录各系统模块使用的文件，可以是Local/NAS/OSS等</summary>
[Serializable]
[DataObject]
[Description("附件。用于记录各系统模块使用的文件，可以是Local/NAS/OSS等")]
[BindIndex("IX_Attachment_Category_Key", false, "Category,Key")]
[BindIndex("IX_Attachment_FilePath", false, "FilePath")]
[BindIndex("IX_Attachment_Extension", false, "Extension")]
[BindTable("Attachment", Description = "附件。用于记录各系统模块使用的文件，可以是Local/NAS/OSS等", ConnName = "Cube", DbType = DatabaseType.None)]
public partial class Attachment : IEntity<AttachmentModel>
{
    #region 属性
    private Int64 _Id;
    /// <summary>编号</summary>
    [DisplayName("编号")]
    [Description("编号")]
    [DataObjectField(true, false, false, 0)]
    [BindColumn("Id", "编号", "", DataScale = "time")]
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
    /// <summary>标题。业务内容作为附件标题，便于查看管理</summary>
    [DisplayName("标题")]
    [Description("标题。业务内容作为附件标题，便于查看管理")]
    [DataObjectField(false, false, true, 200)]
    [BindColumn("Title", "标题。业务内容作为附件标题，便于查看管理", "")]
    public String Title { get => _Title; set { if (OnPropertyChanging("Title", value)) { _Title = value; OnPropertyChanged("Title"); } } }

    private String _FileName;
    /// <summary>文件名。原始文件名</summary>
    [DisplayName("文件名")]
    [Description("文件名。原始文件名")]
    [DataObjectField(false, false, false, 200)]
    [BindColumn("FileName", "文件名。原始文件名", "", Master = true)]
    public String FileName { get => _FileName; set { if (OnPropertyChanging("FileName", value)) { _FileName = value; OnPropertyChanged("FileName"); } } }

    private String _Extension;
    /// <summary>扩展名</summary>
    [DisplayName("扩展名")]
    [Description("扩展名")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("Extension", "扩展名", "")]
    public String Extension { get => _Extension; set { if (OnPropertyChanging("Extension", value)) { _Extension = value; OnPropertyChanged("Extension"); } } }

    private Int64 _Size;
    /// <summary>文件大小</summary>
    [DisplayName("文件大小")]
    [Description("文件大小")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("Size", "文件大小", "", ItemType = "GMK")]
    public Int64 Size { get => _Size; set { if (OnPropertyChanging("Size", value)) { _Size = value; OnPropertyChanged("Size"); } } }

    private String _ContentType;
    /// <summary>内容类型。用于Http响应</summary>
    [DisplayName("内容类型")]
    [Description("内容类型。用于Http响应")]
    [DataObjectField(false, false, true, 200)]
    [BindColumn("ContentType", "内容类型。用于Http响应", "")]
    public String ContentType { get => _ContentType; set { if (OnPropertyChanging("ContentType", value)) { _ContentType = value; OnPropertyChanged("ContentType"); } } }

    private String _FilePath;
    /// <summary>路径。本地相对路径或OSS路径，本地相对路径加上附件目录的配置，方便整体转移附件</summary>
    [DisplayName("路径")]
    [Description("路径。本地相对路径或OSS路径，本地相对路径加上附件目录的配置，方便整体转移附件")]
    [DataObjectField(false, false, true, 200)]
    [BindColumn("FilePath", "路径。本地相对路径或OSS路径，本地相对路径加上附件目录的配置，方便整体转移附件", "")]
    public String FilePath { get => _FilePath; set { if (OnPropertyChanging("FilePath", value)) { _FilePath = value; OnPropertyChanged("FilePath"); } } }

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

    private DateTime _UploadTime;
    /// <summary>上传时间。附件上传时间，可用于构造文件存储路径</summary>
    [DisplayName("上传时间")]
    [Description("上传时间。附件上传时间，可用于构造文件存储路径")]
    [DataObjectField(false, false, true, 0)]
    [BindColumn("UploadTime", "上传时间。附件上传时间，可用于构造文件存储路径", "")]
    public DateTime UploadTime { get => _UploadTime; set { if (OnPropertyChanging("UploadTime", value)) { _UploadTime = value; OnPropertyChanged("UploadTime"); } } }

    private String _Url;
    /// <summary>网址。链接到附件所在信息页的地址</summary>
    [DisplayName("网址")]
    [Description("网址。链接到附件所在信息页的地址")]
    [DataObjectField(false, false, true, 500)]
    [BindColumn("Url", "网址。链接到附件所在信息页的地址", "")]
    public String Url { get => _Url; set { if (OnPropertyChanging("Url", value)) { _Url = value; OnPropertyChanged("Url"); } } }

    private String _Source;
    /// <summary>来源。用于远程抓取的附件来源地址，本地文件不存在时自动依次抓取</summary>
    [DisplayName("来源")]
    [Description("来源。用于远程抓取的附件来源地址，本地文件不存在时自动依次抓取")]
    [DataObjectField(false, false, true, 500)]
    [BindColumn("Source", "来源。用于远程抓取的附件来源地址，本地文件不存在时自动依次抓取", "")]
    public String Source { get => _Source; set { if (OnPropertyChanging("Source", value)) { _Source = value; OnPropertyChanged("Source"); } } }

    private Int32 _Downloads;
    /// <summary>下载次数</summary>
    [DisplayName("下载次数")]
    [Description("下载次数")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("Downloads", "下载次数", "")]
    public Int32 Downloads { get => _Downloads; set { if (OnPropertyChanging("Downloads", value)) { _Downloads = value; OnPropertyChanged("Downloads"); } } }

    private DateTime _LastDownload;
    /// <summary>最后下载。最后一次下载的时间</summary>
    [DisplayName("最后下载")]
    [Description("最后下载。最后一次下载的时间")]
    [DataObjectField(false, false, true, 0)]
    [BindColumn("LastDownload", "最后下载。最后一次下载的时间", "")]
    public DateTime LastDownload { get => _LastDownload; set { if (OnPropertyChanging("LastDownload", value)) { _LastDownload = value; OnPropertyChanged("LastDownload"); } } }

    private String _TraceId;
    /// <summary>追踪。链路追踪，用于APM性能追踪定位，还原该事件的调用链</summary>
    [Category("扩展")]
    [DisplayName("追踪")]
    [Description("追踪。链路追踪，用于APM性能追踪定位，还原该事件的调用链")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("TraceId", "追踪。链路追踪，用于APM性能追踪定位，还原该事件的调用链", "")]
    public String TraceId { get => _TraceId; set { if (OnPropertyChanging("TraceId", value)) { _TraceId = value; OnPropertyChanged("TraceId"); } } }

    private String _CreateUser;
    /// <summary>创建者</summary>
    [Category("扩展")]
    [DisplayName("创建者")]
    [Description("创建者")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("CreateUser", "创建者", "")]
    public String CreateUser { get => _CreateUser; set { if (OnPropertyChanging("CreateUser", value)) { _CreateUser = value; OnPropertyChanged("CreateUser"); } } }

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

    private String _UpdateUser;
    /// <summary>更新者</summary>
    [Category("扩展")]
    [DisplayName("更新者")]
    [Description("更新者")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("UpdateUser", "更新者", "")]
    public String UpdateUser { get => _UpdateUser; set { if (OnPropertyChanging("UpdateUser", value)) { _UpdateUser = value; OnPropertyChanged("UpdateUser"); } } }

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
    public void Copy(AttachmentModel model)
    {
        Id = model.Id;
        Category = model.Category;
        Key = model.Key;
        Title = model.Title;
        FileName = model.FileName;
        Extension = model.Extension;
        Size = model.Size;
        ContentType = model.ContentType;
        FilePath = model.FilePath;
        Hash = model.Hash;
        Enable = model.Enable;
        UploadTime = model.UploadTime;
        Url = model.Url;
        Source = model.Source;
        Downloads = model.Downloads;
        LastDownload = model.LastDownload;
        TraceId = model.TraceId;
        CreateUser = model.CreateUser;
        CreateUserID = model.CreateUserID;
        CreateIP = model.CreateIP;
        CreateTime = model.CreateTime;
        UpdateUser = model.UpdateUser;
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
            "Category" => _Category,
            "Key" => _Key,
            "Title" => _Title,
            "FileName" => _FileName,
            "Extension" => _Extension,
            "Size" => _Size,
            "ContentType" => _ContentType,
            "FilePath" => _FilePath,
            "Hash" => _Hash,
            "Enable" => _Enable,
            "UploadTime" => _UploadTime,
            "Url" => _Url,
            "Source" => _Source,
            "Downloads" => _Downloads,
            "LastDownload" => _LastDownload,
            "TraceId" => _TraceId,
            "CreateUser" => _CreateUser,
            "CreateUserID" => _CreateUserID,
            "CreateIP" => _CreateIP,
            "CreateTime" => _CreateTime,
            "UpdateUser" => _UpdateUser,
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
                case "Id": _Id = value.ToLong(); break;
                case "Category": _Category = Convert.ToString(value); break;
                case "Key": _Key = Convert.ToString(value); break;
                case "Title": _Title = Convert.ToString(value); break;
                case "FileName": _FileName = Convert.ToString(value); break;
                case "Extension": _Extension = Convert.ToString(value); break;
                case "Size": _Size = value.ToLong(); break;
                case "ContentType": _ContentType = Convert.ToString(value); break;
                case "FilePath": _FilePath = Convert.ToString(value); break;
                case "Hash": _Hash = Convert.ToString(value); break;
                case "Enable": _Enable = value.ToBoolean(); break;
                case "UploadTime": _UploadTime = value.ToDateTime(); break;
                case "Url": _Url = Convert.ToString(value); break;
                case "Source": _Source = Convert.ToString(value); break;
                case "Downloads": _Downloads = value.ToInt(); break;
                case "LastDownload": _LastDownload = value.ToDateTime(); break;
                case "TraceId": _TraceId = Convert.ToString(value); break;
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

    #region 关联映射
    #endregion

    #region 扩展查询
    #endregion

    #region 高级查询
    /// <summary>高级查询</summary>
    /// <param name="category">业务分类</param>
    /// <param name="extension">扩展名</param>
    /// <param name="filePath">路径。本地相对路径或OSS路径，本地相对路径加上附件目录的配置，方便整体转移附件</param>
    /// <param name="enable">启用</param>
    /// <param name="start">编号开始</param>
    /// <param name="end">编号结束</param>
    /// <param name="key">关键字</param>
    /// <param name="page">分页参数信息。可携带统计和数据权限扩展查询等信息</param>
    /// <returns>实体列表</returns>
    public static IList<Attachment> Search(String category, String extension, String filePath, Boolean? enable, DateTime start, DateTime end, String key, PageParameter page)
    {
        var exp = new WhereExpression();

        if (!category.IsNullOrEmpty()) exp &= _.Category == category;
        if (!extension.IsNullOrEmpty()) exp &= _.Extension == extension;
        if (!filePath.IsNullOrEmpty()) exp &= _.FilePath == filePath;
        if (enable != null) exp &= _.Enable == enable;
        exp &= _.Id.Between(start, end, Meta.Factory.Snow);
        if (!key.IsNullOrEmpty()) exp &= SearchWhereByKeys(key);

        return FindAll(exp, page);
    }
    #endregion

    #region 数据清理
    /// <summary>清理指定时间段内的数据</summary>
    /// <param name="start">开始时间。未指定时清理小于指定时间的所有数据</param>
    /// <param name="end">结束时间</param>
    /// <param name="maximumRows">最大删除行数。清理历史数据时，避免一次性删除过多导致数据库IO跟不上，0表示所有</param>
    /// <returns>清理行数</returns>
    public static Int32 DeleteWith(DateTime start, DateTime end, Int32 maximumRows = 0)
    {
        return Delete(_.Id.Between(start, end, Meta.Factory.Snow), maximumRows);
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

        /// <summary>标题。业务内容作为附件标题，便于查看管理</summary>
        public static readonly Field Title = FindByName("Title");

        /// <summary>文件名。原始文件名</summary>
        public static readonly Field FileName = FindByName("FileName");

        /// <summary>扩展名</summary>
        public static readonly Field Extension = FindByName("Extension");

        /// <summary>文件大小</summary>
        public static readonly Field Size = FindByName("Size");

        /// <summary>内容类型。用于Http响应</summary>
        public static readonly Field ContentType = FindByName("ContentType");

        /// <summary>路径。本地相对路径或OSS路径，本地相对路径加上附件目录的配置，方便整体转移附件</summary>
        public static readonly Field FilePath = FindByName("FilePath");

        /// <summary>哈希。MD5</summary>
        public static readonly Field Hash = FindByName("Hash");

        /// <summary>启用</summary>
        public static readonly Field Enable = FindByName("Enable");

        /// <summary>上传时间。附件上传时间，可用于构造文件存储路径</summary>
        public static readonly Field UploadTime = FindByName("UploadTime");

        /// <summary>网址。链接到附件所在信息页的地址</summary>
        public static readonly Field Url = FindByName("Url");

        /// <summary>来源。用于远程抓取的附件来源地址，本地文件不存在时自动依次抓取</summary>
        public static readonly Field Source = FindByName("Source");

        /// <summary>下载次数</summary>
        public static readonly Field Downloads = FindByName("Downloads");

        /// <summary>最后下载。最后一次下载的时间</summary>
        public static readonly Field LastDownload = FindByName("LastDownload");

        /// <summary>追踪。链路追踪，用于APM性能追踪定位，还原该事件的调用链</summary>
        public static readonly Field TraceId = FindByName("TraceId");

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

        /// <summary>备注</summary>
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

        /// <summary>标题。业务内容作为附件标题，便于查看管理</summary>
        public const String Title = "Title";

        /// <summary>文件名。原始文件名</summary>
        public const String FileName = "FileName";

        /// <summary>扩展名</summary>
        public const String Extension = "Extension";

        /// <summary>文件大小</summary>
        public const String Size = "Size";

        /// <summary>内容类型。用于Http响应</summary>
        public const String ContentType = "ContentType";

        /// <summary>路径。本地相对路径或OSS路径，本地相对路径加上附件目录的配置，方便整体转移附件</summary>
        public const String FilePath = "FilePath";

        /// <summary>哈希。MD5</summary>
        public const String Hash = "Hash";

        /// <summary>启用</summary>
        public const String Enable = "Enable";

        /// <summary>上传时间。附件上传时间，可用于构造文件存储路径</summary>
        public const String UploadTime = "UploadTime";

        /// <summary>网址。链接到附件所在信息页的地址</summary>
        public const String Url = "Url";

        /// <summary>来源。用于远程抓取的附件来源地址，本地文件不存在时自动依次抓取</summary>
        public const String Source = "Source";

        /// <summary>下载次数</summary>
        public const String Downloads = "Downloads";

        /// <summary>最后下载。最后一次下载的时间</summary>
        public const String LastDownload = "LastDownload";

        /// <summary>追踪。链路追踪，用于APM性能追踪定位，还原该事件的调用链</summary>
        public const String TraceId = "TraceId";

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

        /// <summary>备注</summary>
        public const String Remark = "Remark";
    }
    #endregion
}
