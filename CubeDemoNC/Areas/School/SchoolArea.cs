using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using NewLife.Cube;

namespace WebTest.Areas.School
{
    [DisplayName("教务系统")]
    public class SchoolArea : AreaBaseX
    {
        public static string AreaName => nameof(SchoolArea).TrimEnd("Area");
        public SchoolArea() : base(AreaName)
        {
        }

        static SchoolArea()
        {
            RegisterArea<SchoolArea>();
        }
    }
}
