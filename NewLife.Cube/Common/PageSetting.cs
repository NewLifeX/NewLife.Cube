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

        /// <summary>启用工具栏。默认true</summary>
        public Boolean EnableToolbar { get; set; } = true;

        /// <summary>启用添加按钮。默认true</summary>
        public Boolean EnableAdd { get; set; } = true;

        /// <summary>启用关键字搜索。默认true</summary>
        public Boolean EnableKey { get; set; } = true;

        /// <summary>启用选择列。默认Update/Delete权限时为true</summary>
        public Boolean EnableSelect { get; set; }

        /// <summary>启用尾部。默认true</summary>
        public Boolean EnableFooter { get; set; } = true;

        /// <summary>是否只读页面。只读页面没有添加编辑删除等按钮</summary>
        public Boolean IsReadOnly { get; set; }

        /// <summary>启用列表双击事件配置</summary>
        public Boolean EnableTableDoubleClick { get; set; } = true;
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
                EnableNavbar = EnableNavbar,
                EnableToolbar = EnableToolbar,
                EnableAdd = EnableAdd,
                EnableKey = EnableKey,
                EnableSelect = EnableSelect,
                EnableTableDoubleClick = EnableTableDoubleClick
            };

            return set;
        }
        #endregion
    }
}