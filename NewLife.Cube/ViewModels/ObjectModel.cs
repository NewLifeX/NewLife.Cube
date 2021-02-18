using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using NewLife.Cube.Common;

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
    /// 属性信息，FormatType默认DefaultCase
    /// </summary>
    public class CubePropertyInfo
    {
        private String _name;
        public FormatType FormatType = FormatType.DefaultCase;

        public String Name
        {
            get => FormatName(_name);
            set => _name = value;
        }

        public String DisplayName { get; set; }
        public String Description { get; set; }
        [IgnoreDataMember]
        public Type PropertyType { get; set; }
        public String TypeStr { get; set; }

        /// <summary>根据小写和驼峰格式化名称</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private String FormatName(String name)
        {
            if (name.IsNullOrEmpty()) return name;

            if (FormatType == FormatType.LowerCase) return name.ToLower();
            if (FormatType!= FormatType.CamelCase) return name;
            if (name.EqualIgnoreCase("id")) return "id";
            if (name.Length < 2) return name.ToLower();
            return name.Substring(0, 1).ToLower() + name.Substring(1);
        }
    }
}