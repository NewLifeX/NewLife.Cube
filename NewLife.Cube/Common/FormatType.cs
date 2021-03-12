using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NewLife.Cube.Common
{
    public enum FormatType
    {
        /// <summary>
        /// 小驼峰，
        /// </summary>
        CamelCase = 0,

        /// <summary>
        /// 小写
        /// </summary>
        LowerCase = 1,

        /// <summary>
        /// 保持默认
        /// </summary>
        DefaultCase = 2
    }

    public static class FormatString
    {
        /// <summary>根据小写和驼峰格式化名称</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static String FormatName(this String name, FormatType formatType)
        {
            if (name.IsNullOrEmpty()) return name;

            if (formatType == FormatType.LowerCase) return name.ToLower();
            if (formatType != FormatType.CamelCase) return name;
            if (name.EqualIgnoreCase("id")) return "id";
            if (name.Length < 2) return name.ToLower();
            return name.Substring(0, 1).ToLower() + name.Substring(1);
        }
    }
}
