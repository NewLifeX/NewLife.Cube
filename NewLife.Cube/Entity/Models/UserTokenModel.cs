using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Web.Script.Serialization;
using System.Xml.Serialization;

namespace NewLife.Cube.Entity;

/// <summary>用户令牌。授权指定用户访问接口数据，支持有效期</summary>
public partial class UserTokenModel
{
    #region 属性
    /// <summary>编号</summary>
    public Int32 ID { get; set; }

    /// <summary>令牌</summary>
    public String Token { get; set; }

    /// <summary>地址。锁定该令牌只能访问该资源路径</summary>
    public String Url { get; set; }

    /// <summary>用户。本地用户</summary>
    public Int32 UserID { get; set; }

    /// <summary>过期时间</summary>
    public DateTime Expire { get; set; }

    /// <summary>启用</summary>
    public Boolean Enable { get; set; }

    /// <summary>次数。该令牌使用次数</summary>
    public Int32 Times { get; set; }

    /// <summary>首次地址</summary>
    public String FirstIP { get; set; }

    /// <summary>首次时间</summary>
    public DateTime FirstTime { get; set; }

    /// <summary>最后地址</summary>
    public String LastIP { get; set; }

    /// <summary>最后时间</summary>
    public DateTime LastTime { get; set; }

    /// <summary>创建用户</summary>
    public Int32 CreateUserID { get; set; }

    /// <summary>创建地址</summary>
    public String CreateIP { get; set; }

    /// <summary>创建时间</summary>
    public DateTime CreateTime { get; set; }

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
    public void Copy(UserTokenModel model)
    {
        ID = model.ID;
        Token = model.Token;
        Url = model.Url;
        UserID = model.UserID;
        Expire = model.Expire;
        Enable = model.Enable;
        Times = model.Times;
        FirstIP = model.FirstIP;
        FirstTime = model.FirstTime;
        LastIP = model.LastIP;
        LastTime = model.LastTime;
        CreateUserID = model.CreateUserID;
        CreateIP = model.CreateIP;
        CreateTime = model.CreateTime;
        UpdateUserID = model.UpdateUserID;
        UpdateIP = model.UpdateIP;
        UpdateTime = model.UpdateTime;
        Remark = model.Remark;
    }
    #endregion
}
