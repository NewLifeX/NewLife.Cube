using System;
using System.Text.RegularExpressions;

namespace NewLife.Cube.Services
{
    /// <summary>
    /// 密码服务
    /// </summary>
    public class PasswordService
    {
        private Regex _regex;
        private String _old;

        /// <summary>验证密码强度</summary>
        /// <param name="password"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public Boolean Valid(String password)
        {
            if (password == null) throw new ArgumentNullException(nameof(password));

            // 没有设置密码强度，直接通过
            var set = CubeSetting.Current;
            if (set.PaswordStrength.IsNullOrEmpty() || set.PaswordStrength == "*") return true;

            if (_regex == null || set.PaswordStrength != _old)
            {
                _regex = new Regex(set.PaswordStrength, RegexOptions.Compiled);
                _old = set.PaswordStrength;
            }

            return _regex.IsMatch(password);
        }
    }
}