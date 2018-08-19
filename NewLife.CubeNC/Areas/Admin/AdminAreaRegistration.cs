using System.ComponentModel;
using NewLife.Log;
using XCode.Membership;
using System;

namespace NewLife.Cube.Admin
{
    /// <summary>权限管理区域注册</summary>
    [DisplayName("系统管理")]
    public class AdminArea : AreaBaseX
    {
        public static String AreaName => nameof(AdminArea).TrimEnd("Area");

        /// <inheritdoc />
        public AdminArea() : base(AreaName)
        {
           
        }

        static AdminArea()
        {
            // 自动检查并添加菜单
            XTrace.WriteLine("初始化权限管理体系");
            //var user = ManageProvider.User;
            ManageProvider.Provider.GetService<IUser>();

            RegisterArea<AdminArea>();
        }
    }
}