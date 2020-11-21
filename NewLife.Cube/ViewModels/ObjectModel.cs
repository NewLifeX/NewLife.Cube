using System;
using System.Collections.Generic;
using System.Reflection;

namespace NewLife.Cube.ViewModels
{
    /// <summary>对象模型</summary>
    public class ObjectModel
    {
        /// <summary>对象值</summary>
        public Object Value { get; set; }

        /// <summary>属性集合</summary>
        public IDictionary<String, List<PropertyInfo>> Properties { get; set; }
    }
}