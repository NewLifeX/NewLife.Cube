using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube;
using NewLife.School.Entity;
using NewLife.Web;

namespace CubeDemo.Areas.School.Controllers
{
    [SchoolArea]
    [DisplayName("学生")]
    public class StudentController : EntityController<Student>
    {
        static StudentController()
        {
            ListFields.RemoveField("CreateUserID");
            ListFields.RemoveField("UpdateUserID");
            //FormFields
        }

        protected override Student Find(Object key)
        {
            return base.Find(key);
        }

        protected override IEnumerable<Student> Search(Pager p)
        {
            return base.Search(p);
            var classid = p["classid"].ToInt();
            return Student.Search(null,p);
        }

        public override ActionResult Index(Pager p = null)
        {
            return base.Index(p);
        }
    }
}