using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.Entity;
using XCode;
using XCode.Membership;

namespace NewLife.Cube.Admin.Controllers
{
    /// <summary>委托代理</summary>
    [Area("Admin")]
    public class PrincipalAgentController : EntityController<PrincipalAgent>
    {
        /// <summary>
        /// 添加页面初始化数据
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="type"></param>
        /// <param name="post"></param>
        /// <returns></returns>
        protected override Boolean Valid(PrincipalAgent entity, DataObjectMethodType type, Boolean post)
        {
            if (!post && type == DataObjectMethodType.Insert)
            {
                entity.Enable = true;
                entity.Times = 1;
                entity.Expire = DateTime.Now.AddMinutes(20);
            }

            return base.Valid(entity, type, post);
        }

        /// <summary>菜单不可见</summary>
        /// <param name="menu"></param>
        /// <returns></returns>
        protected override IDictionary<MethodInfo, Int32> ScanActionMenu(IMenu menu)
        {
            if (menu.Visible && !menu.Necessary)
            {
                menu.Visible = false;
                (menu as IEntity).Update();
            }

            return base.ScanActionMenu(menu);
        }
    }
}