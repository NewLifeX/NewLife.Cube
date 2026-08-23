using System.ComponentModel;
using NewLife.Cube;
using Xunit;

namespace XUnitTest;

/// <summary>CubeSetting Mermaid 图表库地址单元测试</summary>
/// <remarks>
/// 验证 GetMermaidUrl()：
/// - 配置 ResourceUrl 时拼接自建 CDN 路径（与 echarts 同约定 /mermaid/mermaid.min.js）
/// - 留空时使用默认公共 CDN（npmmirror，国内访问稳定）
/// 前端 AI 助手据此从 CDN 懒加载 mermaid 库。
/// </remarks>
public class CubeSettingMermaidUrlTests
{
    [Fact]
    [DisplayName("GetMermaidUrl 默认返回 npmmirror 公共 CDN")]
    public void GetMermaidUrl_Default_ReturnsNpmmirror()
    {
        var set = new CubeSetting { ResourceUrl = null };

        var url = set.GetMermaidUrl();

        Assert.StartsWith("https://registry.npmmirror.com/mermaid/", url);
        Assert.EndsWith("/mermaid.min.js", url);
    }

    [Fact]
    [DisplayName("GetMermaidUrl 配置 ResourceUrl 时拼接自建 CDN 路径")]
    public void GetMermaidUrl_WithResourceUrl_JoinsCdnPath()
    {
        var set = new CubeSetting { ResourceUrl = "https://cdn.example.com/content/" };

        var url = set.GetMermaidUrl();

        Assert.Equal("https://cdn.example.com/content/mermaid/mermaid.min.js", url);
    }

    [Fact]
    [DisplayName("GetMermaidUrl 处理 ResourceUrl 无末尾斜杠")]
    public void GetMermaidUrl_WithResourceUrlNoTrailingSlash()
    {
        var set = new CubeSetting { ResourceUrl = "https://cdn.example.com/content" };

        var url = set.GetMermaidUrl();

        Assert.Equal("https://cdn.example.com/content/mermaid/mermaid.min.js", url);
    }
}
