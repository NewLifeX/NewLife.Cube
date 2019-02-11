using System;
using System.ComponentModel;
using NewLife.Cube;

namespace WebTest.Areas.School
{
    [DisplayName("教务系统")]
    public class SchoolArea : AreaBase
    {
        public SchoolArea() : base(nameof(SchoolArea).TrimEnd("Area")) { }

        static SchoolArea() => RegisterArea<SchoolArea>();
    }
}