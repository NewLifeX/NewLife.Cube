namespace NewLife.Cube.Modules;

/// <summary>显式注册的模块类型。用于裁剪/AOT 或单元测试场景，常规场景由程序集自动发现</summary>
public sealed class CubeModuleType
{
    /// <summary>模块类型</summary>
    public Type Type { get; }

    /// <summary>实例化</summary>
    /// <param name="type">模块类型</param>
    public CubeModuleType(Type type) => Type = type ?? throw new ArgumentNullException(nameof(type));
}
