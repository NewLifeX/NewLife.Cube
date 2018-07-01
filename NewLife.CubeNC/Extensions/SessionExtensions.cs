using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace NewLife.CubeNC.Extensions
{
    public static class SessionExtensions
    {
        public static T Get<T>(this ISession session, string key) where T:class => session.Get(key).GetObject<T>();
    }
}
