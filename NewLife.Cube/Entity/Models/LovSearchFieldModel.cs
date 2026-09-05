namespace NewLife.Cube.Entity;

/// <summary>值集搜索字段。列表型值集的搜索条件字段定义，无真实表，数据以 JSON 存 Parameter（LovStore）</summary>
public class LovSearchFieldModel
{
    /// <summary>编号</summary>
    public Int32 Id { get; set; }

    /// <summary>值集定义（按 LovCode 关联，不再落真实表）</summary>
    public Int32 LovDefId { get; set; }

    /// <summary>字段名。搜索参数字段名</summary>
    public String Field { get; set; }

    /// <summary>显示标题</summary>
    public String Title { get; set; }

    /// <summary>控件类型。input/select/lov/datepicker等</summary>
    public String ComponentType { get; set; }

    /// <summary>传参方式。BODY=请求体/QUERY=查询参数</summary>
    public String ParamType { get; set; }

    /// <summary>是否必填</summary>
    public Boolean Required { get; set; }

    /// <summary>默认值</summary>
    public String DefaultValue { get; set; }

    /// <summary>排序</summary>
    public Int32 Sort { get; set; }

    /// <summary>关联值集。该搜索字段渲染为此值集的选择控件</summary>
    public String RefLovCode { get; set; }

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
}
