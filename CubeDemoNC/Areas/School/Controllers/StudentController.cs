using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using NewLife;
using NewLife.Cube;
using NewLife.Log;
using NewLife.School.Entity;
using NewLife.Web;

namespace CubeDemo.Areas.School.Controllers;

[SchoolArea]
[DisplayName("学生")]
[Menu(0, true, Mode = MenuModes.Admin | MenuModes.Tenant)]
public class StudentController : EntityController<Student, StudentModel>
{
    static StudentController()
    {
        ListFields.RemoveField("CreateUserID");
        ListFields.RemoveField("UpdateUserID");
        //FormFields

        {
            var df = ListFields.AddListField("test", null, "Enable");
            df.DisplayName = "新生命团队";
            df.Url = "https://newlifex.com?studentId={Id}&name={Name}";
            df.Target = "_blank";
        }
    }

    protected override Student Find(Object key)
    {
        return base.Find(key);
    }

    protected override IEnumerable<Student> Search(Pager p)
    {
        //var classid = p["classid"].ToInt();
        //return Student.Search(null,p);

        var tips = p["tips"];
        XTrace.WriteLine("tips: {0}", tips);

        if (Request.Method == "POST")
        {
            var tips2 = Request.Form["tips"];
            XTrace.WriteLine("tips2: {0}", tips2);
        }

        //PageSetting.EnableToolbar = false;

        return base.Search(p);
    }

    public override ActionResult Index(Pager p = null)
    {
        return base.Index(p);
    }

    public ActionResult Call(String teacher)
    {
        if (!teacher.IsNullOrEmpty())
        {
            var ids = SelectKeys.Select(e => e.ToInt()).ToArray();
            foreach (var id in ids)
            {
                var student = Student.FindById(id);
                XTrace.WriteLine("{0} 呼叫 {1}", student.Name, teacher);
            }
        }

        return Json(0, "呼叫完成！");
    }
}