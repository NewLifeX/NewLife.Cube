using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.Repositories;
using XCode.Membership;

namespace NewLife.Cube.Services;

/// <summary>在数据库中存储Xml</summary>
public class DbXmlRepository : IXmlRepository
{
    private readonly String _key;

    /// <summary>实例化</summary>
    /// <param name="key"></param>
    public DbXmlRepository(String key)
    {
        if (key.IsNullOrEmpty()) throw new ArgumentNullException(nameof(key));

        _key = key;
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
}
