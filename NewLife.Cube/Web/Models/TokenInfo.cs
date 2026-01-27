using System.Runtime.Serialization;
using NewLife.Web;

namespace NewLife.Cube.Web.Models
{
    /// <summary>令牌信息</summary>
    [Obsolete("=>NewLife.Web.TokenModel", false)]
    public partial class TokenModel
    {
        /// <summary>访问令牌</summary>
        [DataMember(Name = "access_token")]
        public String AccessToken { get; set; }

        /// <summary>刷新令牌</summary>
        [DataMember(Name = "refresh_token")]
        public String RefreshToken { get; set; }

        /// <summary>有效期。单位秒</summary>
        [DataMember(Name = "expires_in")]
        public Int32 Expire { get; set; }

        /// <summary>作用域</summary>
        [DataMember(Name = "scope")]
        public String Scope { get; set; }
    }
}