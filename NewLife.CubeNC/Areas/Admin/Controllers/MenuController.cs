using System;
using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using XCode.Membership;

namespace NewLife.Cube.Admin.Controllers
{
    /// <summary>菜单控制器</summary>
    [DisplayName("菜单")]
    [Description("系统操作菜单以及功能目录树。支持排序，不可见菜单仅用于功能权限限制。每个菜单的权限子项由系统自动生成，请不要人为修改")]
    [Area("Admin")]
    [Menu(80, true, Icon = "fa-navicon")]
    public class MenuController : EntityTreeController<Menu>
    {
        static MenuController()
        {
            // 过滤要显示的字段
            ListFields.RemoveField("Remark");
        }

        /// <summary>验证实体对象</summary>
        /// <param name="entity"></param>
        /// <param name="type"></param>
        /// <param name="post"></param>
        /// <returns></returns>
        protected override Boolean Valid(Menu entity, DataObjectMethodType type, Boolean post)
        {
            var rs = base.Valid(entity, type, post);

            // 清空缓存
            if (post) XCode.Membership.Menu.Meta.Session.ClearCache($"{type}-{entity}", true);

            return rs;
        }
    }
}