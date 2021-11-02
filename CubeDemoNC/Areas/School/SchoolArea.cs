using System;
using System.ComponentModel;
using NewLife;
using NewLife.Cube;

namespace CubeDemo.Areas.School
{
    [DisplayName("教务系统")]
    public class SchoolArea : AreaBase
    {
        public SchoolArea() : base(nameof(SchoolArea).TrimEnd("Area")) { }

        /// <summary>
        /// 菜单顺序
        /// </summary>
        public static Int32 MenuOrder { get; set; } = 123;

        //static SchoolArea() => RegisterArea<SchoolArea>();
    }
}