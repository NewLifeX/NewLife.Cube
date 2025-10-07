namespace NewLife.Cube.Areas.Admin.Models
{
    /// <summary>令牌刷新模型</summary>
    public class RefreshTokenModel
    {
        /// <summary>刷新令牌</summary>
        public String RefreshToken { get; set; }

        /// <summary>用户名</summary>
        public String UserName { get; set; }
    }
}
