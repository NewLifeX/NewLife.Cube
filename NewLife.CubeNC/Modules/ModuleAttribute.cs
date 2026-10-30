namespace NewLife.Cube.Modules;

/// <summary>魔方扩展特性。声明模块或适配器的名称、显示名与装配顺序</summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class ModuleAttribute : Attribute
{
    /// <summary>名称。缺省取类型名去掉 Module/Service/Adapter 后缀</summary>
    public String Name { get; set; }

    /// <summary>显示名称。用于日志与诊断</summary>
    public String DisplayName { get; set; }

    /// <summary>装配顺序。升序，缺省 0</summary>
    public Int32 Order { get; set; }

    /// <summary>指定扩展名称</summary>
    /// <param name="name">名称</param>
    public ModuleAttribute(String name) => Name = name;
}