using System;
using NewLife.Cube;
using Xunit;

namespace XUnitTest;

/// <summary>空富文本判定测试。XCode 侧实体保存时已自动清理；此处覆盖魔方 SSO 同步使用的判定函数</summary>
public class EmptyHtmlHelperTests
{
    [Theory(DisplayName = "判断空富文本：仅空段落/换行/空白视为空")]
    [InlineData("<p><br></p>", true)]
    [InlineData("<p><br/></p>", true)]
    [InlineData("<p><br></p><p><br></p>", true)]
    [InlineData("<p>&nbsp;</p>", true)]
    [InlineData("<p></p>", true)]
    [InlineData("<div><br></div>", true)]
    [InlineData("<p>hello</p>", false)]
    [InlineData("<p><img src=\"a.png\" /></p>", false)]
    [InlineData("", false)]
    [InlineData("   ", true)]
    [InlineData(null, false)]
    public void IsEmptyHtml_Works(String? html, Boolean expected)
    {
        Assert.Equal(expected, EmptyHtmlHelper.IsEmptyHtml(html));
    }
}
