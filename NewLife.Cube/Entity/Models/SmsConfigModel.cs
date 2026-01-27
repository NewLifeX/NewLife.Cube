using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Web.Script.Serialization;
using System.Xml.Serialization;

namespace NewLife.Cube.Entity;

/// <summary>短信配置。短信渠道配置，支持多租户多渠道</summary>
public partial class SmsConfigModel
{
    #region 属性
    /// <summary>编号</summary>
    public Int32 Id { get; set; }

    /// <summary>租户</summary>
    public Int32 TenantId { get; set; }

    /// <summary>提供者。对应具体实现类标识，用于区分同类型多渠道</summary>
    public String Provider { get; set; }

    /// <summary>名称。渠道名称，如Aliyun/Tencent</summary>
    public String Name { get; set; }

    /// <summary>显示名</summary>
    public String DisplayName { get; set; }

    /// <summary>服务端点。短信服务地址，如dypnsapi.aliyuncs.com</summary>
    public String Server { get; set; }

    /// <summary>应用标识。AccessKeyId</summary>
    public String AppKey { get; set; }

    /// <summary>应用密钥。AccessKeySecret</summary>
    public String AppSecret { get; set; }

    /// <summary>签名。短信签名名称</summary>
    public String SignName { get; set; }

    /// <summary>方案名称。短信方案名称，可选</summary>
    public String SchemaName { get; set; }

    /// <summary>验证码长度。默认4位</summary>
    public Int32 CodeLength { get; set; }

    /// <summary>有效期。验证码有效期，单位秒，默认300秒(5分钟)</summary>
    public Int32 Expire { get; set; }

    /// <summary>启用</summary>
    public Boolean Enable { get; set; }

    /// <summary>启用登录。用于登录/注册场景</summary>
    public Boolean EnableLogin { get; set; }

    /// <summary>启用重置。用于忘记密码场景</summary>
    public Boolean EnableReset { get; set; }

    /// <summary>启用绑定。用于绑定手机场景</summary>
    public Boolean EnableBind { get; set; }

    /// <summary>启用通知。用于普通通知场景</summary>
    public Boolean EnableNotify { get; set; }

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
    public void Copy(SmsConfigModel model)
    {
        Id = model.Id;
        TenantId = model.TenantId;
        Provider = model.Provider;
        Name = model.Name;
        DisplayName = model.DisplayName;
        Server = model.Server;
        AppKey = model.AppKey;
        AppSecret = model.AppSecret;
        SignName = model.SignName;
        SchemaName = model.SchemaName;
        CodeLength = model.CodeLength;
        Expire = model.Expire;
        Enable = model.Enable;
        EnableLogin = model.EnableLogin;
        EnableReset = model.EnableReset;
        EnableBind = model.EnableBind;
        EnableNotify = model.EnableNotify;
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
