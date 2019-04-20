using System;
using System.Collections.Generic;
using NewLife.Cube;
using NewLife.School.Entity;
using NewLife.Web;
using XCode.Membership;

namespace CubeDemo.Areas.School.Controllers
{
    public class StudentController : EntityController<Student>
    {
        protected override IEnumerable<Student> Search(Pager p)
        {
            var classid = Request["ClassID"].ToInt();

            var list = Student.Search(SexKinds.女, "992班", p);

            return Student.Search(classid, p["q"], p);
        }
    }
}