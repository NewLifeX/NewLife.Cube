using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using NewLife.Web;
using XCode;
using XCode.Membership;
using XCode.Statistics;

namespace NewLife.Cube.Admin.Controllers
{
    /// <summary>访问统计控制器</summary>
    [DisplayName("访问统计")]
    [Description("每个页面每天的访问统计信息")]
    [Area("Admin")]
    public class VisitStatController : ReadOnlyEntityController<VisitStat>
    {
        /// <summary>搜索数据集</summary>
        /// <param name="p"></param>
        /// <returns></returns>
        protected override IEnumerable<VisitStat> Search(Pager p)
        {
            var model = new VisitStatModel();
            model.Fill(p.Params, StatLevels.Day);
            model.Page = p["p"];

            p.RetrieveState = true;

            return VisitStat.Search(model, p["dtStart"].ToDateTime(), p["dtEnd"].ToDateTime(), p);
        }

        /// <summary>菜单不可见</summary>
        /// <param name="menu"></param>
        /// <returns></returns>
        protected override IDictionary<MethodInfo, Int32> ScanActionMenu(IMenu menu)
        {
            if (menu.Visible)
            {
                menu.Visible = false;
                (menu as IEntity).Save();
            }

            return base.ScanActionMenu(menu);
        }
    }
}