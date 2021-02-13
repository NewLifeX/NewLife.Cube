using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;

namespace NewLife.Cube.ViewModels
{
    /// <summary>对象模型</summary>
    public class ObjectModel
    {
        /// <summary>对象值</summary>
        public Object Value { get; set; }

        /// <summary>属性集合</summary>
        public IDictionary<String, List<CubePropertyInfo>> Properties { get; set; }
    }

    /// <summary>
    /// 属性信息
    /// </summary>
    public class CubePropertyInfo
    {
        public String Name { get; set; }
        public String DisplayName { get; set; }
        public String Description { get; set; }
        [IgnoreDataMember]
        public Type PropertyType { get; set; }
        public String TypeStr { get; set; }
    }
}