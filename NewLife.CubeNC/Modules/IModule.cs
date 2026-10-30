namespace NewLife.Cube.Modules;

/// <summary>魔方功能模块。扩展包实现该接口，引用程序集后由魔方自动发现并装配</summary>
/// <remarks>
/// 装配分两个阶段：<see cref="Add"/> 在依赖注入注册阶段调用，<see cref="Use"/> 在请求管道构建阶段、路由注册之前调用。
/// 需要注册端点的模块额外实现 <see cref="IModuleEndpoint"/>。
/// 自动发现基于程序集扫描；裁剪/AOT 场景改用 AddCubeModule&lt;T&gt; 显式注册。
/// </remarks>
/// <example>
/// <code>
/// [Module("WeChat", DisplayName = "微信公众号", Order = 100)]
/// public class WeChatModule : IModule
/// {
///     public void Add(IServiceCollection services) => services.AddSingleton&lt;IWeChatService, WeChatService&gt;();
///     public void Use(IApplicationBuilder app, IWebHostEnvironment env) { }
/// }
/// </code>
/// </example>
public interface IModule
{
    /// <summary>添加模块。注册依赖注入服务，在 AddCube 阶段调用</summary>
    /// <param name="services">服务集合</param>
    void Add(IServiceCollection services);

    /// <summary>使用模块。配置中间件，在 UseCube 阶段、路由注册之前调用</summary>
    /// <param name="app">应用管道</param>
    /// <param name="env">Web宿主环境</param>
    void Use(IApplicationBuilder app, IWebHostEnvironment env);
}