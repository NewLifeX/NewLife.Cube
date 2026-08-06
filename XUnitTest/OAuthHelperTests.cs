using System;
using NewLife.Cube.Web;
using Xunit;

namespace XUnitTest;

/// <summary>OAuth 助手单元测试。覆盖授权码过期/无效异常判定</summary>
public class OAuthHelperTests
{
    [Theory(DisplayName = "IsCodeExpired_过期或无效消息_判定为真")]
    [InlineData("Code已过期！")]
    [InlineData("Code已过期！ (Parameter 'code')")]
    [InlineData("Code无效！")]
    [InlineData("授权码已失效")]
    [InlineData("code expired")]
    [InlineData("invalid code")]
    [InlineData("invalid_grant")]
    public void IsCodeExpired_ExpiredOrInvalidMessage_ReturnsTrue(String message)
    {
        Assert.True(OAuthHelper.IsCodeExpired(new InvalidOperationException(message)));
    }

    [Theory(DisplayName = "IsCodeExpired_无关消息或空_判定为假")]
    [InlineData("")]
    [InlineData("invalid client_secret")]
    [InlineData("用户[mydevice]不存在")]
    [InlineData("network error")]
    [InlineData("invalid response")]
    public void IsCodeExpired_UnrelatedMessage_ReturnsFalse(String message)
    {
        Assert.False(OAuthHelper.IsCodeExpired(new InvalidOperationException(message)));
    }

    [Fact(DisplayName = "IsCodeExpired_Null异常_返回False")]
    public void IsCodeExpired_Null_ReturnsFalse()
    {
        Assert.False(OAuthHelper.IsCodeExpired(null));
    }
}
