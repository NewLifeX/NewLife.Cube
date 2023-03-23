using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NewLife.Common;
using NewLife.Cube.Entity;
using NewLife.Reflection;

namespace NewLife.Cube.ViewModels
{
    /// <summary>
    /// 登录配置模型
    /// </summary>
    public class LoginConfigModel
    {
        private readonly CubeSetting _set = CubeSetting.Current;
        private readonly SysConfig _cubeSet = SysConfig.Current;

        /// <summary>
        /// 显示名
        /// </summary>
        public String DisplayName => _cubeSet.DisplayName;

        /// <summary>
        /// Logo图标
        /// </summary>
        public String Logo => String.Empty;

        /// <summary>
        /// 允许登录
        /// </summary>
        public Boolean AllowLogin => _set.AllowLogin;

        /// <summary>
        /// 允许注册
        /// </summary>
        public Boolean AllowRegister => _set.AllowRegister;
        //public Boolean AutoRegister => _set.AutoRegister;

        /// <summary>
        /// 提供者
        /// </summary>
        public List<OAuthConfigModel> Providers =>
            OAuthConfig.FindAllWithCache().Where(w=>w.Enable).Select(s =>
            {
                var m = new OAuthConfigModel();
                m.Copy(s);
                return m;
            }).ToList();
    }

    /// <summary>
    /// OAuth配置模型
    /// </summary>
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
