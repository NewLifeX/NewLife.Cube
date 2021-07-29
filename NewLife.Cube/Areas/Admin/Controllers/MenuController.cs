using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.ViewModels;
using XCode.Membership;

namespace NewLife.Cube.Admin.Controllers
{
    /// <summary>菜单控制器</summary>
    [DisplayName("菜单")]
    [Description("系统操作菜单以及功能目录树。支持排序，不可见菜单仅用于功能权限限制。每个菜单的权限子项由系统自动生成，请不要人为修改")]
    [Area("Admin")]
    public class MenuController : EntityTreeController<Menu>
    {
        static MenuController()
        {
            MenuOrder = 80;

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
            if (post) Menu.Meta.Session.ClearCache($"{type}-{entity}", true);

            return rs;
        }
        /// <summary>
        /// 获取所有菜单节点列表
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [EntityAuthorize(PermissionFlags.Detail)]
        public ActionResult GetMenuAll()
        {
            var list = Menu.Meta.Factory.FindAll(null, "Sort", "", 0, 0);
            return Json(0, null, list);
        }
        /// <summary>
        /// 获取所有select 树形菜单
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [EntityAuthorize(PermissionFlags.Detail)]
        public IActionResult GetSelectTree(int id)
        {
            var menuList = Menu.FindAll();
            var treeList = (from menu in menuList
                            where menu.DisplayName != "主页" && menu.Visible == true
                            select new SelectTree
                            {
                                name = menu.DisplayName,
                                disabled = menu.ID == id ? true : false,
                                value = menu.ID.ToString(),
                                parentID = menu.ParentID.ToString()
                            }).ToList();

            var sList = new List<SelectTree>();
            var node = new SelectTree();
            sList = node.GetSelectTreeList(treeList, sList, "0");
            return Json(new
            {
                data = sList
            });
        }
    }
}