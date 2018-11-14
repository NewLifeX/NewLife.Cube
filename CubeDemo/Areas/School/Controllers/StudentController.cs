using System;
using System.Collections.Generic;
using NewLife.Cube;
using NewLife.School.Entity;
using NewLife.Web;

namespace CubeDemo.Areas.School.Controllers
{
    public class StudentController : EntityController<Student>
    {
        protected override IEnumerable<Student> Search(Pager p)
        {
            var classid = Request["ClassID"].ToInt();

            return Student.Search(classid, p["q"], p);
        }
    }
}