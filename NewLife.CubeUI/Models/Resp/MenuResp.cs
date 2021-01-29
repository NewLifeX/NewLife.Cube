using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NewLife.CubeUI.Models.Resp
{
    /// <summary>
    /// 菜单树
    /// </summary>
    public class MenuResp
    {
        /// <summary>
        /// 
        /// </summary>
        /// <value></value>
        public Int32 ID { get; set; }

        /// <summary>
        /// 名称
        /// </summary>
        /// <value></value>
        public String Name { get; set; }
        /// <summary>
        /// 链接
        /// </summary>
        /// <value></value>
        public String Url { get; set; }
        /// <summary>
        /// 图标
        /// </summary>
        /// <value></value>
        public String Icon { get; set; }
        /// <summary>
        /// 自定义样式类
        /// </summary>
        /// <value></value>
        public String Class { get; set; }
        /// <summary>
        /// 子菜单
        /// </summary>
        /// <value></value>
        public List<MenuResp> Children { get; set; }
    }
}
