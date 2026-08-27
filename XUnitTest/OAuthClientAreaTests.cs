using System;
using System.Collections.Generic;
using NewLife.Web;
using Xunit;

namespace XUnitTest;

/// <summary>OAuthClient 地区字段解析单元测试。覆盖 areaid/areaName 捕获与省市区拼接</summary>
public class OAuthClientAreaTests
{
    /// <summary>测试用客户端，暴露受保护的 OnGetInfo 方法</summary>
    private class AreaTestClient : OAuthClient
    {
        public void CallOnGetInfo(IDictionary<String, String> dic) => OnGetInfo(dic);
    }

    [Theory(DisplayName = "解析地区编码_各字段名")]
    [InlineData("areaid", "310116", 310116)]
    [InlineData("area_id", "310116", 310116)]
    [InlineData("areacode", "310116", 310116)]
    [InlineData("area_code", "310116", 310116)]
    public void OnGetInfo_AreaId(String key, String value, Int32 expected)
    {
        var client = new AreaTestClient();
        client.CallOnGetInfo(new Dictionary<String, String> { [key] = value });

        Assert.Equal(expected, client.AreaId);
    }

    [Theory(DisplayName = "解析地区编码_非法值返回0")]
    [InlineData("abc")]
    [InlineData("")]
    [InlineData("  ")]
    public void OnGetInfo_AreaId_Invalid(String value)
    {
        var client = new AreaTestClient();
        client.CallOnGetInfo(new Dictionary<String, String> { ["areaid"] = value });

        Assert.Equal(0, client.AreaId);
    }

    [Theory(DisplayName = "解析地区名_各字段名")]
    [InlineData("areaName", "上海市/金山区", "上海市/金山区")]
    [InlineData("area_name", "金山区", "金山区")]
    public void OnGetInfo_AreaName(String key, String value, String expected)
    {
        var client = new AreaTestClient();
        client.CallOnGetInfo(new Dictionary<String, String> { [key] = value });

        Assert.Equal(expected, client.AreaName);
    }

    [Theory(DisplayName = "解析地区名_省市区拼接")]
    [InlineData("上海市", "金山区", "上海市/金山区")]
    [InlineData("上海市", "", "上海市")]
    [InlineData("", "金山区", "金山区")]
    public void OnGetInfo_AreaName_FromProvinceCity(String province, String city, String expected)
    {
        var client = new AreaTestClient();
        client.CallOnGetInfo(new Dictionary<String, String>
        {
            ["province"] = province,
            ["city"] = city,
        });

        Assert.Equal(expected, client.AreaName);
    }

    [Fact(DisplayName = "解析地区_编码与名称同时返回")]
    public void OnGetInfo_AreaIdAndName()
    {
        var client = new AreaTestClient();
        client.CallOnGetInfo(new Dictionary<String, String>
        {
            ["areaid"] = "310116",
            ["areaName"] = "上海市/金山区",
        });

        Assert.Equal(310116, client.AreaId);
        Assert.Equal("上海市/金山区", client.AreaName);
    }

    [Fact(DisplayName = "解析地区_名称优先于省市区拼接")]
    public void OnGetInfo_AreaName_PriorityOverProvinceCity()
    {
        var client = new AreaTestClient();
        client.CallOnGetInfo(new Dictionary<String, String>
        {
            ["province"] = "上海市",
            ["city"] = "金山区",
            ["areaName"] = "上海市/浦东新区",
        });

        Assert.Equal("上海市/浦东新区", client.AreaName);
    }

    [Fact(DisplayName = "解析地区_空字典不改变地区字段")]
    public void OnGetInfo_EmptyDict()
    {
        var client = new AreaTestClient();
        client.CallOnGetInfo(new Dictionary<String, String>());

        Assert.Equal(0, client.AreaId);
        Assert.Null(client.AreaName);
    }
}
