using System;
using System.Collections.Generic;
using System.Web.Mvc;
using NewLife.Cube;
using NewLife.School.Entity;
using NewLife.Security;
using NewLife.Web;
using XCode;
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

        public ActionResult MakeData()
        {
            // 关闭日志
            Student.Meta.Session.Dal.Db.ShowSQL = false;

            var count = 1_000_000;
            var list = new List<Student>();
            for (var i = 0; i < count; i++)
            {
                var entity = new Student
                {
                    ClassID = Rand.Next(5),
                    Name = Rand.NextString(8),
                    Sex = (SexKinds)Rand.Next(1, 3),
                    Age = Rand.Next(100),
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