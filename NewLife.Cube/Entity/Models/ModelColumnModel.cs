using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Web.Script.Serialization;
using System.Xml.Serialization;

namespace NewLife.Cube.Entity;

/// <summary>模型列。实体表的数据列</summary>
public partial class ModelColumnModel
{
    #region 属性
    /// <summary>编号</summary>
    public Int32 Id { get; set; }

    /// <summary>模型表</summary>
    public Int32 TableId { get; set; }

    /// <summary>名称</summary>
    public String Name { get; set; }

    /// <summary>显示名</summary>
    public String DisplayName { get; set; }

    /// <summary>启用</summary>
    public Boolean Enable { get; set; }

    /// <summary>数据类型</summary>
    public String DataType { get; set; }

    /// <summary>元素类型。image,file,html,singleSelect,multipleSelect</summary>
    public String ItemType { get; set; }

    /// <summary>主键</summary>
    public Boolean PrimaryKey { get; set; }

    /// <summary>主字段。主字段作为业务主要字段，代表当前数据行意义</summary>
    public Boolean Master { get; set; }

    /// <summary>长度</summary>
    public Int32 Length { get; set; }

    /// <summary>允许空</summary>
    public Boolean Nullable { get; set; }

    /// <summary>数据字段</summary>
    public Boolean IsDataObjectField { get; set; }

    /// <summary>说明</summary>
    public String Description { get; set; }

    /// <summary>列表页显示</summary>
    public Boolean ShowInList { get; set; }

    /// <summary>添加表单页显示</summary>
    public Boolean ShowInAddForm { get; set; }

    /// <summary>编辑表单页显示</summary>
    public Boolean ShowInEditForm { get; set; }

    /// <summary>详情表单页显示</summary>
    public Boolean ShowInDetailForm { get; set; }

    /// <summary>搜索显示</summary>
    public Boolean ShowInSearch { get; set; }

    /// <summary>排序</summary>
    public Int32 Sort { get; set; }

    /// <summary>宽度</summary>
    public String Width { get; set; }

    /// <summary>单元格文字</summary>
    public String CellText { get; set; }

    /// <summary>单元格标题。数据单元格上的提示文字</summary>
    public String CellTitle { get; set; }

    /// <summary>单元格链接。数据单元格的链接</summary>
    public String CellUrl { get; set; }

    /// <summary>头部文字</summary>
    public String HeaderText { get; set; }

    /// <summary>头部标题。数据移上去后显示的文字</summary>
    public String HeaderTitle { get; set; }

    /// <summary>头部链接。一般是排序</summary>
    public String HeaderUrl { get; set; }

    /// <summary>数据动作。设为action时走ajax请求</summary>
    public String DataAction { get; set; }

    /// <summary>多选数据源</summary>
    public String DataSource { get; set; }

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
    #endregion

    #region 拷贝
    /// <summary>拷贝模型对象</summary>
    /// <param name="model">模型</param>
    public void Copy(ModelColumnModel model)
    {
        Id = model.Id;
        TableId = model.TableId;
        Name = model.Name;
        DisplayName = model.DisplayName;
        Enable = model.Enable;
        DataType = model.DataType;
        ItemType = model.ItemType;
        PrimaryKey = model.PrimaryKey;
        Master = model.Master;
        Length = model.Length;
        Nullable = model.Nullable;
        IsDataObjectField = model.IsDataObjectField;
        Description = model.Description;
        ShowInList = model.ShowInList;
        ShowInAddForm = model.ShowInAddForm;
        ShowInEditForm = model.ShowInEditForm;
        ShowInDetailForm = model.ShowInDetailForm;
        ShowInSearch = model.ShowInSearch;
        Sort = model.Sort;
        Width = model.Width;
        CellText = model.CellText;
        CellTitle = model.CellTitle;
        CellUrl = model.CellUrl;
        HeaderText = model.HeaderText;
        HeaderTitle = model.HeaderTitle;
        HeaderUrl = model.HeaderUrl;
        DataAction = model.DataAction;
        DataSource = model.DataSource;
        CreateUserId = model.CreateUserId;
        CreateTime = model.CreateTime;
        CreateIP = model.CreateIP;
        UpdateUserId = model.UpdateUserId;
        UpdateTime = model.UpdateTime;
        UpdateIP = model.UpdateIP;
    }
    #endregion
}
