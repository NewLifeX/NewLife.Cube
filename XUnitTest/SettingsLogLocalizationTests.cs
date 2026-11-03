using System;
using System.ComponentModel;
using System.Linq;
using NewLife;
using NewLife.Configuration;
using NewLife.Cube;
using NewLife.Cube.ViewModels;
using Xunit;

namespace XUnitTest;

/// <summary>设置页审计日志本地化测试。验证类别与字段明细取的是当前语言的显示名，而不是特性源文</summary>
/// <remarks>
/// 背景：设置保存时，类别取配置类的 [DisplayName]，明细取属性的 [DisplayName]。
/// 两处原先直接写源文，导致中文界面下日志仍显示 "System settings" / "Application instance"。
/// 现改为经 L.T 翻译（类别）与 DataField.Localize（字段名），与设置页表单保持一致。
/// </remarks>
public class SettingsLogLocalizationTests
{
    #region 测试配置
    /// <summary>日志本地化测试配置</summary>
    [DisplayName("测试日志配置")]
    public class LogSampleConfig : Config<LogSampleConfig>
    {
        /// <summary>应用实例。源文显示名与上游设置类一致，用于验证翻译链路</summary>
        [DisplayName("Application instance")]
        public Int32 AppId { get; set; }
    }
    #endregion

    [Fact(DisplayName = "设置日志 - 类别取当前语言的类型显示名")]
    public void LogCategory_Localized()
    {
        // 设置页日志类别来自配置类 DisplayName，写库前需翻译
        Assert.Equal("系统设置", Localization.Translate("System settings"));
        Assert.Equal("核心设置", Localization.Translate("Core settings"));
    }

    [Fact(DisplayName = "设置日志 - 字段明细与表单同款本地化显示名")]
    public void LogFieldName_Localized()
    {
        var fields = DataField.Localize(ObjectController<LogSampleConfig>.GetMembers(typeof(LogSampleConfig)));
        var df = fields.First(e => e.Name == nameof(LogSampleConfig.AppId));

        // 源文为英文特性，本地化后与设置页表单显示一致
        Assert.Equal("应用实例", df.DisplayName);
    }
}
