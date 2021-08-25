using System;

namespace NewLife.Cube.ViewModels
{
    /// <summary>界面元素模型</summary>
    public class ItemModel
    {
        #region 属性
        /// <summary>名称</summary>
        public String Name { get; set; }

        /// <summary>数值</summary>
        public Object Value { get; set; }

        /// <summary>类型</summary>
        public Type Type { get; set; }

        /// <summary>字段长度</summary>
        public Int32 Length { get;set;  }

        /// <summary>格式化字符串</summary>
        public String Format { get; set; }

        /// <summary>Html特性</summary>
        public Object HtmlAttributes { get; set; }
        #endregion

        #region 构造
        /// <summary>实例化</summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="type"></param>
        public ItemModel(String name, Object value, Type type)
        {
            Name = name;
            Value = value;
            Type = type;
        }

        /// <summary>实例化</summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="type"></param>
        /// <param name="format"></param>
        /// <param name="htmlAttributes"></param>
        public ItemModel(String name, Object value, Type type, String format, Object htmlAttributes)
        {
            Name = name;
            Value = value;
            Type = type;
            Format = format;
            HtmlAttributes = htmlAttributes;
        }
        #endregion
    }
}