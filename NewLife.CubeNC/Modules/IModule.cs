namespace NewLife.Cube.Modules;

/// <summary>
/// 魔方模块接口。魔方将自动扫描加载实现该接口的模块，实现应用插件功能
/// </summary>
public interface IModule
{
    /// <summary>
    /// 添加模块
    /// </summary>
    /// <param name="services"></param>
    void Add(IServiceCollection services);

    /// <summary>
    /// 使用模块
    /// </summary>
    /// <param name="app"></param>
    /// <param name="env"></param>
    void Use(IApplicationBuilder app, IWebHostEnvironment env);
}