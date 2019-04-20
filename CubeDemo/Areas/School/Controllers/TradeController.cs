using System.Collections.Generic;
using System.Web.Mvc;
using NewLife.Cube;
using NewLife.School.Entity;
using NewLife.Security;
using NewLife.Web;
using XCode;

namespace CubeDemo.Areas.School.Controllers
{
    public class TradeController : EntityController<Trade>
    {

        public ActionResult MakeData()
        {
            // 关闭日志
            Trade.Meta.Session.Dal.Db.ShowSQL = false;

            var count = 1_000_000;
            var list = new List<Trade>();
            for (var i = 0; i < count; i++)
            {
                var entity = new Trade
                {
                    Tid = Rand.NextString(8)
                };

                list.Add(entity);

                if ((i + 1) % 5000 == 0)
                {
                    list.Insert();
                    list.Clear();
                }
            }

            return IndexView(new Pager());
        }
    }
}