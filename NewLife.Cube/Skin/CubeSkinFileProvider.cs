using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace NewLife.Cube;

/// <summary>前端皮肤文件提供者。每次请求按当前皮肤解析静态资源，实现切换皮肤后无需重启</summary>
/// <remarks>
/// 包装在 <see cref="Microsoft.AspNetCore.Builder.StaticFileExtensions.UseStaticFiles(Microsoft.AspNetCore.Builder.IApplicationBuilder)"/> 的
/// <see cref="IWebHostEnvironment.WebRootFileProvider"/> 中，SPA 回退路由 <c>index.html</c> 与资源文件都会跟随当前皮肤。
/// </remarks>
public class CubeSkinFileProvider : IFileProvider
{
    private readonly CubeSkinManager _manager;

    /// <summary>实例化</summary>
    /// <param name="manager">皮肤管理器</param>
    public CubeSkinFileProvider(CubeSkinManager manager) => _manager = manager ?? throw new ArgumentNullException(nameof(manager));

    /// <summary>获取文件信息，按当前皮肤解析</summary>
    /// <param name="subpath"></param>
    /// <returns></returns>
    public IFileInfo GetFileInfo(String subpath) => GetProvider()?.GetFileInfo(subpath) ?? new NotFoundFileInfo(subpath);

    /// <summary>获取目录内容，按当前皮肤解析</summary>
    /// <param name="subpath"></param>
    /// <returns></returns>
    public IDirectoryContents GetDirectoryContents(String subpath) => GetProvider()?.GetDirectoryContents(subpath) ?? NotFoundDirectoryContents.Singleton;

    /// <summary>监视文件变化。嵌入式资源不会变化，返回空令牌</summary>
    /// <param name="filter"></param>
    /// <returns></returns>
    public IChangeToken Watch(String filter) => NullChangeToken.Singleton;

    private IFileProvider? GetProvider()
    {
        var skin = _manager.Current;
        return skin == null ? null : _manager.GetFileProvider(skin);
    }
}
