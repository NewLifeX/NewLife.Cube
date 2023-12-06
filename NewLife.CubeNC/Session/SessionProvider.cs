using System.Collections.Concurrent;
using NewLife.Caching;

namespace NewLife.Cube;

/// <summary>基于内存实现的会话</summary>
public class SessionProvider
{
    /// <summary>有效期</summary>
    public TimeSpan Expire { get; set; } = TimeSpan.FromMinutes(20);

    /// <summary>会话存储</summary>
    public ICache Cache { get; set; } = new MemoryCache { Expire = 20 * 60, Period = 60 };

    /// <summary>根据会话标识获取session</summary>
    /// <param name="sessionKey"></param>
    /// <returns></returns>
    public virtual IDictionary<String, Object> GetSession(String sessionKey)
    {
        if (sessionKey.IsNullOrEmpty()) return null;

        // 采用哈希结构。内存缓存用并行字段，Redis用Set
        var dic = Cache.GetDictionary<Object>(sessionKey);
        Cache.SetExpire(sessionKey, Expire);

        //!! 临时修正可空字典的BUG
        if (Cache is MemoryCache mc && dic is not NullableDictionary2<String, Object>)
        {
            dic = new NullableDictionary2<String, Object>(dic, StringComparer.Ordinal);
            mc.Set(sessionKey, dic);
        }

        return dic;
    }
}


/// <summary>可空字典。获取数据时如果指定键不存在可返回空而不是抛出异常</summary>
/// <typeparam name="TKey"></typeparam>
/// <typeparam name="TValue"></typeparam>
class NullableDictionary2<TKey, TValue> : ConcurrentDictionary<TKey, TValue>, IDictionary<TKey, TValue> where TKey : notnull
{
    /// <summary>实例化一个可空字典</summary>
    public NullableDictionary2() { }

    /// <summary>指定比较器实例化一个可空字典</summary>
    /// <param name="comparer"></param>
    public NullableDictionary2(IEqualityComparer<TKey> comparer) : base(comparer) { }

    /// <summary>实例化一个可空字典</summary>
    /// <param name="dic"></param>
    public NullableDictionary2(IDictionary<TKey, TValue> dic) : base(dic) { }

    /// <summary>实例化一个可空字典</summary>
    /// <param name="dic"></param>
    /// <param name="comparer"></param>
    public NullableDictionary2(IDictionary<TKey, TValue> dic, IEqualityComparer<TKey> comparer) : base(dic, comparer) { }

    /// <summary>获取 或 设置 数据</summary>
    /// <param name="item"></param>
    /// <returns></returns>
    public new TValue this[TKey item]
    {
        get
        {
            if (TryGetValue(item, out var v)) return v;

            return default!;
        }
        set
        {
            base[item] = value;
        }
    }
}