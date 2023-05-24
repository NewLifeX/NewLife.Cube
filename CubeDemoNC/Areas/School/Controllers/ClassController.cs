using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube;
using NewLife.Cube.ViewModels;
using NewLife.Log;
using NewLife.School.Entity;
using NewLife.Web;

namespace CubeDemo.Areas.School.Controllers
{
    [SchoolArea]
    [DisplayName("班级")]
    public class ClassController : EntityController<Class, ClassModel>
    {
        private readonly ITracer _tracer;

        static ClassController()
        {
            {
                var df = ListFields.AddListField("test", null, "ID");
                df.DisplayName = "AJAX操作请求";
                df.Url = "/School/Class/Hello";
                df.DataAction = "action";
            }

            {
                var df = ListFields.AddListField("test1", null, "ID");
                df.DisplayName = "当前页打开";
                df.Url = "/School/Class";
                df.Target = TargetEnum._self + "";
            }

            {
                var df = ListFields.AddListField("test1", null, "ID");
                df.DisplayName = "浏览器多标签页打开";
                df.Url = "/School/Student";
                df.Target = TargetEnum._blank + "";
            }
        }

        public ClassController(IServiceProvider provider)
        {
            PageSetting.EnableTableDoubleClick = true;

            _tracer = provider?.GetService<ITracer>();
        }

        public IActionResult Hello()
        {
            return Json(new
            {
                code = "200",
                data = "ok"
            });
        }

        protected override IEnumerable<Class> Search(Pager p)
        {
            using var span = _tracer?.NewSpan(nameof(Search), p);

            var id = p["Id"].ToInt(-1);
            if (id > 0)
            {
                var entity = Class.FindById(id);
                return entity == null ? new List<Class>() : new List<Class> { entity };
            }

            var start = p["dtStart"].ToDateTime();
            var end = p["dtEnd"].ToDateTime();

            return Class.Search(start, end, p["Q"], p);
        }
    }
}