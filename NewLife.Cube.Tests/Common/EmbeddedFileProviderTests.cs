using System;
using System.Linq;
using Microsoft.Extensions.FileProviders;
using NewLife.Cube.Extensions;
using Xunit;

namespace NewLife.Cube.Tests.Common;

/// <summary>
/// SPA-2 嵌入式静态文件服务测试。覆盖嵌入 wwwroot 资源的读取、缺失文件、目录枚举与非法路径处理。
/// </summary>
public class EmbeddedFileProviderTests
{
    private const String BaseNamespace = "NewLife.Cube.wwwroot";

    private static readonly System.Reflection.Assembly _assembly = typeof(CubeEmbeddedFileProvider).Assembly;

    [Fact(DisplayName = "嵌入文件_可读取已打包的静态资源")]
    public void GetFileInfo_ExistingResource()
    {
        var provider = new CubeEmbeddedFileProvider(_assembly, BaseNamespace);

        // 根目录 favicon 与多级目录图片都应能读到
        var icon = provider.GetFileInfo("/favicon.ico");
        Assert.True(icon.Exists, "应能读到嵌入资源 favicon.ico");
        Assert.True(icon.Length > 0);
        Assert.Equal("favicon.ico", icon.Name);
        Assert.True(icon.LastModified.Year >= 2000);

        var logo = provider.GetFileInfo("/Content/images/logo/Github.png");
        Assert.True(logo.Exists, "应能读到嵌入资源 Content/images/logo/Github.png");
        Assert.True(logo.Length > 0);
        Assert.Equal("Github.png", logo.Name);
    }

    [Fact(DisplayName = "嵌入文件_缺失的资源返回不存在")]
    public void GetFileInfo_Missing_NotFound()
    {
        var provider = new CubeEmbeddedFileProvider(_assembly, BaseNamespace);

        Assert.False(provider.GetFileInfo("/not-exists/file.js").Exists);
        Assert.False(provider.GetFileInfo("not-exists/file.js").Exists);
        Assert.False(provider.GetFileInfo("").Exists);
        Assert.False(provider.GetFileInfo(null).Exists);
    }

    [Fact(DisplayName = "嵌入文件_目录枚举返回全部打包资源")]
    public void GetDirectoryContents_Root_ListsResources()
    {
        var provider = new CubeEmbeddedFileProvider(_assembly, BaseNamespace);

        var contents = provider.GetDirectoryContents("/");

        Assert.True(contents.Exists);
        Assert.NotEmpty(contents);
        Assert.All(contents, e => Assert.True(e.Exists));
    }

    [Fact(DisplayName = "嵌入文件_子目录枚举不支持")]
    public void GetDirectoryContents_SubPath_NotFound()
    {
        var provider = new CubeEmbeddedFileProvider(_assembly, BaseNamespace);

        Assert.False(provider.GetDirectoryContents("/js").Exists);
        Assert.False(provider.GetDirectoryContents(null).Exists);
    }

    [Fact(DisplayName = "嵌入文件_监视令牌始终可返回")]
    public void Watch_ReturnsToken()
    {
        var provider = new CubeEmbeddedFileProvider(_assembly, BaseNamespace);

        Assert.NotNull(provider.Watch("**/*"));
    }
}


