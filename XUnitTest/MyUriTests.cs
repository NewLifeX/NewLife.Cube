using System;
using Xunit;

namespace XUnitTest;

public class MyUriTests
{
    [Theory]
    [InlineData("http://localhost:8080/cube/info", "http", "localhost", 8080, "/cube/info")]
    [InlineData("http://localhost:8080/", "http", "localhost", 8080, "/")]
    [InlineData("Http://localhost/", "Http", "localhost", 0, "/")]
    [InlineData("Http://localhost", "Http", "localhost", 0, null)]
    [InlineData("localhost:8080/cube/info", null, "localhost", 8080, "/cube/info")]
    [InlineData("localhost:8080/", null, "localhost", 8080, "/")]
    [InlineData("localhost/", null, "localhost", 0, "/")]
    [InlineData("localhost", null, "localhost", 0, null)]
    public void Parse(String url, String schema, String host, Int32 port, String path)
    {
        var uri = new MyUri(url);
        Assert.Equal(schema, uri.Scheme);
        Assert.Equal(host, uri.Host);
        Assert.Equal(port, uri.Port);
        Assert.Equal(path, uri.PathAndQuery);
    }
}

class MyUri
{
    public String Scheme { get; set; }
    public String Host { get; set; }
    public Int32 Port { get; set; }
    public String PathAndQuery { get; set; }

    public MyUri(String value)
    {
        // 先处理头尾，再处理中间的主机和端口
        var p = value.IndexOf("://");
        if (p >= 0)
        {
            Scheme = value[..p];
            p += 3;
        }
        else
            p = 0;

        var p2 = value.IndexOf('/', p);
        if (p2 > 0)
        {
            PathAndQuery = value[p2..];
            value = value[p..p2];
        }
        else
            value = value[p..];

        // 拆分主机和端口，注意IPv6地址
        p2 = value.LastIndexOf(':');
        if (p2 > 0)
        {
            Host = value[..p2];
            Port = value[(p2 + 1)..].ToInt();
        }
        else
        {
            Host = value;
        }
    }
}
