using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Web.Script.Serialization;
using System.Xml.Serialization;

namespace NewLife.Cube.Entity;

/// <summary>值集表格列。列表型值集的列字段定义</summary>
public partial class LovTableColumnModel
{
    #region 属性
    /// <summary>编号</summary>
    public Int32 Id { get; set; }

    /// <summary>值集定义。关联LovDefinition</summary>
    public Int32 LovDefId { get; set; }

    /// <summary>字段名。数据字段名</summary>
    public String Field { get; set; }

    /// <summary>显示标题</summary>
    public String Title { get; set; }

    /// <summary>列宽</summary>
    public Int32 Width { get; set; }

    /// <summary>对齐方式。left/center/right</summary>
    public String Align { get; set; }

    /// <summary>是否可排序</summary>
    public Boolean Sortable { get; set; }

    /// <summary>关联值集。该列原始值需翻译为此值集的显示文本</summary>
    public String RefLovCode { get; set; }

    /// <summary>格式化类型。与RefLovCode互斥，如 date/amount</summary>
    public String FormatType { get; set; }

    /// <summary>排序</summary>
    public Int32 Sort { get; set; }

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
    public void Copy(LovTableColumnModel model)
    {
        Id = model.Id;
        LovDefId = model.LovDefId;
        Field = model.Field;
        Title = model.Title;
        Width = model.Width;
        Align = model.Align;
        Sortable = model.Sortable;
        RefLovCode = model.RefLovCode;
        FormatType = model.FormatType;
        Sort = model.Sort;
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
