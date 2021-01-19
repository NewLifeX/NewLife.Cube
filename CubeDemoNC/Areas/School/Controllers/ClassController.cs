using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using NewLife.Caching;
using NewLife.Cube;
using NewLife.Log;
using NewLife.School.Entity;
using NewLife.Web;
using XCode.Membership;

namespace CubeDemo.Areas.School.Controllers
{
    [SchoolArea]
    [DisplayName("班级")]
    [Route("School/[controller]")]
    public class ClassController : EntityController<Class>
    {
        private readonly ITracer _tracer;

        public ClassController(ITracer tracer) => _tracer = tracer;

        public override ActionResult Index(Pager p = null) => base.Index(p);

        protected override IEnumerable<Class> Search(Pager p)
        {
            using var span = _tracer?.NewSpan(nameof(Search), p);

            var id = p["Id"].ToInt(-1);
            if (id > 0)
            {
                var entity = Class.FindByID(id);
                return entity == null ? new List<Class>() : new List<Class> { entity };
            }

            var start = p["dtStart"].ToDateTime();
            var end = p["dtEnd"].ToDateTime();

            return Class.Search(start, end, p["Q"], p);
        }

        protected override IDictionary<MethodInfo, Int32> ScanActionMenu(IMenu menu)
        {
            menu.Visible = true;
            return base.ScanActionMenu(menu);
        }
    }
}