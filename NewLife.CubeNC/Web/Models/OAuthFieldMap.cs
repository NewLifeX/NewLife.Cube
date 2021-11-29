using System;

namespace NewLife.Cube.Web.Models
{
    /// <summary>
    /// OAuth配置字段映射
    /// </summary>
    public class OAuthFieldMap
    {
        /// <summary>统一标识。当前应用下唯一</summary>
        public String OpenID { get; set; }

        /// <summary>企业级标识。当前企业所有应用下唯一</summary>
        public String UnionID { get; set; }

        /// <summary>用户ID</summary>
        public String UserID { get; set; }

        /// <summary>用户名</summary>
        public String UserName { get; set; }

        /// <summary>昵称</summary>
        public String NickName { get; set; }

        /// <summary>性别。0未知，1男，2女</summary>
        public String Sex { get; set; }

        /// <summary>用户代码</summary>
        public String UserCode { get; set; }

        /// <summary>部门代码，唯一标识</summary>
        public String DepartmentCode { get; set; }

        /// <summary>部门名称</summary>
        public String DepartmentName { get; set; }

        /// <summary>手机</summary>
        public String Mobile { get; set; }

        /// <summary>邮箱</summary>
        public String Mail { get; set; }

        /// <summary>头像</summary>
        public String Avatar { get; set; }

        /// <summary>明细。用户个人描述，座右铭等</summary>
        public String Detail { get; set; }

        /// <summary>设备标识。</summary>
        public String DeviceId { get; set; }

        /// <summary>地区</summary>
        public String AreaId { get; set; }
    }
}