using NewLife.AI.Services;
using NewLife.AI.Tools;
using NewLife.Cube.Modules;
using NewLife.Log;

namespace NewLife.Cube.AI;

/// <summary>AI 扩展方法，注册 AI 服务到依赖注入</summary>
public static class AIServiceExtensions
{
    /// <summary>添加魔方 AI 服务</summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddCubeAI(this IServiceCollection services)
    {
        var set = CubeSetting.Current;
        //if (!set.AISwitch)
        //{
        //    XTrace.WriteLine("AI 未启用（AISwitch=false），注册空实现");
        //    services.AddSingleton<IAIService, NoopAIService>();
        //    return services;
        //}

        XTrace.WriteLine("AI 已启用，Provider={0} Model={1}", set.AIProvider, set.AIModel);
        services.AddSingleton<IAIService, AIService>();

        // 对话会话历史：单例存储（NewLife.AI 内置 MemoryCacheSessionStore，1h 过期）。AiController 每请求用当前客户端创建 AiChatService 并复用本会话服务
        services.AddSingleton<IChatSessionStore, MemoryCacheSessionStore>();
        services.AddSingleton<ChatSessionService>();

        // 联网工具依赖的服务实现（免费，无需密钥）：AiController 注册 NetworkToolService 后经 DI 解析
        services.AddSingleton<IWebFetchService, WebFetchDirectService>();
        services.AddSingleton<ISearchService, SearchBingRssService>();
        services.AddSingleton<IWeatherService, WeatherNmcService>();
        services.AddSingleton<ITranslateService, TranslateMyMemoryService>();
        services.AddSingleton<IIpLocationService, IpLocationPconlineService>();

        return services;
    }
}

/// <summary>AI 插件，实现 IModule 接口，可通过 AppModule 数据库注册方式启用</summary>
public class AIPlugin : IModule
{
    /// <summary>添加模块，注册服务</summary>
    /// <param name="services"></param>
    public void Add(IServiceCollection services) => services.AddCubeAI();

    /// <summary>使用模块，配置中间件</summary>
    /// <param name="app"></param>
    /// <param name="env"></param>
    public void Use(IApplicationBuilder app, IWebHostEnvironment env) { }
}

///// <summary>AI 服务空实现，用于 AI 未启用时的占位</summary>
//file class NoopAIService : IAIService
//{
//    public Task<String> ChatAsync(String prompt, String data, CancellationToken cancellationToken = default)
//        => Task.FromResult("AI 未启用");
//}
