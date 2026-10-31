using Microsoft.Extensions.FileProviders;
using Microsoft.Net.Http.Headers;
using NewLife.Log;

namespace NewLife.Cube;

/// <summary>前端皮肤服务。UseCube 自动发现并注册已引用的皮肤包，无需用户代码额外调用</summary>
public static class CubeSkinService
{
    private const String SkinKey = "CubeSpaSkin";

    /// <summary>注册皮肤静态资源。由 UseCube 调用；项目引用了皮肤包且当前皮肤带前端产物时自动启用</summary>
    /// <remarks>
    /// 返回的管理器存入应用属性，UseVue/UseReact/UseCubeSkin 后续只调整强制皮肤，不重复注册中间件与回退路由。
    /// </remarks>
    /// <param name="app"></param>
    /// <returns></returns>
    internal static CubeSkinManager? RegisterSkins(IApplicationBuilder app)
    {
        // 只允许注册一次，避免重复 UseStaticFiles 与重复 SPA 回退路由（后者会造成路由歧义）
        if (app.Properties.TryGetValue(SkinKey, out var value)) return value as CubeSkinManager;

        var manager = app.ApplicationServices.GetService<CubeSkinManager>() ?? new CubeSkinManager();
        var current = manager.Current;
        if (current == null) return null;

        // 引用了皮肤包但未构建前端（没有 index.html）时不接管路由，避免把根路径变成 404
        var skinProvider = manager.GetFileProvider(current);
        if (skinProvider == null || !skinProvider.GetFileInfo("index.html").Exists)
        {
            XTrace.WriteLine("SPA skin [{0}] has no embedded index.html, skip auto registration", current.Name);
            return null;
        }

        app.UseStaticFiles(new StaticFileOptions { FileProvider = new CubeSkinFileProvider(manager) });

        app.Properties[SkinKey] = manager;

        XTrace.WriteLine("Enabled SPA skin: {0} ({1})", current.DisplayName, current.Name);
        if (manager.Skins.Count > 1) XTrace.WriteLine("Available SPA skins: {0}", manager.Skins.Keys.Join(", "));

        return manager;
    }

    /// <summary>注册 SPA 回退路由。未匹配的请求交给当前皮肤的 index.html，支持 History 路由</summary>
    /// <param name="endpoints"></param>
    /// <param name="manager">皮肤管理器，为空时不注册</param>
    internal static void MapFallback(IEndpointRouteBuilder endpoints, CubeSkinManager? manager)
    {
        if (manager == null) return;

        // 入口页面不缓存，切换皮肤后刷新即可加载新皮肤
        endpoints.MapFallbackToFile("index.html", new StaticFileOptions
        {
            FileProvider = new CubeSkinFileProvider(manager),
            OnPrepareResponse = context =>
            {
                if (context.File.Name.EqualIgnoreCase("index.html"))
                    context.Context.Response.Headers[HeaderNames.CacheControl] = "no-cache";
            }
        });
    }

    /// <summary>使用前端皮肤（兼容入口）。UseCube 已自动注册，通常无需调用；仅在需要强制指定皮肤时使用</summary>
    /// <remarks>
    /// 未经过 UseCube 自动注册时可独立注册静态文件与 SPA 回退，便于单元测试或特殊宿主。
    /// 已在 UseCube 中自动注册时，只应用强制皮肤，静态资源与回退路由按请求动态解析，切换无需重启。
    /// </remarks>
    /// <param name="app"></param>
    /// <param name="env">Web宿主环境，为空时使用 <paramref name="app"/> 的环境</param>
    /// <param name="skin">强制指定皮肤标识。为空时读取设置项 SpaSkin，仍未配置则使用默认皮肤</param>
    /// <returns></returns>
    /// <example>
    /// <code>
    /// // 常规用法：AddCube + UseCube 即自动启用，无需任何额外调用
    /// app.UseCube(builder.Environment);
    /// app.MapControllers();
    ///
    /// // 如需强制某套皮肤（忽略设置项）
    /// app.UseCubeSkin(skin: "React");
    /// </code>
    /// </example>
    public static WebApplication UseCubeSkin(this WebApplication app, IWebHostEnvironment? env = null, String? skin = null)
    {
        env ??= app.Environment;

        var properties = ((IApplicationBuilder)app).Properties;

        // 已由 UseCube 自动注册：只应用强制皮肤，其余按请求动态解析
        if (properties.TryGetValue(SkinKey, out var value) && value is CubeSkinManager registered)
        {
            if (!skin.IsNullOrEmpty()) registered.Forced = skin;
            return app;
        }

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

        app.MapFallbackToFile("index.html", new StaticFileOptions
        {
            OnPrepareResponse = context =>
            {
                if (context.File.Name.EqualIgnoreCase("index.html"))
                    context.Context.Response.Headers[HeaderNames.CacheControl] = "no-cache";
            }
        });

        properties[SkinKey] = manager;

        XTrace.WriteLine("Enabled SPA skin: {0} ({1})", current.DisplayName, current.Name);

        return app;
    }
}
