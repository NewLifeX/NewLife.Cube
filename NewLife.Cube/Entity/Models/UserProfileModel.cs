using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Web.Script.Serialization;
using System.Xml.Serialization;

namespace NewLife.Cube.Entity;

/// <summary>用户呈现配置。个人布局、主题与工作台默认</summary>
public partial class UserProfileModel
{
    #region 属性
    /// <summary>编号</summary>
    public Int32 Id { get; set; }

    /// <summary>用户</summary>
    public Int32 UserId { get; set; }

    /// <summary>布局。JSON：mode/siderCollapsed/siderWidth/showTabs/contentWidth</summary>
    public String LayoutJson { get; set; }

    /// <summary>主题。JSON：appearance/primaryColor/radius/density/fontScale</summary>
    public String ThemeJson { get; set; }

    /// <summary>工作台。JSON：defaultView/pageSize</summary>
    public String WorkspaceJson { get; set; }

    /// <summary>首页工作台。JSON：version+widgets</summary>
    public String HomeJson { get; set; }

    /// <summary>版本。配置契约版本</summary>
    public Int32 Version { get; set; }

    /// <summary>启用</summary>
    public Boolean Enable { get; set; }

    /// <summary>创建者</summary>
    public Int32 CreateUserId { get; set; }

    /// <summary>创建时间</summary>
    public DateTime CreateTime { get; set; }

    /// <summary>创建地址</summary>
    public String CreateIP { get; set; }

    /// <summary>更新者</summary>
    public Int32 UpdateUserId { get; set; }

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
    public void Copy(UserProfileModel model)
    {
        Id = model.Id;
        UserId = model.UserId;
        LayoutJson = model.LayoutJson;
        ThemeJson = model.ThemeJson;
        WorkspaceJson = model.WorkspaceJson;
        HomeJson = model.HomeJson;
        Version = model.Version;
        Enable = model.Enable;
        CreateUserId = model.CreateUserId;
        CreateTime = model.CreateTime;
        CreateIP = model.CreateIP;
        UpdateUserId = model.UpdateUserId;
        UpdateTime = model.UpdateTime;
        UpdateIP = model.UpdateIP;
        Remark = model.Remark;
    }
    #endregion
}
