using System;
using System.Collections.Generic;

namespace NewLife.Cube.Services
{
    /// <summary>UI服务</summary>
    public class UIService
    {
        #region 属性
        /// <summary>主题集合</summary>
        public IList<String> Themes { get; } = new List<String> { "Ace", "layui" };

        /// <summary>皮肤集合</summary>
        public IList<String> Skins { get; } = new List<String> { "Ace", "layui" };
        #endregion

        /// <summary>添加主题</summary>
        /// <param name="theme"></param>
        public void AddTheme(String theme)
        {
            if (!Themes.Contains(theme)) Themes.Add(theme);
        }

        /// <summary>添加皮肤</summary>
        /// <param name="skin"></param>
        public void AddSkin(String skin)
        {
            if (!Skins.Contains(skin)) Skins.Add(skin);
        }
    }
}