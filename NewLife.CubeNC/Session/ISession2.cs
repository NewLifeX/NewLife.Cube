using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NewLife.Cube
{
    /// <summary>扩展Session接口</summary>
    public interface ISession2
    {
        /// <summary>会话标识</summary>
        String Id { get; }

        /// <summary>可用</summary>
        Boolean IsAvailable { get; }

        /// <summary>所有键</summary>
        IEnumerable<String> Keys { get; }

        /// <summary>获取值</summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        Boolean TryGetValue(String key, out Object value);

        /// <summary>设置值</summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        void Set(String key, Object value);

        /// <summary>删除</summary>
        /// <param name="key"></param>
        void Remove(String key);

        /// <summary>清空</summary>
        void Clear();
    }
}