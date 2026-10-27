using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.Repositories;
using NewLife.Log;
using NewLife.Serialization;
using NewLife.Threading;
using XCode.Membership;

namespace NewLife.Cube.Services;

/// <summary>在数据库中存储Xml</summary>
public class DbXmlRepository : DisposeBase, IXmlRepository
{
    private readonly String _key;
    private TimerX _timer;

    /// <summary>实例化</summary>
    /// <param name="key">密钥存储键，对应参数表中该分类下的条目名</param>
    public DbXmlRepository(String key)
    {
        if (key.IsNullOrEmpty()) throw new ArgumentNullException(nameof(key));

        XTrace.WriteLine(L.TLog("DataProtection persists keys in database, Key={0}", key));

        _key = key;

        _timer = new TimerX(TrimExpired, null, 1_000, 3600_000) { Async = true };
    }

    /// <summary>销毁</summary>
    /// <param name="disposing">是否释放托管资源</param>
    protected override void Dispose(Boolean disposing)
    {
        base.Dispose(disposing);

        _timer.TryDispose();
    }

    /// <summary>获取所有元素</summary>
    /// <returns>数据库中全部密钥 XML 元素</returns>
    public IReadOnlyCollection<XElement> GetAllElements() => GetAllElementsCore().ToList().AsReadOnly();

    /// <summary>遍历元素</summary>
    /// <returns>逐条读取的密钥 XML 元素序列</returns>
    private IEnumerable<XElement> GetAllElementsCore()
    {
        var list = Parameter.FindAllByUserID(0, _key);
        foreach (var item in list)
        {
            var value = item.GetValue() as String;
            if (!value.IsNullOrEmpty()) yield return XElement.Parse(value);
        }
    }

    /// <summary>存储元素</summary>
    /// <param name="element">待写入的密钥 XML 元素</param>
    /// <param name="friendlyName">友好名称，用作参数表条目名，便于排查</param>
    public void StoreElement(XElement element, String friendlyName)
    {
        var p = Parameter.GetOrAdd(0, _key, friendlyName);
        var value = element.ToString(SaveOptions.DisableFormatting);
        p.SetValue(value);
        p.Update();
    }

    /// <summary>自动删除已过期的key</summary>
    /// <param name="state">定时器状态对象，未使用</param>
    private void TrimExpired(Object state)
    {
        var list = Parameter.FindAllByUserID(0, _key);
        foreach (var item in list)
        {
            var value = item.GetValue() as String;
            if (!value.IsNullOrEmpty())
            {
                var xml = XmlParser.Decode(value);
                var expire = xml["expirationDate"].ToDateTime();
                if (expire.Year > 2000 && expire.AddMonths(1) < DateTime.Now)
                {
                    LogProvider.Provider.WriteLog("DataProtection", "Remove", true, L.TLog("Deleted expired data {0}", item.Name));

                    item.Delete();
                }
            }
        }
    }
}
