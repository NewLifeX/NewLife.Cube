using System;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using NewLife.Cube.Services;
using NewLife.Cube.Tests.Membership;
using XCode.Membership;
using Xunit;

namespace NewLife.Cube.Tests.Services;

/// <summary>
/// BASE-5 数据保护测试。覆盖密钥 XML 存入数据库、读回、同名覆盖，以及过期密钥自动清理。
/// </summary>
[Collection("TenantAuth")]
public class DbXmlRepositoryTests
{
    private readonly TenantAuthFixture _fx;

    /// <summary>实例化</summary>
    /// <param name="fixture">多租户认证夹具。复用其 SQLite 库（参数表）</param>
    public DbXmlRepositoryTests(TenantAuthFixture fixture) => _fx = fixture;

    private static String Unique(String prefix) => prefix + Guid.NewGuid().ToString("N")[..8];

    [Fact(DisplayName = "数据保护密钥_写入数据库后可读回")]
    public void StoreAndLoad_RoundTrip()
    {
        var key = Unique("dpapi");
        using var repo = new DbXmlRepository(key);

        var xml = XElement.Parse("<key id=\"abc\"><value>secret</value></key>");
        repo.StoreElement(xml, "key-abc");

        var list = repo.GetAllElements();

        Assert.Single(list);
        Assert.Equal("abc", list.First().Attribute("id")?.Value);
        Assert.Equal("secret", list.First().Element("value")?.Value);
    }

    [Fact(DisplayName = "数据保护密钥_同名写入覆盖而不新增")]
    public void StoreElement_SameName_Overwrites()
    {
        var key = Unique("dpapi");
        using var repo = new DbXmlRepository(key);

        repo.StoreElement(XElement.Parse("<key id=\"v1\" />"), "same");
        repo.StoreElement(XElement.Parse("<key id=\"v2\" />"), "same");

        var list = repo.GetAllElements();
        Assert.Single(list);
        Assert.Equal("v2", list.First().Attribute("id")?.Value);

        var rows = Parameter.FindAllByUserID(0, key);
        Assert.Single(rows);
    }

    [Fact(DisplayName = "数据保护密钥_多个密钥全部可读")]
    public void StoreElement_MultipleKeys_AllReturned()
    {
        var key = Unique("dpapi");
        using var repo = new DbXmlRepository(key);

        repo.StoreElement(XElement.Parse("<key id=\"a\" />"), "a");
        repo.StoreElement(XElement.Parse("<key id=\"b\" />"), "b");

        var ids = repo.GetAllElements().Select(e => e.Attribute("id")?.Value).OrderBy(e => e).ToArray();

        Assert.Equal(["a", "b"], ids);
    }

    [Fact(DisplayName = "数据保护密钥_过期元素被自动清理")]
    public void TrimExpired_RemovesExpiredKeys()
    {
        var key = Unique("dpapi");
        using var repo = new DbXmlRepository(key);

        var expired = new XElement("key",
            new XAttribute("id", "old"),
            new XElement("expirationDate", DateTime.Now.AddMonths(-3).ToString("O")));
        var alive = new XElement("key",
            new XAttribute("id", "new"),
            new XElement("expirationDate", DateTime.Now.AddMonths(3).ToString("O")));

        repo.StoreElement(expired, "old-key");
        repo.StoreElement(alive, "new-key");

        // 定时清理 1 秒后首次运行，轮询等待
        var ids = Array.Empty<String>();
        var end = Environment.TickCount64 + 6000;
        while (Environment.TickCount64 < end)
        {
            ids = repo.GetAllElements().Select(e => e.Attribute("id")?.Value).ToArray();
            if (!ids.Contains("old")) break;
            Thread.Sleep(200);
        }

        Assert.DoesNotContain("old", ids);
        Assert.Contains("new", ids);
    }

    [Fact(DisplayName = "数据保护密钥_键名为空被拒绝")]
    public void Ctor_EmptyKey_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new DbXmlRepository(null));
        Assert.Throws<ArgumentNullException>(() => new DbXmlRepository(""));
    }
}
