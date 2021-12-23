using System;
using System.ComponentModel;
using NewLife;
using NewLife.Cube;

namespace CubeDemo.Areas.School
{
    [DisplayName("教务系统")]
    [Menu(123)]
    public class SchoolArea : AreaBase
    {
        public SchoolArea() : base(nameof(SchoolArea).TrimEnd("Area")) { }
    }
}