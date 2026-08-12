using System;
using System.Collections.Generic;
using NewLife.Cube.Services;
using Xunit;

namespace XUnitTest;

/// <summary>邮件服务单元测试。覆盖验证码生成逻辑</summary>
public class MailServiceTests
{
    [Theory(DisplayName = "生成验证码_长度正确")]
    [InlineData(4)]
    [InlineData(6)]
    [InlineData(0)]
    [InlineData(-1)]
    public void GenerateVerifyCode_Length(Int32 length)
    {
        var expect = length <= 0 ? MailService.DefaultCodeLength : length;

        var code = MailService.GenerateVerifyCode(length);

        Assert.Equal(expect, code.Length);
    }

    [Fact(DisplayName = "生成验证码_全部为数字")]
    public void GenerateVerifyCode_AllDigits()
    {
        var code = MailService.GenerateVerifyCode(6);

        Assert.All(code, c => Assert.True(c >= '0' && c <= '9', $"出现非数字字符[{c}]"));
    }

    [Fact(DisplayName = "生成验证码_多次生成不重复")]
    public void GenerateVerifyCode_Random()
    {
        var set = new HashSet<String>();
        for (var i = 0; i < 100; i++)
        {
            set.Add(MailService.GenerateVerifyCode(6));
        }

        // 100 个 6 位验证码理论上几乎不可能全部重复，仅统计分布
        Assert.True(set.Count > 90, $"验证码重复度过高，仅生成[{set.Count}]个不同验证码");
    }
}
