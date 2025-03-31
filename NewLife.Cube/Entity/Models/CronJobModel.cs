using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Web.Script.Serialization;
using System.Xml.Serialization;

namespace NewLife.Cube.Entity;

/// <summary>定时作业。定时执行任务</summary>
public partial class CronJobModel
{
    #region 属性
    /// <summary>编号</summary>
    public Int32 Id { get; set; }

    /// <summary>名称</summary>
    public String Name { get; set; }

    /// <summary>显示名</summary>
    public String DisplayName { get; set; }

    /// <summary>Cron表达式。用于定时执行的Cron表达式</summary>
    public String Cron { get; set; }

    /// <summary>命令。ICubeJob类名或静态方法全名(包含一个String参数)</summary>
    public String Method { get; set; }

    /// <summary>参数。方法参数，时间日期、网址、SQL等</summary>
    public String Argument { get; set; }

    /// <summary>数据。作业运行中的小量数据，可传递给下一次作业执行，例如记录数据统计的时间点</summary>
    public String Data { get; set; }

    /// <summary>启用</summary>
    public Boolean Enable { get; set; }

    /// <summary>启用日志</summary>
    public Boolean EnableLog { get; set; }

    /// <summary>最后时间。最后一次执行作业的时间</summary>
    public DateTime LastTime { get; set; }

    /// <summary>下一次时间。下一次执行作业的时间</summary>
    public DateTime NextTime { get; set; }

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
    public void Copy(CronJobModel model)
    {
        Id = model.Id;
        Name = model.Name;
        DisplayName = model.DisplayName;
        Cron = model.Cron;
        Method = model.Method;
        Argument = model.Argument;
        Data = model.Data;
        Enable = model.Enable;
        EnableLog = model.EnableLog;
        LastTime = model.LastTime;
        NextTime = model.NextTime;
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
