namespace NewLife.Cube.Entity;

/// <summary>值集定义模型（手工/名值定义）。无真实表，数据以 JSON 存 Parameter（LovStore，Category=Lov.Def，Name=LovCode）</summary>
/// <remarks>
/// 本模型仅承载运行时手工创建的值集定义；代码声明的枚举与 [LovList] 列表型值集由
/// <see cref="NewLife.Cube.Services.LovRegistry"/> 反射提供，不落库也不产生本模型实例。
/// </remarks>
public class LovDefModel
{
    /// <summary>值集编码。带前缀的完全限定编码，如 Enum.Manual.Status / List.Manual.Role</summary>
    public String LovCode { get; set; } = "";

    /// <summary>显示名称</summary>
    public String Name { get; set; } = "";

    /// <summary>值集类型。ENUM=名值（值/标签对）定义/LIST=列表型（外部数据源），须与LovCode前缀一致</summary>
    public String Type { get; set; } = "ENUM";

    /// <summary>值字段。列表型的值字段名</summary>
    public String ValueField { get; set; } = "id";

    /// <summary>标签字段。列表型的标签字段名</summary>
    public String LabelField { get; set; } = "name";

    /// <summary>来源。手工定义恒为 MANUAL</summary>
    public String Source { get; set; } = "MANUAL";

    /// <summary>启用</summary>
    public Boolean Enabled { get; set; } = true;

    /// <summary>备注</summary>
    public String? Remark { get; set; }

    /// <summary>创建者（来自 Parameter 审计，展示用，不参与存储语义）</summary>
    public String? CreateUser { get; set; }

    /// <summary>创建时间（来自 Parameter 审计，展示用）</summary>
    public DateTime CreateTime { get; set; }

    /// <summary>更新者（来自 Parameter 审计，展示用）</summary>
    public String? UpdateUser { get; set; }

    /// <summary>更新时间（来自 Parameter 审计，展示用）</summary>
    public DateTime UpdateTime { get; set; }
}
