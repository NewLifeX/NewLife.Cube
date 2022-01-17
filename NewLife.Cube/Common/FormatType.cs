using System;

namespace NewLife.Cube.Common
{
    /// <summary>
    /// 格式化类型
    /// </summary>
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

    /// <summary>
    /// 格式化助手
    /// </summary>
    public static class FormatHelper
    {
        /// <summary>根据小写和驼峰格式化名称</summary>
        /// <param name="name"></param>
        /// <param name="formatType"></param>
        /// <returns></returns>
        public static String FormatName(this String name, FormatType formatType)
        {
            if (name.IsNullOrEmpty()) return name;

            if (formatType == FormatType.LowerCase) return name.ToLower();
            if (formatType != FormatType.CamelCase) return name;
            if (name.EqualIgnoreCase("id")) return "id";
            if (name.Length < 2) return name.ToLower();
            return name[..1].ToLower() + name[1..];
        }
    }
}
