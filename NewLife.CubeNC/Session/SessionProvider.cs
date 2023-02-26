using NewLife.Caching;
using NewLife.Collections;

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
        if (Cache is MemoryCache mc && dic is not NullableDictionary<String, Object>)
        {
            dic = new NullableDictionary<String, Object>(dic, StringComparer.Ordinal);
            mc.Set(sessionKey, dic);
        }

        return dic;
    }
}