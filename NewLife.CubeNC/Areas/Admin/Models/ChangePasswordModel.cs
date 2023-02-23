using System;

namespace NewLife.Cube.Areas.Admin.Models
{
    /// <summary>修改密码模型</summary>
    public class ChangePasswordModel : ICubeModel
    {
        /// <summary>用户名</summary>
        public String Name { get; set; }

        /// <summary>Sso登录渠道</summary>
        public String SsoName { get; set; }

        /// <summary>旧密码</summary>
        public String OldPassword { get; set; }

        /// <summary>新密码</summary>
        public String NewPassword { get; set; }

        /// <summary>确认密码</summary>
        public String NewPassword2 { get; set; }
    }
}