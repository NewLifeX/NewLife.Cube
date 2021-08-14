using System;
using System.Collections;

namespace NewLife.Cube.ViewModels
{
    /// <summary>下拉列表模型</summary>
    public class ListBoxModel
    {
        #region 属性
        /// <summary>名称</summary>
        public String Name { get; set; }

        /// <summary>数据</summary>
        public IEnumerable Value { get; set; }

        /// <summary>已选值</summary>
        public Object SelectedValues { get; set; }

        /// <summary>标签</summary>
        public String OptionLabel { get; set; }

        /// <summary>自动提交</summary>
        public Boolean AutoPostback { get; set; }

        /// <summary>Html特性</summary>
        public Object HtmlAttributes { get; set; }
        #endregion

        #region 构造
        /// <summary>实例化</summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="selectedValues"></param>
        public ListBoxModel(String name, IEnumerable value, Object selectedValues)
        {
            Name = name;
            Value = value;
            SelectedValues = selectedValues;
        }

        /// <summary>实例化</summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="selectedValues"></param>
        /// <param name="optionLabel"></param>
        /// <param name="autoPostback"></param>
        public ListBoxModel(String name, IEnumerable value, Object selectedValues, String optionLabel, Boolean autoPostback, Object htmlAttributes)
        {
            Name = name;
            Value = value;
            SelectedValues = selectedValues;
            OptionLabel = optionLabel;
            AutoPostback = autoPostback;
            HtmlAttributes = htmlAttributes;
        }
        #endregion
    }
}