using Microsoft.Extensions.FileProviders;

namespace NewLife.Cube;

/// <summary>前端皮肤。皮肤包实现该接口，由魔方自动发现，支持配置切换与运行时切换</summary>
/// <remarks>
/// 皮肤包只提供前端静态资源，控制器始终由 NewLife.Cube 与业务项目提供，因此一套控制器可以对接多套皮肤。
/// 启动阶段调用 <see cref="CubeSkinService.UseCubeSkin(WebApplication, IWebHostEnvironment, String)"/> 即可启用，
/// 当前皮肤由设置项 <see cref="CubeSetting.SpaSkin"/> 决定，在管理后台修改后刷新页面即可生效，无需重启。
/// </remarks>
/// <example>
/// <code>
/// internal sealed class VueSkin : ICubeSkin
/// {
///     public String Name => "Vue";
///     public String DisplayName => "Vue3 + Element Plus";
///     public IFileProvider CreateFileProvider() => new CubeEmbeddedFileProvider(typeof(VueSkin).Assembly, "NewLife.Cube.Vue.wwwroot");
/// }
/// </code>
/// </example>
public interface ICubeSkin
{
    /// <summary>皮肤标识。如 Vue、React，配置项 SpaSkin 按该值匹配</summary>
    String Name { get; }

    /// <summary>显示名称。管理后台的皮肤下拉列表展示该名称</summary>
    String DisplayName { get; }

    /// <summary>创建静态资源文件提供者。通常返回基于程序集嵌入资源的文件提供者</summary>
    /// <returns></returns>
    IFileProvider CreateFileProvider();
}
