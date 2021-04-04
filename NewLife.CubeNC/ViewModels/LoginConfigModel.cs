using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NewLife.Common;
using NewLife.Cube.Entity;
using NewLife.Reflection;

namespace NewLife.Cube.ViewModels
{
    public class LoginConfigModel
    {
        private readonly Setting _set = Setting.Current;
        private readonly SysConfig _cubeSet = SysConfig.Current;

        public String DisplayName => _cubeSet.DisplayName;
        public String Logo => String.Empty;
        public Boolean AllowLogin => _set.AllowLogin;
        public Boolean AllowRegister => _set.AllowRegister;
        //public Boolean AutoRegister => _set.AutoRegister;

        public List<OAuthConfigModel> Providers =>
            OAuthConfig.FindAllWithCache().Where(w=>w.Enable).Select(s =>
            {
                var m = new OAuthConfigModel();
                m.Copy(s);
                return m;
            }).ToList();
    }

    public class OAuthConfigModel
    {
        /// <summary>
        /// 应用名
        /// </summary>
        public String Name { get; set; }

        /// <summary>
        /// 图标
        /// </summary>
        public String Logo { get; set; }

        /// <summary>
        /// 显示名
        /// </summary>
        public String NickName { get; set; }
    }
}
