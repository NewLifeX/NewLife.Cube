using NewLife.Cube.ViewModels;
using Xunit;

namespace NewLife.Cube.Tests.ViewModels;

/// <summary>覆盖 <see cref="DataField.ValueField"/> 取值字段元数据：GetPage 序列化输出，驱动前端单元格取值优先。</summary>
public class DataFieldValueFieldTests
{
    [Fact(DisplayName = "ToDictionary：设置 ValueField 时输出 valueField")]
    public void ToDictionary_Outputs_ValueField()
    {
        var lf = new ListField { Name = "Name", ValueField = "DisplayName" };

        var dic = lf.ToDictionary();

        Assert.Equal("Name", dic["name"]);
        Assert.Equal("DisplayName", dic["valueField"]);
    }

    [Fact(DisplayName = "ToDictionary：未设置 ValueField 时不输出 valueField")]
    public void ToDictionary_Skips_Empty_ValueField()
    {
        var lf = new ListField { Name = "Name" };

        var dic = lf.ToDictionary();

        Assert.False(dic.ContainsKey("valueField"));
    }
}
