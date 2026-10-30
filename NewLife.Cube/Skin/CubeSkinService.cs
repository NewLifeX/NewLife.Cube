using Microsoft.Extensions.FileProviders;
using Microsoft.Net.Http.Headers;
using NewLife.Log;

namespace NewLife.Cube;

/// <summary>前端皮肤服务</summary>
public static class CubeSkinService
{
    private const String SkinKey = "CubeSpaSkin";

    /// <summary>使用前端皮肤。自动发现已引用的皮肤包，按设置项 SpaSkin 决定当前皮肤，并注册 SPA 回退路由</summary>
    /// <remarks>
    /// 必须在 <c>MapControllers</c> 之后调用，保证接口路由优先匹配，SPA 回退兜底。
    /// 多个皮肤包同时引用时，只注册一次静态文件与回退路由，切换皮肤由设置项驱动，无需重启。
    /// </remarks>
    /// <param name="app"></param>
    /// <param name="env">Web宿主环境，为空时使用 <paramref name="app"/> 的环境</param>
    /// <param name="skin">强制指定皮肤标识。为空时读取设置项 SpaSkin，仍未配置则使用默认皮肤</param>
    /// <returns></returns>
    /// <example>
    /// <code>
    /// app.UseCube(builder.Environment);
    /// app.MapControllers();
    /// app.UseCubeSkin();                 // 读取设置项 SpaSkin，运行时可在管理后台切换
    /// // app.UseCubeSkin(skin: "React"); // 强制指定皮肤
    /// </code>
    /// </example>
    public static WebApplication UseCubeSkin(this WebApplication app, IWebHostEnvironment? env = null, String? skin = null)
    {
        // 多个皮肤包共存时只允许注册一次，避免重复 UseStaticFiles 与重复 SPA 回退路由（后者会造成路由歧义）
        var properties = ((IApplicationBuilder)app).Properties;
        if (properties.ContainsKey(SkinKey))
        {
            XTrace.WriteLine("Cube SPA skin already registered, ignore duplicated call");
            return app;
        }

        env ??= app.Environment;

        var manager = app.Services.GetService<CubeSkinManager>() ?? new CubeSkinManager();
        if (!skin.IsNullOrEmpty()) manager.Forced = skin;

        var current = manager.Current;
        if (current == null)
            throw new InvalidOperationException("No front-end skin package was found. Reference NewLife.Cube.Vue or NewLife.Cube.React first.");

        // 皮肤嵌入式资源优先，其次宿主 wwwroot，允许用物理文件覆盖个别资源
        var provider = new CubeSkinFileProvider(manager);
        if (env.WebRootFileProvider != null && !env.WebRootPath.IsNullOrEmpty() && Directory.Exists(env.WebRootPath))
            env.WebRootFileProvider = new CompositeFileProvider(provider, env.WebRootFileProvider);
        else
            env.WebRootFileProvider = provider;

        app.UseStaticFiles();

        // SPA 回退路由：未匹配 endpoint 的请求交给当前皮肤的 index.html，支持 History 路由
        // 入口页面不缓存，切换皮肤后刷新即可加载新皮肤
        app.MapFallbackToFile("index.html", new StaticFileOptions
        {
            OnPrepareResponse = context =>
            {
                if (context.File.Name.EqualIgnoreCase("index.html"))
                    context.Context.Response.Headers[HeaderNames.CacheControl] = "no-cache";
            }
        });

        properties[SkinKey] = current.Name;

        XTrace.WriteLine("Enabled SPA skin: {0} ({1})", current.DisplayName, current.Name);

        return app;
    }
}
