using System.ComponentModel;
using NewLife.Cube;
using Xunit;

namespace XUnitTest;

/// <summary>PageSetting 页面设置单元测试</summary>
public class PageSettingTests
{
    [Fact]
    [DisplayName("PageSetting 默认值验证")]
    public void DefaultValues_AreCorrect()
    {
        var setting = new PageSetting();

        Assert.Null(setting.NavView);
        Assert.True(setting.EnableNavbar);
        Assert.True(setting.EnableToolbar);
        Assert.True(setting.EnableAdd);
        Assert.True(setting.EnableKey);
        Assert.True(setting.EnableSelect);
        Assert.True(setting.EnableFooter);
        Assert.False(setting.IsReadOnly);
        Assert.True(setting.EnableTableDoubleClick);
        Assert.True(setting.OrderByKey);
        Assert.True(setting.DoubleDelete);
        Assert.True(setting.EnableTotalCount);
    }

    [Fact]
    [DisplayName("PageSetting 属性可修改")]
    public void Properties_CanBeModified()
    {
        var setting = new PageSetting
        {
            NavView = "_Nav",
            EnableNavbar = false,
            EnableToolbar = false,
            EnableAdd = false,
            EnableKey = false,
            EnableSelect = false,
            EnableFooter = false,
            IsReadOnly = true,
            EnableTableDoubleClick = false,
            OrderByKey = false,
            DoubleDelete = false,
            EnableTotalCount = false
        };

        Assert.Equal("_Nav", setting.NavView);
        Assert.False(setting.EnableNavbar);
        Assert.False(setting.EnableToolbar);
        Assert.False(setting.EnableAdd);
        Assert.False(setting.EnableKey);
        Assert.False(setting.EnableSelect);
        Assert.False(setting.EnableFooter);
        Assert.True(setting.IsReadOnly);
        Assert.False(setting.EnableTableDoubleClick);
        Assert.False(setting.OrderByKey);
        Assert.False(setting.DoubleDelete);
        Assert.False(setting.EnableTotalCount);
    }

    [Fact]
    [DisplayName("PageSetting 静态全局实例为单例")]
    public void Global_IsSingleton()
    {
        var global1 = PageSetting.Global;
        var global2 = PageSetting.Global;

        Assert.Same(global1, global2);
    }

    [Fact]
    [DisplayName("PageSetting 克隆后与原实例独立")]
    public void Clone_IsIndependentCopy()
    {
        var original = new PageSetting
        {
            EnableNavbar = false,
            EnableAdd = false,
            IsReadOnly = true
        };

        var cloned = original.Clone();

        Assert.NotNull(cloned);
        Assert.Equal(original.EnableNavbar, cloned.EnableNavbar);
        Assert.Equal(original.EnableAdd, cloned.EnableAdd);
        Assert.Equal(original.IsReadOnly, cloned.IsReadOnly);

        // 修改克隆不影响原实例
        cloned.EnableNavbar = true;
        Assert.False(original.EnableNavbar);
        Assert.True(cloned.EnableNavbar);
    }
}
