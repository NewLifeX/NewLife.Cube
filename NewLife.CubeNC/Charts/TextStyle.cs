using System;

namespace NewLife.Cube.Charts
{
    /// <summary>文字样式</summary>
    public class TextStyle
    {
        /// <summary>
        /// 颜色
        /// </summary>
        public String Color { get; set; }

        /// <summary>
        /// 默认值：'none' 
        /// 修饰，仅对tooltip.textStyle生效 
        /// </summary>
        public String Decoration { get; set; }

        /// <summary>
        /// 默认值：各异 
        /// 水平对齐方式，可选为：'left' | 'right' | 'center' 
        /// </summary>
        public String Align { get; set; }

        /// <summary>
        /// 默认值：各异
        /// 垂直对齐方式，可选为：'top' | 'bottom' | 'middle' 
        /// </summary>
        public String Baseline { get; set; }

        /// <summary>
        /// 默认值：'Arial, Verdana, sans-serif'
        /// 字体系列  
        /// </summary>
        public String FontFamily { get; set; }

        /// <summary>
        /// 默认值：12
        /// 字号，单位px  
        /// </summary>
        public Int32 FontSize { get; set; }

        /// <summary>
        /// 字体系列  
        /// </summary>
        public String FontStyle { get; set; }

        /// <summary>
        /// 粗细，可选为：'normal' | 'bold' | 'bolder' | 'lighter' | 100 | 200 |... | 900  
        /// </summary>
        public String FontWeight { get; set; }
    }
}