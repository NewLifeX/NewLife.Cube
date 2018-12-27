using System;
using System.Collections.Generic;
using System.Reflection;
using NewLife.Cube.Entity;
using XCode;
using XCode.Membership;

namespace NewLife.Cube.Admin.Controllers
{
    /// <summary>部门</summary>
    public class DepartmentController : EntityController<Department>
    {
        static DepartmentController()
        {
            MenuOrder = 95;
        }
    }
}