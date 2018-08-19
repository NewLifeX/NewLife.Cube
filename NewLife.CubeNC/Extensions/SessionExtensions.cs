using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace NewLife.CubeNC.Extensions
{
    public static class SessionExtensions
    {
        /// <summary>
        /// 从session中获取对象
        /// </summary>
        /// <typeparam name="T">一定要传递可初始化的类型，否则会因为不能创建类型的实例报错</typeparam>
        /// <param name="session"></param>
        /// <param name="key"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static T Get<T>(this ISession session, String key, Type type = null) where T:class
            //, new() 
            => (T)(Object)session.Get(key, typeof(T));

        /// <summary>
        /// 从session中获取对象
        /// </summary>
        /// <param name="session"></param>
        /// <param name="key"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Object Get(this ISession session, String key, Type type) => session.Get(key).GetObject(type);
    }
}
