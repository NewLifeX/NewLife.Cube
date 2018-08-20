using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>区域特性。兼容netcore</summary>
    public class AreaAttribute : Attribute
    {
        /// <summary>实例化区域特性</summary>
        /// <param name="areaName"></param>
        public AreaAttribute(String areaName) { }
    }
}