﻿using System;
using System.Collections;
using System.Text.RegularExpressions;
using NewLife.Data;
using XCode;
using XCode.Configuration;

namespace NewLife.Cube
{
    /// <summary>获取数据源委托</summary>
    /// <param name="entity"></param>
    /// <param name="field"></param>
    /// <returns></returns>
    public delegate IDictionary DataSourceDelegate(IEntity entity, FieldItem field);

    /// <summary>数据可见委托</summary>
    /// <param name="entity"></param>
    /// <param name="field"></param>
    /// <returns></returns>
    public delegate Boolean DataVisibleDelegate(IEntity entity, FieldItem field);

    /// <summary>数据字段。用于定制数据列</summary>
    public class DataField
    {
        #region 属性
        /// <summary>名称</summary>
        public String Name { get; set; }

        /// <summary>前缀名称。放在某字段之前</summary>
        public String BeforeName { get; set; }

        /// <summary>后缀名称。放在某字段之后</summary>
        public String AfterName { get; set; }

        /// <summary>显示名</summary>
        public String DisplayName { get; set; }

        /// <summary>链接</summary>
        public String Url { get; set; }

        /// <summary>标题。数据单元格上的提示文字</summary>
        public String Title { get; set; }

        /// <summary>头部文字</summary>
        public String Header { get; set; }

        /// <summary>头部链接。一般是排序</summary>
        public String HeaderUrl { get; set; }

        /// <summary>头部标题。数据移上去后显示的文字</summary>
        public String HeaderTitle { get; set; }

        /// <summary>数据动作。设为action时走ajax请求</summary>
        public String DataAction { get; set; }

        /// <summary>多选数据源</summary>
        public DataSourceDelegate DataSource { get; set; }

        /// <summary>是否显示</summary>
        public DataVisibleDelegate DataVisible { get; set; }
        #endregion

        #region 方法
        private static readonly Regex _reg = new Regex(@"{(\w+)}", RegexOptions.Compiled);

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

            return _reg.Replace(Url, m => data[m.Groups[1].Value + ""] + "");
        }
        #endregion
    }
}