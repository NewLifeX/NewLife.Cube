using System;

namespace NewLife.Cube.Common
{
    /// <summary>菜单特性</summary>
    public class MenuAttribute : Attribute
    {
        #region 属性
        /// <summary>
        /// 顺序
        /// </summary>
        public Int32 Order { get; set; }

        /// <summary>
        /// 可见
        /// </summary>
        public Boolean Visible { get; set; } = true;
        #endregion

        /// <summary>
        /// 设置菜单特性
        /// </summary>
        /// <param name="order"></param>
        /// <param name="visible"></param>
        public MenuAttribute(Int32 order, Boolean visible = true)
        {
            Order = order;
            Visible = visible;
        }
    }
}