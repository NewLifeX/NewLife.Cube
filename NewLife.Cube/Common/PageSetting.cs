using System;

namespace NewLife.Cube
{
    /// <summary>页面设置。用于控制各页面功能</summary>
    public class PageSetting
    {
        #region 静态
        /// <summary>全局设置</summary>
        public static PageSetting Global { get; } = new PageSetting();
        #endregion

        #region 属性
        /// <summary>启用导航栏。默认true</summary>
        public Boolean EnableNavbar { get; set; } = true;
        #endregion

        #region 构造
        #endregion

        #region 方法
        /// <summary>克隆</summary>
        /// <returns></returns>
        public PageSetting Clone()
        {
            var set = new PageSetting
            {
                EnableNavbar = EnableNavbar
            };

            return set;
        }
        #endregion
    }
}