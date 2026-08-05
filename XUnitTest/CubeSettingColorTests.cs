using System.ComponentModel;
using NewLife.Cube;
using Xunit;

namespace XUnitTest;

/// <summary>CubeSetting AI 配色配置单元测试</summary>
/// <remarks>验证 AI 助手主题色方案、主色/辅色的默认值与可修改性，前端渲染依赖这些默认值</remarks>
public class CubeSettingColorTests
{
    [Fact]
    [DisplayName("AI 配色字段默认值")]
    public void AiColorDefaults_AreCorrect()
    {
        var set = new CubeSetting();

        Assert.Equal("新生命绿", set.AIColorScheme);
        Assert.Equal("#2ecc71", set.AIPrimaryColor);
        Assert.Equal("#1e8e3e", set.AISecondaryColor);
    }

    [Fact]
    [DisplayName("AI 配色字段可修改")]
    public void AiColor_CanBeModified()
    {
        var set = new CubeSetting
        {
            AIColorScheme = "翠绿",
            AIPrimaryColor = "#10b981",
            AISecondaryColor = "#059669",
        };

        Assert.Equal("翠绿", set.AIColorScheme);
        Assert.Equal("#10b981", set.AIPrimaryColor);
        Assert.Equal("#059669", set.AISecondaryColor);
    }
}
