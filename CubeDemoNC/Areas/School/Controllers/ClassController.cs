using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube;
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
                df.DisplayName = "测试多标签";
                df.Url = "/School/Class";
                df.Target = "_frame";
            }
        }

        public ClassController(IServiceProvider provider)
        {
            PageSetting.EnableTableDoubleClick = true;

            _tracer = provider?.GetService<ITracer>();
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