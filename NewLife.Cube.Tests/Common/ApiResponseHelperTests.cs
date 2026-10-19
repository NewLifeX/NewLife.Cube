using System.Globalization;
using NewLife;
using NewLife.Cube.Extensions;
using Xunit;

namespace NewLife.Cube.Tests.Common;

/// <summary>API 响应本地化测试</summary>
[CollectionDefinition("ApiResponseLocalization", DisableParallelization = true)]
public class ApiResponseLocalizationCollection { }

[Collection("ApiResponseLocalization")]
public class ApiResponseHelperTests
{
    [Fact(DisplayName = "参数错误_带参数名_保留默认提示")]
    public void ParaResponseWithName()
    {
        var old = Localization.Culture;
        try
        {
            Localization.Culture = new CultureInfo("en-US");
            var rs = 0.ToParaApiResponse("id");

            Assert.Equal("Invalid request parameter:id", rs.Message);
        }
        finally
        {
            Localization.Culture = old;
        }
    }

    [Fact(DisplayName = "参数错误_中文提示")]
    public void ParaResponseChinese()
    {
        var old = Localization.Culture;
        try
        {
            Localization.Culture = new CultureInfo("zh-CN");
            var rs = 0.ToParaApiResponse("id");

            Assert.Equal("请求参数错误:id", rs.Message);
        }
        finally
        {
            Localization.Culture = old;
        }
    }
}
