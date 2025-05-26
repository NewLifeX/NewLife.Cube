using XCode;

namespace NewLife.Cube.ViewModels;

/// <summary>实体数据列模型</summary>
/// <param name="entity"></param>
/// <param name="field"></param>
public class EntityField(IEntity entity, DataField field)
{
    /// <summary>实体</summary>
    public IEntity Entity { get; set; } = entity;

    /// <summary>数据字段</summary>
    public DataField Field { get; set; } = field;
}
