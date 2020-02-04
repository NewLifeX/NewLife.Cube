using System;
using System.Web;
using NewLife.Data;

namespace NewLife.Cube.Extensions
{
    class SessionExtend : IExtend
    {
        public HttpSessionStateBase Session { get; set; }

        public Object this[String key]
        {
            get => Session[key];
            set => Session[key] = value;
        }
    }
}