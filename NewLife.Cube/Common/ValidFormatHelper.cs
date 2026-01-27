using System.Numerics;
using System.Text.RegularExpressions;
using Microsoft.Extensions.FileSystemGlobbing.Internal;

namespace NewLife.Cube.Common
{
    /// <summary>格式检查</summary>
    public class ValidFormatHelper
    {
        /// <summary>邮箱格式：包含.和@</summary>
        /// <returns></returns>
        public static Boolean IsEmail(String email)
            => !String.IsNullOrWhiteSpace(email)
               && Regex.Matches(email, @"\w[-\w.+]*@([A-Za-z0-9][-A-Za-z0-9]+\.)+[A-Za-z]{2,14}").Count > 0;


        /// <summary>验证手机号格式是否正确</summary>
        /// <param name="phone">手机号11位，且全为数字</param>
        /// <returns>格式正确返回true</returns>
        public static Boolean IsMobile(String phone)
            => !String.IsNullOrWhiteSpace(phone)
               && Regex.IsMatch(phone, @"^1[3-9]\d{9}$");
    }
}
