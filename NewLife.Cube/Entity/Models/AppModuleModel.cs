using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Web.Script.Serialization;
using System.Xml.Serialization;

namespace NewLife.Cube.Entity;

/// <summary>应用插件。基于魔方实现的应用功能插件</summary>
public partial class AppModuleModel
{
    #region 属性
    /// <summary>编号</summary>
    public Int32 Id { get; set; }

    /// <summary>名称</summary>
    public String Name { get; set; }

    /// <summary>显示名</summary>
    public String DisplayName { get; set; }

    /// <summary>类型。.NET/Javascript/Lua</summary>
    public String Type { get; set; }

    /// <summary>类名。完整类名</summary>
    public String ClassName { get; set; }

    /// <summary>文件。插件文件包，zip压缩</summary>
    public String FilePath { get; set; }

    /// <summary>启用</summary>
    public Boolean Enable { get; set; }

    /// <summary>描述</summary>
    public String Remark { get; set; }
    #endregion

    #region 拷贝
    /// <summary>拷贝模型对象</summary>
    /// <param name="model">模型</param>
    public void Copy(AppModuleModel model)
    {
        Id = model.Id;
        Name = model.Name;
        DisplayName = model.DisplayName;
        Type = model.Type;
        ClassName = model.ClassName;
        FilePath = model.FilePath;
        Enable = model.Enable;
        Remark = model.Remark;
    }
    #endregion
}
