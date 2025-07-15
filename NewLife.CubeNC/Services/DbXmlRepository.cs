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
    /// <param name="key"></param>
    public DbXmlRepository(String key)
    {
        if (key.IsNullOrEmpty()) throw new ArgumentNullException(nameof(key));

        XTrace.WriteLine("DataProtection使用数据库持久化密钥，Key={0}", key);

        _key = key;

        _timer = new TimerX(TrimExpired, null, 1_000, 3600_000) { Async = true };
    }

    /// <summary>销毁</summary>
    /// <param name="disposing"></param>
    protected override void Dispose(Boolean disposing)
    {
        base.Dispose(disposing);

        _timer.TryDispose();
    }

    /// <summary>获取所有元素</summary>
    /// <returns></returns>
    public IReadOnlyCollection<XElement> GetAllElements() => GetAllElementsCore().ToList().AsReadOnly();

    /// <summary>遍历元素</summary>
    /// <returns></returns>
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
    /// <param name="element"></param>
    /// <param name="friendlyName"></param>
    public void StoreElement(XElement element, String friendlyName)
    {
        var p = Parameter.GetOrAdd(0, _key, friendlyName);
        var value = element.ToString(SaveOptions.DisableFormatting);
        p.SetValue(value);
        p.Update();
    }

    /// <summary>自动删除已过期的key</summary>
    /// <param name="state"></param>
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
                    LogProvider.Provider.WriteLog("DataProtection", "Remove", true, $"删除过期数据 {item.Name}");

                    item.Delete();
                }
            }
        }
    }
}
