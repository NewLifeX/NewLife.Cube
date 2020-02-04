using System;
using System.Collections;
using NewLife.Data;

namespace NewLife.Cube.Extensions
{
    class ItemsExtend : IExtend
    {
        public IDictionary Items { get; set; }

        public Object this[String key]
        {
            get => Items[key];
            set => Items[key] = value;
        }
    }
}