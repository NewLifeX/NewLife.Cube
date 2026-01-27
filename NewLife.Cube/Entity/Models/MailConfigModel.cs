using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Web.Script.Serialization;
using System.Xml.Serialization;

namespace NewLife.Cube.Entity;

/// <summary>邮件配置。邮件渠道配置，支持多租户多渠道</summary>
public partial class MailConfigModel
{
    #region 属性
    /// <summary>编号</summary>
    public Int32 Id { get; set; }

    /// <summary>租户</summary>
    public Int32 TenantId { get; set; }

    /// <summary>提供者。对应具体实现类标识，用于区分同类型多渠道</summary>
    public String Provider { get; set; }

    /// <summary>名称。渠道名称</summary>
    public String Name { get; set; }

    /// <summary>显示名</summary>
    public String DisplayName { get; set; }

    /// <summary>服务器。SMTP服务器地址</summary>
    public String Server { get; set; }

    /// <summary>端口。SMTP端口，默认25，SSL465/587</summary>
    public Int32 Port { get; set; }

    /// <summary>启用SSL。是否使用SSL加密连接</summary>
    public Boolean EnableSsl { get; set; }

    /// <summary>用户名。SMTP登录账号</summary>
    public String UserName { get; set; }

    /// <summary>密码。SMTP登录密码或授权码</summary>
    public String Password { get; set; }

    /// <summary>发件人邮箱。发送邮件时的发件人地址</summary>
    public String FromMail { get; set; }

    /// <summary>发件人名称。发送邮件时显示的发件人名称</summary>
    public String FromName { get; set; }

    /// <summary>验证码长度。默认6位</summary>
    public Int32 CodeLength { get; set; }

    /// <summary>有效期。验证码有效期，单位秒，默认600秒(10分钟)</summary>
    public Int32 Expire { get; set; }

    /// <summary>启用</summary>
    public Boolean Enable { get; set; }

    /// <summary>优先级。较大优先</summary>
    public Int32 Priority { get; set; }

    /// <summary>创建者</summary>
    public Int32 CreateUserID { get; set; }

    /// <summary>创建时间</summary>
    public DateTime CreateTime { get; set; }

    /// <summary>创建地址</summary>
    public String CreateIP { get; set; }

    /// <summary>更新者</summary>
    public Int32 UpdateUserID { get; set; }

    /// <summary>更新时间</summary>
    public DateTime UpdateTime { get; set; }

    /// <summary>更新地址</summary>
    public String UpdateIP { get; set; }

    /// <summary>备注</summary>
    public String Remark { get; set; }
    #endregion

    #region 拷贝
    /// <summary>拷贝模型对象</summary>
    /// <param name="model">模型</param>
    public void Copy(MailConfigModel model)
    {
        Id = model.Id;
        TenantId = model.TenantId;
        Provider = model.Provider;
        Name = model.Name;
        DisplayName = model.DisplayName;
        Server = model.Server;
        Port = model.Port;
        EnableSsl = model.EnableSsl;
        UserName = model.UserName;
        Password = model.Password;
        FromMail = model.FromMail;
        FromName = model.FromName;
        CodeLength = model.CodeLength;
        Expire = model.Expire;
        Enable = model.Enable;
        Priority = model.Priority;
        CreateUserID = model.CreateUserID;
        CreateTime = model.CreateTime;
        CreateIP = model.CreateIP;
        UpdateUserID = model.UpdateUserID;
        UpdateTime = model.UpdateTime;
        UpdateIP = model.UpdateIP;
        Remark = model.Remark;
    }
    #endregion
}
