using System;
using System.Runtime.Serialization;

namespace NewLife.Cube.Web.Models
{
    /// <summary>Sso令牌模型</summary>
    public class SsoTokenModel
    {
        /// <summary>应用标识</summary>
        public String client_id { get; set; }

        /// <summary>应用密钥</summary>
        public String client_secret { get; set; }

        /// <summary>用户名。可以是设备编码等唯一使用者标识</summary>
        public String UserName { get; set; }

        /// <summary>密码</summary>
        public String Password { get; set; }

        /// <summary>授权类型</summary>
        public String grant_type { get; set; }
    }
}