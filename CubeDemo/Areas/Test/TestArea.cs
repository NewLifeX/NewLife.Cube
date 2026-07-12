using System.ComponentModel;
using NewLife;
using NewLife.Cube;

namespace CubeDemo.Areas.Test;

/// <summary>测试中心区域。承载「字段类型全覆盖」测试台，用于验证前端默认模板对所有字段类型的渲染与提交。</summary>
[DisplayName("测试中心")]
[Menu(200)]
public class TestArea : AreaBase
{
    public TestArea() : base(nameof(TestArea).TrimEnd("Area")) { }
}
