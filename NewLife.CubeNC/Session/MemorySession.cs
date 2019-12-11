using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace NewLife.Cube.Session
{
    /// <summary>内存会话</summary>
    public class MemorySession : ISession, ISession2
    {
        #region 属性
        /// <summary>会话标识</summary>
        public String Id { get; set; }

        /// <summary>可用</summary>
        public Boolean IsAvailable { get; } = true;

        /// <summary>所有键</summary>
        public IEnumerable<String> Keys => throw new NotImplementedException();
        #endregion

        #region 方法
        /// <summary>获取值</summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public Boolean TryGetValue(String key, out Object value) => throw new NotImplementedException();

        /// <summary>设置值</summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Set(String key, Object value) => throw new NotImplementedException();

        /// <summary>删除</summary>
        /// <param name="key"></param>
        public void Remove(String key) => throw new NotImplementedException();

        /// <summary>清空</summary>
        public void Clear() => throw new NotImplementedException();
        #endregion

        #region ISession
        Task ISession.CommitAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        Task ISession.LoadAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        void ISession.Set(String key, Byte[] value) => Set(key, value);

        Boolean ISession.TryGetValue(String key, out Byte[] value)
        {
            value = null;
            if (!TryGetValue(key, out var rs)) return false;

            value = rs as Byte[];

            return true;
        }
        #endregion
    }
}