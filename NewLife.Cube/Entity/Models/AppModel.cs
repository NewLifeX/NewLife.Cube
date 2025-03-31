using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Web.Script.Serialization;
using System.Xml.Serialization;

namespace NewLife.Cube.Entity;

/// <summary>应用系统。用于OAuthServer的子系统</summary>
public partial class AppModel
{
    #region 属性
    /// <summary>编号</summary>
    public Int32 Id { get; set; }

    /// <summary>名称。AppID</summary>
    public String Name { get; set; }

    /// <summary>显示名</summary>
    public String DisplayName { get; set; }

    /// <summary>密钥。AppSecret</summary>
    public String Secret { get; set; }

    /// <summary>类别</summary>
    public String Category { get; set; }

    /// <summary>启用</summary>
    public Boolean Enable { get; set; }

    /// <summary>首页</summary>
    public String HomePage { get; set; }

    /// <summary>图标。附件路径</summary>
    public String Logo { get; set; }

    /// <summary>IP白名单。符合条件的来源IP才允许访问，支持*通配符，多个逗号隔开</summary>
    public String White { get; set; }

    /// <summary>IP黑名单。符合条件的来源IP禁止访问，支持*通配符，多个逗号隔开</summary>
    public String Black { get; set; }

    /// <summary>有效期。访问令牌AccessToken的有效期，单位秒，默认使用全局设置</summary>
    public Int32 TokenExpire { get; set; }

    /// <summary>回调地址。用于限制回调地址安全性，多个地址逗号隔开</summary>
    public String Urls { get; set; }

    /// <summary>授权角色。只允许这些角色登录该系统，多个角色逗号隔开，未填写时表示不限制</summary>
    public String RoleIds { get; set; }

    /// <summary>能力集合。逗号分隔，password，client_credentials</summary>
    public String Scopes { get; set; }

    /// <summary>三方OAuth。本系统作为OAuthServer时，该应用前来验证时可用的第三方OAuth提供商，多个逗号隔开</summary>
    public String OAuths { get; set; }

    /// <summary>过期时间。空表示永不过期</summary>
    public DateTime Expired { get; set; }

    /// <summary>次数</summary>
    public Int32 Auths { get; set; }

    /// <summary>最后请求</summary>
    public DateTime LastAuth { get; set; }

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
    public void Copy(AppModel model)
    {
        Id = model.Id;
        Name = model.Name;
        DisplayName = model.DisplayName;
        Secret = model.Secret;
        Category = model.Category;
        Enable = model.Enable;
        HomePage = model.HomePage;
        Logo = model.Logo;
        White = model.White;
        Black = model.Black;
        TokenExpire = model.TokenExpire;
        Urls = model.Urls;
        RoleIds = model.RoleIds;
        Scopes = model.Scopes;
        OAuths = model.OAuths;
        Expired = model.Expired;
        Auths = model.Auths;
        LastAuth = model.LastAuth;
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
