using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Web.Script.Serialization;
using System.Xml.Serialization;

namespace NewLife.Cube.Entity;

/// <summary>附件。用于记录各系统模块使用的文件，可以是Local/NAS/OSS等</summary>
public partial class AttachmentModel
{
    #region 属性
    /// <summary>编号</summary>
    public Int64 Id { get; set; }

    /// <summary>业务分类</summary>
    public String Category { get; set; }

    /// <summary>业务主键</summary>
    public String Key { get; set; }

    /// <summary>标题。业务内容作为附件标题，便于查看管理</summary>
    public String Title { get; set; }

    /// <summary>文件名。原始文件名</summary>
    public String FileName { get; set; }

    /// <summary>扩展名</summary>
    public String Extension { get; set; }

    /// <summary>文件大小</summary>
    public Int64 Size { get; set; }

    /// <summary>内容类型。用于Http响应</summary>
    public String ContentType { get; set; }

    /// <summary>路径。本地相对路径或OSS路径，本地相对路径加上附件目录的配置，方便整体转移附件</summary>
    public String FilePath { get; set; }

    /// <summary>哈希。MD5</summary>
    public String Hash { get; set; }

    /// <summary>启用</summary>
    public Boolean Enable { get; set; }

    /// <summary>上传时间。附件上传时间，可用于构造文件存储路径</summary>
    public DateTime UploadTime { get; set; }

    /// <summary>网址。链接到附件所在信息页的地址</summary>
    public String Url { get; set; }

    /// <summary>来源。用于远程抓取的附件来源地址，本地文件不存在时自动依次抓取</summary>
    public String Source { get; set; }

    /// <summary>追踪。链路追踪，用于APM性能追踪定位，还原该事件的调用链</summary>
    public String TraceId { get; set; }

    /// <summary>创建者</summary>
    public String CreateUser { get; set; }

    /// <summary>创建用户</summary>
    public Int32 CreateUserID { get; set; }

    /// <summary>创建地址</summary>
    public String CreateIP { get; set; }

    /// <summary>创建时间</summary>
    public DateTime CreateTime { get; set; }

    /// <summary>更新者</summary>
    public String UpdateUser { get; set; }

    /// <summary>更新用户</summary>
    public Int32 UpdateUserID { get; set; }

    /// <summary>更新地址</summary>
    public String UpdateIP { get; set; }

    /// <summary>更新时间</summary>
    public DateTime UpdateTime { get; set; }

    /// <summary>备注</summary>
    public String Remark { get; set; }
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
}
