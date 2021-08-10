using System;
using System.Collections.Generic;
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
        /// <summary>名称</summary>
        public String Name { get; set; }

        /// <summary>显示名</summary>
        public String DisplayName { get; set; }

        /// <summary>描述</summary>
        public String Description { get; set; }

        /// <summary>属性类型</summary>
        [IgnoreDataMember]
        public Type PropertyType { get; set; }

        /// <summary>类型</summary>
        public String Type => PropertyType?.Name;

        /// <summary>类型（字符串）</summary>
        [Obsolete]
        public String TypeStr => PropertyType?.Name;

        /// <summary>类别</summary>
        public String Category { get; set; }

        /// <summary>格式化类型</summary>
        public FormatType FormatType { get; set; } = FormatType.DefaultCase;

        /// <summary>克隆</summary>
        /// <returns></returns>
        public CubePropertyInfo Clone() => new()
        {
            Name = Name,
            DisplayName = DisplayName,
            Description = Description,
            PropertyType = PropertyType,
            Category = Category,
            FormatType = FormatType.DefaultCase,
        };
    }
}