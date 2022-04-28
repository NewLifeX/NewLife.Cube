namespace NewLife.Cube.Modules;

/// <summary>魔方模块特性</summary>
public class ModuleAttribute : Attribute
{
    /// <summary>
    /// 名称
    /// </summary>
    public String Name { get; set; }

    /// <summary>
    /// 指定驱动名称
    /// </summary>
    /// <param name="name"></param>
    public ModuleAttribute(String name) => Name = name;
}