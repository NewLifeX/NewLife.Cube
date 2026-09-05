namespace NewLife.Cube.Entity;

/// <summary>值集表格列。列表型值集的列字段定义，无真实表，数据以 JSON 存 Parameter（LovStore）</summary>
public class LovTableColumnModel
{
    /// <summary>编号</summary>
    public Int32 Id { get; set; }

    /// <summary>值集定义（按 LovCode 关联，不再落真实表）</summary>
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
}
