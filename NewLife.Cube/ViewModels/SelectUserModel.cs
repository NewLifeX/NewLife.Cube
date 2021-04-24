using System;

namespace NewLife.Cube.ViewModels
{
    /// <summary>选择用户控件所使用的模型</summary>
    public class SelectUserModel
    {
        /// <summary>控件</summary>
        public String Id { get; set; }

        /// <summary>角色</summary>
        public Int32 RoleId { get; set; }

        /// <summary>部门</summary>
        public Int32 DepartmentId { get; set; }

        /// <summary>用户Id</summary>
        public Int32 UserId { get; set; }
    }
}