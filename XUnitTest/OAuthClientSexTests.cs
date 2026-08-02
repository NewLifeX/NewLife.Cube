using System;
using NewLife.Web;
using XCode.Membership;
using Xunit;

namespace XUnitTest;

/// <summary>OAuthClient 性别解析单元测试。覆盖数字、中文枚举、英文枚举的兼容解析</summary>
public class OAuthClientSexTests
{
    /// <summary>测试用客户端，暴露受保护的 ParseSex 方法</summary>
    private class SexTestClient : OAuthClient
    {
        public SexKinds CallParseSex(String value) => ParseSex(value);
    }

    [Theory(DisplayName = "解析性别_数字字符串")]
    [InlineData("0", SexKinds.未知)]
    [InlineData("1", SexKinds.男)]
    [InlineData("2", SexKinds.女)]
    [InlineData(" 1 ", SexKinds.男)]
    public void ParseSex_Number(String value, SexKinds expected)
    {
        var client = new SexTestClient();
        Assert.Equal(expected, client.CallParseSex(value));
    }

    [Theory(DisplayName = "解析性别_中文枚举")]
    [InlineData("男", SexKinds.男)]
    [InlineData("女", SexKinds.女)]
    [InlineData("未知", SexKinds.未知)]
    public void ParseSex_Chinese(String value, SexKinds expected)
    {
        var client = new SexTestClient();
        Assert.Equal(expected, client.CallParseSex(value));
    }

    [Theory(DisplayName = "解析性别_英文枚举")]
    [InlineData("male", SexKinds.男)]
    [InlineData("female", SexKinds.女)]
    [InlineData("m", SexKinds.男)]
    [InlineData("f", SexKinds.女)]
    public void ParseSex_English(String value, SexKinds expected)
    {
        var client = new SexTestClient();
        Assert.Equal(expected, client.CallParseSex(value));
    }

    [Theory(DisplayName = "解析性别_非法值返回未知")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("-1")]
    public void ParseSex_Invalid(String value)
    {
        var client = new SexTestClient();
        Assert.Equal(SexKinds.未知, client.CallParseSex(value));
    }
}
