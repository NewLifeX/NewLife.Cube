using System;

namespace NewLife.Cube.Web.Models
{
    /// <summary>部门信息</summary>
    public class DepartmentInfo
    {
        /// <summary>编号</summary>
        public Int32 Id { get; set; }

        /// <summary>名称</summary>
        public String Name { get; set; }

        /// <summary>英文名</summary>
        public String EnglishName { get; set; }

        /// <summary>父级部门。根部门1</summary>
        public Int32 ParentId { get; set; }

        /// <summary>顺序</summary>
        public Int32 Order { get; set; }

        /// <summary>已重载。显示名称</summary>
        /// <returns></returns>
        public override String ToString() => Name;
    }
}