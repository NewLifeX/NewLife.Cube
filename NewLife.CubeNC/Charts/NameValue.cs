namespace NewLife.Cube.Charts;

/// <summary>
/// 名值对
/// </summary>
public class NameValue
{
    /// <summary>
    /// 名称
    /// </summary>
    public String Name { get; set; }

    /// <summary>
    /// 数值
    /// </summary>
    public Object Value { get; set; }

    /// <summary>
    /// 实例化
    /// </summary>
    public NameValue() { }

    /// <summary>
    /// 实例化
    /// </summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
    public NameValue(String name, Object value)
    {
        Name = name;
        Value = value;
    }
}