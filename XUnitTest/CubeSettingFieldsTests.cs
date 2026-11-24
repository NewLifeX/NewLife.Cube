using System;
using System.Collections.Generic;
using System.Linq;
using NewLife.Cube;
using NewLife.Cube.ViewModels;
using Xunit;

namespace XUnitTest;

/// <summary>魔方设置下拉数据源测试</summary>
public class CubeSettingFieldsTests
{
    [Fact(DisplayName = "枚举型配置项生成下拉数据源")]
    public void Apply_EnumSettings_HaveDataSource()
    {
        var fields = typeof(CubeSetting).GetProperties()
            .Select(e => new DataField { Name = e.Name })
            .ToList();

        CubeSettingFields.Apply(fields);

        foreach (var name in new[] { "SecurityMode", "XFrameOptions", "RobotError", "EnableUserOnline", "CaptchaScene", "CaptchaRiskThreshold", "AttachmentStorage", "TableStyle", "TableDensity" })
        {
            var field = fields.First(e => e.Name == name);
            Assert.NotNull(field.DataSource);
            Assert.Equal("singleSelect", field.ItemType);

            var ds = field.DataSource(null);
            Assert.NotNull(ds);
            Assert.True(ds.Count > 0);
        }
    }

    [Fact(DisplayName = "安全防御模式下拉包含全部档位")]
    public void Apply_SecurityMode_AllOptions()
    {
        var fields = new List<DataField> { new() { Name = "SecurityMode" } };

        CubeSettingFields.Apply(fields);

        var ds = fields[0].DataSource(null);
        Assert.Equal(["0", "1", "2", "3"], ds.Keys.Cast<Object>().Select(e => e + "").ToArray());
    }
}
