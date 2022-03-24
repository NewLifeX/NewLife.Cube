using System;
using System.Text.RegularExpressions;
using NewLife.Data;
using XCode;
using XCode.Configuration;

namespace NewLife.Cube.ViewModels
{
    /// <summary>列表字段</summary>
    public class ListField : DataField
    {
        #region 属性
        ///// <summary>单元格文字</summary>
        //public String Text { get; set; }

        /// <summary>单元格标题。数据单元格上的提示文字</summary>
        public String Title { get; set; }

        /// <summary>单元格链接。数据单元格的链接</summary>
        public String Url { get; set; }

        /// <summary>头部文字</summary>
        public String Header { get; set; }

        /// <summary>头部标题。数据移上去后显示的文字</summary>
        public String HeaderTitle { get; set; }

        ///// <summary>头部链接。一般是排序</summary>
        //public String HeaderUrl { get; set; }

        /// <summary>数据动作。设为action时走ajax请求</summary>
        public String DataAction { get; set; }

        ///// <summary>是否显示</summary>
        //public DataVisibleDelegate DataVisible { get; set; }
        #endregion

        #region 方法
        /// <summary>填充</summary>
        /// <param name="field"></param>
        public override void Fill(FieldItem field)
        {
            base.Fill(field);

            Header = field.DisplayName;
        }
        #endregion

        #region 数据格式化
        private static readonly Regex _reg = new(@"{(\w+)}", RegexOptions.Compiled);

        /// <summary>针对指定实体对象计算DisplayName，替换其中变量</summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public virtual String GetDisplayName(IExtend data)
        {
            if (DisplayName.IsNullOrEmpty()) return null;

            return _reg.Replace(DisplayName, m => data[m.Groups[1].Value + ""] + "");
        }

        /// <summary>针对指定实体对象计算url，替换其中变量</summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public virtual String GetUrl(IExtend data)
        {
            if (Url.IsNullOrEmpty()) return null;

            var svc = GetService<IUrlExtend>();
            if (svc != null) return svc.Resolve(this, data);

            return _reg.Replace(Url, m => data[m.Groups[1].Value + ""] + "");
        }

        /// <summary>针对指定实体对象计算title，替换其中变量</summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public virtual String GetTitle(IExtend data)
        {
            if (Title.IsNullOrEmpty()) return null;

            return _reg.Replace(Title, m => data[m.Groups[1].Value + ""] + "");
        }
        #endregion
    }
}