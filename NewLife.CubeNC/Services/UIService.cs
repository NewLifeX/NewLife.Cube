using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NewLife.Cube.Services
{
    /// <summary>UI服务</summary>
    public class UIService
    {
        #region 属性
        /// <summary>主题集合</summary>
        public IList<String> Themes { get; } = new List<String>();

        /// <summary>皮肤集合</summary>
        public IList<String> Skins { get; } = new List<String>();
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

        /// <summary>
        /// 获取EChart图表主题
        /// </summary>
        /// <returns></returns>
        public IList<String> GetEChartsThemes()
        {
            var asm = Assembly.GetExecutingAssembly();
            var names = asm.GetManifestResourceNames();
            names = names.Where(e => e.Contains(".echarts.theme.")).ToArray();

            return names.Select(e => e.Substring(".echarts.theme.", null)?.TrimEnd(".js")).Where(e => !e.IsNullOrEmpty()).ToList();
        }
    }
}