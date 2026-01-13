using System.Text;
using NewLife.Security;

namespace NewLife.Cube.Services
{
    /// <summary>短信服务帮助类</summary>
    public class SmsVerifyCodeService
    {
        /// <summary>验证手机号格式是否正确</summary>
        /// <param name="phone">手机号</param>
        /// <returns>格式正确返回true</returns>
        public static Boolean IsValidPhone(String phone)
            => !String.IsNullOrWhiteSpace(phone)
               && phone.Length >= 11
               && phone.All(c => c >= '0' && c <= '9');

        /// <summary>生成验证码</summary>
        public static String GenerateVerifyCode()
        {
            var codeLength = CubeSetting.Current.SmsCodeLength;
            var seed = $"{Rand.Next(Int32.MaxValue / 10, Int32.MaxValue)}";
            var sb = new StringBuilder();
            for (var i = 0; i < codeLength; i++)
            {
                var index = i % seed.Length;
                var c = seed[index];//避免超长，超过长度时会循环取
                sb.Append(c);
            }
            var code = sb.ToString();
            return code;
        }
    }
}
