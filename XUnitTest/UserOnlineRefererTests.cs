using System.ComponentModel;
using NewLife.Cube.Entity;
using Xunit;

namespace XUnitTest;

/// <summary>UserOnline 来源字段单元测试</summary>
/// <remarks>验证新增 Referer 字段的属性与索引器读写，用户在线会话的外部来源记录依赖该字段</remarks>
public class UserOnlineRefererTests
{
    [Fact]
    [DisplayName("UserOnline.Referer_属性读写")]
    public void Referer_Property_Roundtrip()
    {
        var online = new UserOnline();
        var refer = "https://erp.newlifex.com/dashboard";

        online.Referer = refer;

        Assert.Equal(refer, online.Referer);
    }

    [Fact]
    [DisplayName("UserOnline.Referer_索引器读写")]
    public void Referer_Indexer_Roundtrip()
    {
        var online = new UserOnline();
        var refer = "https://erp.newlifex.com/dashboard";

        online["Referer"] = refer;

        Assert.Equal(refer, online["Referer"] + "");
        Assert.Equal(refer, online.Referer);
    }

    [Fact]
    [DisplayName("UserOnline.Referer_默认空")]
    public void Referer_Default_IsEmpty()
    {
        var online = new UserOnline();

        Assert.Null(online.Referer);
    }
}
