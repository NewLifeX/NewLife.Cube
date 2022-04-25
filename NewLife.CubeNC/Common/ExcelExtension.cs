using System;
using System.Collections.Generic;

namespace NewLife.Cube.Common
{
    public static class ExcelExtension
    {
        /// <summary>
        /// 1、2、3 转换A、B、C
        /// <param name="index"></param>
        /// <returns></returns>
        public static string IndexToName(int index)
        {
            if (index < 0) { throw new Exception("参数非法！");
            }
            List<string> chars = new List<string>();
            do
            {
                if (chars.Count > 0) index--;
                string tempchar = "";
                if (chars.Count == 0)
                {
                    tempchar = ((char)(index % 26 + (int)'A' - 1)).ToString();
                }
                else
                {
                    tempchar = ((char)(index % 26 + (int)'A')).ToString();
                }
                chars.Insert(0, tempchar);
                index = (int)((index - index % 26) / 26);
            } while (index > 0);
            return String.Join(string.Empty, chars.ToArray());
        }
    }
}
