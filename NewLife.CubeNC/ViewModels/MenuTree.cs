using System;
using System.Collections.Generic;

namespace NewLife.CubeNC.ViewModels
{
    /// <summary>
    /// 菜单树
    /// </summary>
    public class MenuTree
    {
        /// <summary>
        /// 
        /// </summary>
        /// <value></value>
        public int ID { get; set; }

        /// <summary>
        /// 名称
        /// </summary>
        /// <value></value>
        public string Name { get; set; }
        /// <summary>
        /// 链接
        /// </summary>
        /// <value></value>
        public string Url { get; set; }
        /// <summary>
        /// 图标
        /// </summary>
        /// <value></value>
        public string Icon { get; set; }
        /// <summary>
        /// 自定义样式类
        /// </summary>
        /// <value></value>
        public string Class { get; set; }
        /// <summary>
        /// 子菜单
        /// </summary>
        /// <value></value>
        public List<MenuTree> Children { get => GetChildren?.Invoke(this) ?? null; set { } }

        /// <summary>
        /// 获取子菜单的方法
        /// </summary>
        private static Func<MenuTree, List<MenuTree>> GetChildren;

        /// <summary>
        /// 获取菜单树
        /// </summary>
        /// <param name="getChildrenSrc">自定义的获取子菜单需要数据的方法</param>
        /// <param name="getMenuList">获取菜单列表的方法</param>
        /// <param name="src">获取菜单列表的初始数据来源</param>
        /// <returns></returns>
        public static List<MenuTree> GetMenuTree<T>(Func<MenuTree, T> getChildrenSrc,
        Func<T, List<MenuTree>> getMenuList, T src) where T : class, new()
        {
            GetChildren = m =>getMenuList?.Invoke(getChildrenSrc(m));

            return getMenuList?.Invoke(src);
        }
    }
}