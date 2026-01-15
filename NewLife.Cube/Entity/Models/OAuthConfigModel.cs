using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Web.Script.Serialization;
using System.Xml.Serialization;

namespace NewLife.Cube.Entity;

/// <summary>OAuth配置。需要连接的OAuth认证方</summary>
public partial class OAuthConfigModel
{
    #region 属性
    /// <summary>编号</summary>
    public Int32 ID { get; set; }

    /// <summary>租户</summary>
    public Int32 TenantId { get; set; }

    /// <summary>名称。提供者名称</summary>
    public String Name { get; set; }

    /// <summary>昵称</summary>
    public String NickName { get; set; }

    /// <summary>图标</summary>
    public String Logo { get; set; }

    /// <summary>应用标识</summary>
    public String AppId { get; set; }

    /// <summary>应用密钥</summary>
    public String Secret { get; set; }

    /// <summary>服务地址</summary>
    public String Server { get; set; }

    /// <summary>令牌服务地址。可以不同于验证地址的内网直达地址</summary>
    public String AccessServer { get; set; }

    /// <summary>授权类型</summary>
    public GrantTypes GrantType { get; set; }

    /// <summary>授权范围</summary>
    public String Scope { get; set; }

    /// <summary>验证地址。跳转SSO的验证地址</summary>
    public String AuthUrl { get; set; }

    /// <summary>令牌地址。根据code换取令牌的地址</summary>
    public String AccessUrl { get; set; }

    /// <summary>用户地址。根据令牌获取用户信息的地址</summary>
    public String UserUrl { get; set; }

    /// <summary>应用地址。域名和端口，应用系统经过反向代理重定向时指定外部地址</summary>
    public String AppUrl { get; set; }

    /// <summary>启用</summary>
    public Boolean Enable { get; set; }

    /// <summary>调试。设置处于调试状态，输出详细日志</summary>
    public Boolean Debug { get; set; }

    /// <summary>可见。是否在登录页面可见，不可见的提供者只能使用应用内自动登录，例如微信公众号</summary>
    public Boolean Visible { get; set; }

    /// <summary>自动注册。SSO登录后，如果本地没有匹配用户，自动注册新用户，否则跳到登录页，在登录后绑定</summary>
    public Boolean AutoRegister { get; set; }

    /// <summary>自动角色。该渠道登录的用户，将会自动得到指定角色名，多个角色逗号隔开</summary>
    public String AutoRole { get; set; }

    /// <summary>排序。较大者在前面</summary>
    public Int32 Sort { get; set; }

    /// <summary>安全密钥。公钥，用于RSA加密用户密码，在通信链路上保护用户密码安全，密钥前面可以增加keyName，形成keyName$keyValue，用于向服务端指示所使用的密钥标识，方便未来更换密钥。</summary>
    public String SecurityKey { get; set; }

    /// <summary>字段映射。SSO用户字段如何映射到OAuthClient内部属性</summary>
    public String FieldMap { get; set; }

    /// <summary>抓取头像。是否抓取头像并保存到本地</summary>
    public Boolean FetchAvatar { get; set; }

    /// <summary>删除。是否已删除，可恢复</summary>
    public Boolean IsDeleted { get; set; }

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

    /// <summary>内容</summary>
    public String Remark { get; set; }
    #endregion

    #region 拷贝
    /// <summary>拷贝模型对象</summary>
    /// <param name="model">模型</param>
    public void Copy(OAuthConfigModel model)
    {
        ID = model.ID;
        TenantId = model.TenantId;
        Name = model.Name;
        NickName = model.NickName;
        Logo = model.Logo;
        AppId = model.AppId;
        Secret = model.Secret;
        Server = model.Server;
        AccessServer = model.AccessServer;
        GrantType = model.GrantType;
        Scope = model.Scope;
        AuthUrl = model.AuthUrl;
        AccessUrl = model.AccessUrl;
        UserUrl = model.UserUrl;
        AppUrl = model.AppUrl;
        Enable = model.Enable;
        Debug = model.Debug;
        Visible = model.Visible;
        AutoRegister = model.AutoRegister;
        AutoRole = model.AutoRole;
        Sort = model.Sort;
        SecurityKey = model.SecurityKey;
        FieldMap = model.FieldMap;
        FetchAvatar = model.FetchAvatar;
        IsDeleted = model.IsDeleted;
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
