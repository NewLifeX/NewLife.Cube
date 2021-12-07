using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.Entity;
using NewLife.Web;
using XCode.Membership;
#if __CORE__
#else
using System.Web;
using System.Web.Mvc;
#endif

namespace NewLife.Cube.Admin.Controllers
{
    /// <summary>用户在线控制器</summary>
    [DataPermission(null, "UserID={#userId}")]
    [Area("Admin")]
    [Menu(0, false)]
    public class UserOnlineController : EntityController<UserOnline>
    {
        /// <summary>
        /// 实例化
        /// </summary>
        public UserOnlineController() => PageSetting.EnableAdd = false;

        /// <summary>不允许添加修改日志</summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        [DisplayName()]
        public override ActionResult Add(UserOnline entity) =>
            //return base.Save(entity);
            throw new Exception("不允许添加/修改记录");

        /// <summary>搜索数据集</summary>
        /// <param name="p"></param>
        /// <returns></returns>
        protected override IEnumerable<UserOnline> Search(Pager p)
        {
            var userid = p["UserID"].ToInt(-1);
            var start = p["dtStart"].ToDateTime();
            var end = p["dtEnd"].ToDateTime();

            // 强制当前用户
            if (userid < 0)
            {
                var user = ManageProvider.User;
                if (!user.Roles.Any(e => e.IsSystem)) userid = user.ID;
            }

            return UserOnline.Search(userid, null, start, end, p["Q"], p);
        }
    }
}