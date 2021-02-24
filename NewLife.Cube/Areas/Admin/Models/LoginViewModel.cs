using System;
using System.Collections.Generic;
using NewLife.Cube.Entity;

namespace NewLife.Cube.Areas.Admin.Models
{
    /// <summary>登录视图模型</summary>
    public class LoginViewModel
    {
        /// <summary>名称</summary>
        public String Name { get; set; }

        /// <summary>显示名</summary>
        public String DisplayName { get; set; }

        /// <summary>Logo</summary>
        public String Logo { get; set; }

        /// <summary>允许登录</summary>
        public Boolean AllowLogin { get; set; }

        /// <summary>允许注册</summary>
        public Boolean AllowRegister { get; set; }

        /// <summary>自动注册</summary>
        public Boolean AutoRegister { get; set; }

        /// <summary>登录提示</summary>
        public String LoginTip { get; set; }

        /// <summary>资源地址。指向CDN，如 https://sso.newlifex.com/content/，留空表示使用本地</summary>
        public String ResourceUrl { get; set; }

        /// <summary>返回地址</summary>
        public String ReturnUrl { get; set; }

        /// <summary>OAuth系统集合</summary>
        public IList<OAuthConfig> OAuthItems { get; set; }
    }
}