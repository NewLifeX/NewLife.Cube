using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NewLife.CubeUI.Models.Entity
{
    /// <summary>数据实体抽象，用于页面模板</summary>
    public class EntityBase
    {
        /// <summary>获取/设置 字段值。</summary>
        /// <param name="name">字段名</param>
        /// <returns></returns>
        public virtual Object this[String name]
        {
            get
            {
                //Console.WriteLine($"Get Value:{name}");
                return name;
            }
            set { }
        }
    }
}
