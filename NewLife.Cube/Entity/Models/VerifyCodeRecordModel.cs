using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Web.Script.Serialization;
using System.Xml.Serialization;

namespace NewLife.Cube.Entity;

/// <summary>验证码记录。短信/邮件等验证码生命周期与验证结果</summary>
public partial class VerifyCodeRecordModel
{
    #region 属性
    /// <summary>编号</summary>
    public Int64 Id { get; set; }

    /// <summary>租户</summary>
    public Int32 TenantId { get; set; }

    /// <summary>操作。Login/Reset/Bind等</summary>
    public String Action { get; set; }

    /// <summary>渠道。Sms/Mail等</summary>
    public String Channel { get; set; }

    /// <summary>渠道编号</summary>
    public Int32 ConfigId { get; set; }

    /// <summary>渠道配置</summary>
    public String ConfigName { get; set; }

    /// <summary>提供者。渠道实现标识</summary>
    public String Provider { get; set; }

    /// <summary>用户。已知的用户编号</summary>
    public Int32 UserId { get; set; }

    /// <summary>目标。手机号/邮箱等</summary>
    public String Target { get; set; }

    /// <summary>验证码</summary>
    public String Code { get; set; }

    /// <summary>内容。发送时的完整内容</summary>
    public String Content { get; set; }

    /// <summary>发送成功</summary>
    public Boolean Success { get; set; }

    /// <summary>发送结果。包含错误信息</summary>
    public String Result { get; set; }

    /// <summary>过期时间。验证码过期时间</summary>
    public DateTime ExpireTime { get; set; }

    /// <summary>验证次数。验证码被验证的次数，0表示未验证</summary>
    public Int32 VerifyTimes { get; set; }

    /// <summary>最后验证时间。最后一次验证的时间</summary>
    public DateTime LastVerifyTime { get; set; }

    /// <summary>最后验证结果。最后一次验证码是否正确</summary>
    public Boolean LastVerifyResult { get; set; }

    /// <summary>追踪。链路追踪</summary>
    public String TraceId { get; set; }

    /// <summary>创建地址</summary>
    public String CreateIP { get; set; }

    /// <summary>创建时间</summary>
    public DateTime CreateTime { get; set; }

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
    public void Copy(VerifyCodeRecordModel model)
    {
        Id = model.Id;
        TenantId = model.TenantId;
        Action = model.Action;
        Channel = model.Channel;
        ConfigId = model.ConfigId;
        ConfigName = model.ConfigName;
        Provider = model.Provider;
        UserId = model.UserId;
        Target = model.Target;
        Code = model.Code;
        Content = model.Content;
        Success = model.Success;
        Result = model.Result;
        ExpireTime = model.ExpireTime;
        VerifyTimes = model.VerifyTimes;
        LastVerifyTime = model.LastVerifyTime;
        LastVerifyResult = model.LastVerifyResult;
        TraceId = model.TraceId;
        CreateIP = model.CreateIP;
        CreateTime = model.CreateTime;
        UpdateTime = model.UpdateTime;
        UpdateIP = model.UpdateIP;
        Remark = model.Remark;
    }
    #endregion
}
