namespace NewLife.Cube.Modules;

/// <summary>模块注册扩展</summary>
public static class CubeModuleExtensions
{
    /// <summary>显式注册功能模块。引用程序集后默认自动发现，仅在裁剪/AOT 或单元测试需要固定清单时调用</summary>
    /// <remarks>必须在 AddCube 之前调用，否则模块的 Add 不会被装配。</remarks>
    /// <typeparam name="T">模块类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <returns></returns>
    public static IServiceCollection AddCubeModule<T>(this IServiceCollection services) where T : class, IModule
    {
        services.AddSingleton(new CubeModuleType(typeof(T)));
        return services;
    }
}
