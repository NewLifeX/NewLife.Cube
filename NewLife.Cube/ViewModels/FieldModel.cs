using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NewLife.Cube.ViewModels
{
    /// <summary>
    /// 字段模型
    /// </summary>
    public class FieldModel
    {
        private String _name;
        private String _columnName;

        /// <summary>
        /// 小写格式，默认false
        /// </summary>
        public Boolean LowerCase = false;

        /// <summary>
        /// 小驼峰格式，默认true
        /// </summary>
        public Boolean CamelCase = true;

        public FieldModel() { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="formatType">0-小驼峰，1-小写，2-保持默认</param>
        public FieldModel(Int32 formatType)
        {
            if (formatType == 0)
            {
                LowerCase = false;
                CamelCase = true;
            }
            else if (formatType == 1)
            {
                LowerCase = true;
                CamelCase = false;
            }
            else
            {
                LowerCase = false;
                CamelCase = false;
            }
        }

        /// <summary>备注</summary>
        public String Description { get; set; }

        /// <summary>说明</summary>
        public String DisplayName { get; set; }

        /// <summary>属性名</summary>
        public String Name
        {
            get => FormatName(_name);
            internal set => _name = value;
        }

        /// <summary>是否允许空</summary>
        public Boolean IsNullable { get; internal set; }

        /// <summary>长度</summary>
        public Int32 Length { get; internal set; }

        /// <summary>是否数据绑定列</summary>
        public Boolean IsDataObjectField { get; set; }

        /// <summary>是否动态字段</summary>
        public Boolean IsDynamic { get; set; }

        /// <summary>用于数据绑定的字段名</summary>
        /// <remarks>
        /// 默认使用BindColumn特性中指定的字段名，如果没有指定，则使用属性名。
        /// 字段名可能两边带有方括号等标识符
        /// </remarks>
        public String ColumnName
        {
            get => FormatName(_columnName);
            set => _columnName = value;
        }

        /// <summary>是否只读</summary>
        /// <remarks>set { _ReadOnly = value; } 放出只读属性的设置，比如在编辑页面的时候，有的字段不能修改 如修改用户时  不能修改用户名</remarks>
        public Boolean ReadOnly { get; set; }

        /// <summary>
        /// 字段类型
        /// </summary>
        public String TypeStr { get; set; } = nameof(String);

        #region 用于定制字段

        /// <summary>
        /// 是否定制字段
        /// </summary>
        public Boolean IsCustom { get; set; }

        /// <summary>前缀名称。放在某字段之前</summary>
        public String BeforeName { get; set; }

        /// <summary>后缀名称。放在某字段之后</summary>
        public String AfterName { get; set; }

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
        #endregion

        /// <summary>根据小写和驼峰格式化名称</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private String FormatName(String name)
        {
            if (name.IsNullOrEmpty()) return name;

            if (LowerCase) return name.ToLower();
            if (!CamelCase) return name;
            if (name.EqualIgnoreCase("id")) return "id";
            if (name.Length < 2) return name.ToLower();
            return name.Substring(0, 1).ToLower() + name.Substring(1);
        }
    }
}