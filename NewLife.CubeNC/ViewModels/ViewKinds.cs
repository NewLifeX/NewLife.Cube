using System.ComponentModel;

namespace NewLife.Cube.ViewModels;

/// <summary>视图类型。如List/Detail/AddForm/EditForm/Search等</summary>
public enum ViewKinds
{
    /// <summary>列表</summary>
    [Description("List")]
    List = 1,

    /// <summary>详情</summary>
    [Description("Details")]
    Detail = 2,

    /// <summary>添加表单</summary>
    [Description("Add form")]
    AddForm = 3,

    /// <summary>编辑表单</summary>
    [Description("Edit form")]
    EditForm = 4,

    /// <summary>搜索</summary>
    [Description("Search")]
    Search = 5,
}