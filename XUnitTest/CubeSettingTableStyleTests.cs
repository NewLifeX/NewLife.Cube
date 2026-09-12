using System.ComponentModel;
using NewLife.Cube;
using Xunit;

namespace XUnitTest;

/// <summary>CubeSetting 表格分隔样式与行高密度配置单元测试</summary>
/// <remarks>
/// 验证 TableStyle / TableDensity 的默认值与可修改性。
/// ACE 皮肤经 _Layout 将两值输出为 body class（cube-table-xxx / cube-density-xxx），
/// ace-ui.css 按 class 切换三档分隔样式与两档行高，前端渲染依赖这些默认值。
/// </remarks>
public class CubeSettingTableStyleTests
{
    [Fact]
    [DisplayName("表格样式字段默认值")]
    public void TableStyleDefaults_AreCorrect()
    {
        var set = new CubeSetting();

        // 默认标准档：清晰行线解决"分界线过淡"；默认紧凑密度保持既有行高
        Assert.Equal("Standard", set.TableStyle);
        Assert.Equal("Compact", set.TableDensity);
    }

    [Fact]
    [DisplayName("表格样式字段可修改")]
    public void TableStyle_CanBeModified()
    {
        var set = new CubeSetting
        {
            TableStyle = "Grid",
            TableDensity = "Normal",
        };

        Assert.Equal("Grid", set.TableStyle);
        Assert.Equal("Normal", set.TableDensity);
    }
}
