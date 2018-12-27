using System;
using System.Collections.Generic;
using System.Reflection;
using NewLife.Cube.Entity;
using XCode;
using XCode.Membership;

namespace NewLife.Cube.Admin.Controllers
{
    /// <summary>字典参数</summary>
    public class ParameterController : EntityController<Parameter>
    {
        static ParameterController()
        {
            MenuOrder = 65;
        }
    }
}