using System.ComponentModel;

namespace NewLife.Cube.ViewModels;

/// <summary>视图类型。如List/Detail/AddForm/EditForm/Search等</summary>
public enum ViewKinds
{
    /// <summary>列表</summary>
    [Description("列表")]
    List = 1,

    /// <summary>详情</summary>
    [Description("详情")]
    Detail = 2,

    /// <summary>添加表单</summary>
    [Description("添加表单")]
    AddForm = 3,

    /// <summary>编辑表单</summary>
    [Description("编辑表单")]
    EditForm = 4,

    /// <summary>搜索</summary>
    [Description("搜索")]
    Search = 5,
}