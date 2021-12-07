using System;

namespace NewLife.Cube
{
    /// <summary>菜单特性</summary>
    public class MenuAttribute : Attribute
    {
        #region 属性
        /// <summary>
        /// 顺序。较大者在前面
        /// </summary>
        public Int32 Order { get; set; }

        /// <summary>
        /// 可见
        /// </summary>
        public Boolean Visible { get; set; } = true;

        /// <summary>
        /// 图标
        /// </summary>
        public String Icon { get; set; }
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