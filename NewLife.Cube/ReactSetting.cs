using System.ComponentModel;
using NewLife.Configuration;
using XCode.Configuration;

namespace NewLife.Cube;

/// <summary>React皮肤设置</summary>
[DisplayName("React皮肤设置")]
[Config("React")]
public class ReactSetting : Config<ReactSetting>
{
    /// <summary>指向数据库参数字典表</summary>
    static ReactSetting() => Provider = new DbConfigProvider { UserId = 0, Category = "React" };

    #region 表单
    /// <summary>表单风格。inline-三栏同排（对齐MVC），vertical-标签一行控件一行（antd6风格）</summary>
    [Description("表单风格。inline-三栏同排（对齐MVC），vertical-标签一行控件一行（antd6风格）")]
    [Category("表单")]
    public String FormStyle { get; set; } = "inline";

    /// <summary>字段注释显示方式。1-标签后小字，2-标签后问号图标悬浮提示，0-不显示</summary>
    [Description("字段注释显示方式。1-标签后小字，2-标签后问号图标悬浮提示，0-不显示")]
    [Category("表单")]
    public Int32 DescMode { get; set; } = 1;
    #endregion

    #region 导航
    /// <summary>配置导航排开。true-魔方设置等配置页一字排开，false-核心配置+更多下拉</summary>
    [Description("配置导航排开。true-魔方设置等配置页一字排开，false-核心配置+更多下拉")]
    [Category("导航")]
    public Boolean ConfigNavFlat { get; set; } = true;
    #endregion
}
