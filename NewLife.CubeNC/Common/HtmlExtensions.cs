using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using NewLife.Collections;
using NewLife.Reflection;
using XCode;
using XCode.Configuration;

namespace NewLife.Cube
{
    /// <summary>Html扩展</summary>
    public static class HtmlExtensions
    {
        /// <summary>输出编辑框</summary>
        /// <param name="Html"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="type"></param>
        /// <param name="format"></param>
        /// <param name="htmlAttributes"></param>
        /// <returns></returns>
        public static IHtmlContent ForEditor(this IHtmlHelper Html, String name, Object value, Type type = null, String format = null, Object htmlAttributes = null)
        {
            if (type == null && value != null) type = value.GetType();
            if (type == null) throw new XException("设计错误！ForEditor({0}, null, null)不能值和类型同时为空，否则会导致死循环", name);

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Boolean:
                    return Html.ForBoolean(name, value.ToBoolean(), htmlAttributes);
                case TypeCode.DateTime:
                    return Html.ForDateTime(name, value.ToDateTime(), format, htmlAttributes);
                case TypeCode.Decimal:
                    return Html.ForDecimal(name, Convert.ToDecimal(value), format, htmlAttributes);
                case TypeCode.Single:
                case TypeCode.Double:
                    return Html.ForDouble(name, value.ToDouble(), format, htmlAttributes);
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    if (type.IsEnum)
                        return Html.ForEnum(name, value ?? 0.ChangeType(type));
                    else
                        return Html.ForInt(name, Convert.ToInt64(value), format, htmlAttributes);
                case TypeCode.String:
                    return Html.ForString(name, value + "", 0, htmlAttributes);
                default:
                    return Html.ForObject(name, value);
            }
        }

        /// <summary>输出编辑框</summary>
        /// <param name="Html"></param>
        /// <param name="field"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static IHtmlContent ForEditor(this IHtmlHelper Html, FieldItem field, IEntity entity = null)
        {
            entity ??= Html.ViewData.Model as IEntity;

            // 优先处理映射。因为映射可能是字符串
            {
                var mhs = ForMap(Html, field, entity);
                if (mhs != null) return mhs;
            }

            if (field.ReadOnly)
            {
                var label = $"<label class=\"form-control form-control-sm\">{entity[field.Name]}</label>";
                return Html.Raw(label);
            }

            if (field.Type == typeof(String))
            {
                var dc = field.Field;
                if (dc != null && dc.ItemType.EqualIgnoreCase("file", "image"))
                {
                    var h1 = Html.ForString(field.Name, (String)entity[field.Name], field.Length);
                    var h2 = Html.ForFile(field.Name, entity[field.Name], dc.ItemType);
                    return new HtmlString(h1.GetString() + h2.GetString());
                }

                //if (field.Length <= 0 || field.Length > 300)
                return Html.ForString(field.Name, (String)entity[field.Name], field.Length);
            }

            // 如果是实体树，并且当前是父级字段，则生产下拉
            if (entity is IEntityTree)
            {
                var mhs = ForTreeEditor(Html, field, entity as IEntityTree);
                if (mhs != null) return mhs;
            }

            return Html.ForEditor(field.Name, entity[field.Name], field.Type);
        }

        private static IHtmlContent ForTreeEditor(IHtmlHelper Html, FieldItem field, IEntityTree entity)
        {
            //var fact = EntityFactory.CreateFactory(entity.GetType());
            var set = entity.GetType().GetValue("Setting") as IEntityTreeSetting;
            if (set == null || set.Parent != field.Name) return null;

            var root = entity.GetType().GetValue("Root") as IEntityTree;
            // 找到完整菜单树，但是排除当前节点这个分支
            var list = root.FindAllChildsExcept(entity);
            var data = new SelectList(list, set.Key, "TreeNodeText", entity[field.Name]);
            return Html.DropDownList(field.Name, data, new { @class = "multiselect" });
        }

        private static IHtmlContent ForMap(IHtmlHelper Html, FieldItem field, IEntity entity)
        {
            var map = field.Map;
            if (map == null) return null;

            // 如果没有外部关联，输出数字编辑框和标签
            // 如果映射目标列表项过多，不能使用下拉
            var fact = map.Provider == null ? null : EntityFactory.CreateFactory(map.Provider.EntityType);
            if (map.Provider == null || fact != null && fact.Session.Count > Setting.Current.MaxDropDownList)
            {
                var label = $"&nbsp;<label class=\"\">{entity[field.Name]}</label>";
                if (field.OriField != null) field = field.OriField;
                var mhs = Html.ForEditor(field.Name, entity[field.Name], field.Type);
                return new HtmlString(mhs.GetString() + label);
            }

            // 为该字段创建下拉菜单
            var dic = map?.Provider?.GetDataSource();
            if (dic == null) return null;

            // 表单页的映射下拉，开头增加无效值选项
            if (fact != null && !map.Provider.Key.IsNullOrEmpty())
            {
                if (fact.Table.FindByName(map.Provider.Key) is FieldItem fi && fi.Type.IsInt())
                {
                    var dic2 = new Dictionary<Object, String>();
                    if (!dic.ContainsKey(-1))
                    {
                        dic2[0] = " ";
                        foreach (var item in dic)
                        {
                            dic2[item.Key] = item.Value;
                        }
                        dic = dic2;
                    }
                }
            }

            return Html.ForDropDownList(map.Name, dic, entity[map.Name]);
        }

        /// <summary>输出编辑框</summary>
        /// <param name="Html"></param>
        /// <param name="name"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static IHtmlContent ForEditor(this IHtmlHelper Html, String name, IEntity entity = null)
        {
            entity ??= Html.ViewData.Model as IEntity;

            var fact = EntityFactory.CreateFactory(entity.GetType());
            var field = fact.Table.FindByName(name);

            return Html.ForEditor(field, entity);
        }

        /// <summary>输出复杂对象</summary>
        /// <param name="Html"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static IHtmlContent ForObject(this IHtmlHelper Html, String name, Object value)
        {
            if (value == null || Type.GetTypeCode(value.GetType()) != TypeCode.Object) return Html.ForEditor(name, value);

            var pis = value.GetType().GetProperties(true);
            pis = pis.Where(pi => pi.CanWrite).ToArray();

            var sb = Pool.StringBuilder.Get();
            var txt = Html.Label(name);
            foreach (var pi in pis)
            {
                var pname = $"{name}_{pi.Name}";

                sb.AppendLine("<div class=\"form-group\">");
                sb.AppendLine(Html.Label(pi.Name, pi.GetDisplayName(), new { @class = "control-label col-md-2" }).GetString());
                sb.AppendLine("<div class=\"input-group col-md-8\">");
                sb.AppendLine(Html.ForEditor(pi.Name, value.GetValue(pi), pi.PropertyType).GetString());

                var des = pi.GetDescription();
                if (!des.IsNullOrEmpty())
                {
                    sb.AppendFormat("<span>&nbsp;{0}</span>", des);
                    sb.AppendLine();
                }

                sb.AppendLine("</div>");
                sb.AppendLine("</div>");
            }

            return new HtmlString(sb.Put(true));
        }

        #region 基础属性
        /// <summary>输出字符串</summary>
        /// <param name="Html"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="length"></param>
        /// <param name="htmlAttributes"></param>
        /// <returns></returns>
        public static IHtmlContent ForString(this IHtmlHelper Html, String name, String value, Int32 length = 0, Object htmlAttributes = null)
        {
            var atts = HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes);
            //if (!atts.ContainsKey("class")) atts.Add("class", "col-xs-10 col-sm-5");
            //if (!atts.ContainsKey("class")) atts.Add("class", "col-xs-12 col-sm-8 col-md-6 col-lg-4");
            if (!atts.ContainsKey("class")) atts.Add("class", "form-control form-control-sm");

            // 首先输出图标
            var ico = "";

            IHtmlContent txt;
            if (name.EqualIgnoreCase("Pass", "Password"))
            {
                if (!atts.ContainsKey("autocomplete")) atts.Add("autocomplete", "off");
                txt = Html.Password(name, value, atts);
            }
            else if (name.EqualIgnoreCase("Phone"))
            {
                ico = "<span class=\"input-group-addon\"><i class=\"glyphicon glyphicon-phone-alt\"></i></span>";
                if (!atts.ContainsKey("type")) atts.Add("type", "tel");
                txt = Html.TextBox(name, value, atts);
            }
            else if (name.EqualIgnoreCase("MobilePhone", "CellularPhone"))
            {
                ico = "<span class=\"input-group-addon\"><i class=\"glyphicon glyphicon-phone\"></i></span>";
                if (!atts.ContainsKey("type")) atts.Add("type", "tel");
                txt = Html.TextBox(name, value, atts);
            }
            else if (name.EqualIgnoreCase("email", "mail"))
            {
                ico = "<span class=\"input-group-addon\"><i class=\"glyphicon glyphicon-envelope\"></i></span>";
                if (!atts.ContainsKey("type")) atts.Add("type", "email");
                txt = Html.TextBox(name, value, atts);
            }
            else if (name.EndsWithIgnoreCase("url"))
            {
                ico = "<span class=\"input-group-addon\"><i class=\"glyphicon glyphicon-home\"></i></span>";
                //if (!atts.ContainsKey("type")) atts.Add("type", "url");
                txt = Html.TextBox(name, value, atts);
            }
            else if (length < 0 || length >= 300 || (length >= 200 && name.EqualIgnoreCase("Remark", "Description", "Comment")))
            {
                var row = (Int32)Math.Round(length / 100d);
                if (row < 3) row = 3;
                txt = Html.TextArea(name, value, row, 20, atts);
            }
            else
            {
                txt = Html.TextBox(name, value, atts);
            }
            var icog = "<div class=\"input-group\">{0}</div>";
            var html = !String.IsNullOrWhiteSpace(ico) ? String.Format(icog, ico + txt.GetString()) : txt.GetString();
            return Html.Raw(html);
        }

        /// <summary>输出整数</summary>
        /// <param name="Html"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="format"></param>
        /// <param name="htmlAttributes"></param>
        /// <returns></returns>
        public static IHtmlContent ForInt(this IHtmlHelper Html, String name, Int64 value, String format = null, Object htmlAttributes = null)
        {
            var atts = HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes);
            if (!atts.ContainsKey("class")) atts.Add("class", "form-control form-control-sm");
            if (!atts.ContainsKey("role")) atts.Add("role", "number");

            return Html.TextBox(name, value, format, atts);
        }

        /// <summary>时间日期输出</summary>
        /// <param name="Html"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="format"></param>
        /// <param name="htmlAttributes"></param>
        /// <returns></returns>
        public static IHtmlContent ForDateTime(this IHtmlHelper Html, String name, DateTime value, String format = null, Object htmlAttributes = null)
        {
            var atts = HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes);
            //if (!atts.ContainsKey("type")) atts.Add("type", "date");
            if (!atts.ContainsKey("class")) atts.Add("class", "form-control form-control-sm date form_datetime");
            atts["autocomplete"] = "off";

            var obj = value == DateTime.MinValue ? "" : value.ToFullString();

            // 最小时间不显示
            if (value <= DateTime.MinValue || value.Year <= 1900) obj = "";
            //if (format.IsNullOrWhiteSpace()) format = "yyyy-MM-dd HH:mm:ss";

            // 日期
            if (name.EndsWithIgnoreCase("Date", "Day"))
            {
                if (value > DateTime.MinValue) obj = value.ToString("yyyy-MM-dd");
                //format = "yyyy-mm-dd";
                atts["dateformat"] = "yyyy-MM-dd";
            }

            // 首先输出图标
            var ico = Html.Raw("<span class=\"input-group-addon\"><i class=\"fa fa-calendar\"></i></span>");

            var txt = Html.TextBox(name, obj, format, atts);
            //var txt = BuildInput(InputType.Text, name, obj, atts);

            return Html.Raw(ico.GetString() + txt.GetString());
        }

        /// <summary>输出布尔型</summary>
        /// <param name="Html"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="htmlAttributes"></param>
        /// <returns></returns>
        public static IHtmlContent ForBoolean(this IHtmlHelper Html, String name, Boolean value, Object htmlAttributes = null)
        {
            var atts = HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes);
            if (!atts.ContainsKey("class")) atts.Add("class", "chkSwitch");
            // 因为得不到很好的样式支撑，暂时去掉CheckBox的Boostrap样式
            //if (!atts.ContainsKey("class")) atts.Add("class", "form-control form-control-sm");
            //var html="<div><label><input name=\"{0}\" value=\"{1}\" type=\"checkbox\" class=\"ace\"><span class=\"lbl\"> Latest news and announcements</span></label></div>";
            return Html.CheckBox(name, value, atts);
        }

        /// <summary>输出货币类型</summary>
        /// <param name="Html"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="format"></param>
        /// <param name="htmlAttributes"></param>
        /// <returns></returns>
        public static IHtmlContent ForDecimal(this IHtmlHelper Html, String name, Decimal value, String format = null, Object htmlAttributes = null)
        {
            var atts = HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes);
            if (!atts.ContainsKey("class")) atts.Add("class", "form-control form-control-sm");

            // 首先输出图标
            var ico = Html.Raw("<span class=\"input-group-addon\"><i class=\"glyphicon glyphicon-yen\"></i></span>");
            var txt = Html.TextBox(name, value, format, atts);

            return Html.Raw(ico.GetString() + txt.GetString());
        }

        /// <summary>输出浮点数</summary>
        /// <param name="Html"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="format"></param>
        /// <param name="htmlAttributes"></param>
        /// <returns></returns>
        public static IHtmlContent ForDouble(this IHtmlHelper Html, String name, Double value, String format = null, Object htmlAttributes = null)
        {
            var atts = HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes);
            if (!atts.ContainsKey("class")) atts.Add("class", "form-control form-control-sm");

            var txt = Html.TextBox(name, value, format, atts);

            return txt;
        }
        #endregion

        #region 专有属性
        /// <summary>输出描述</summary>
        /// <param name="Html"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        public static IHtmlContent ForDescription(this IHtmlHelper Html, FieldItem field)
        {
            var des = field.Description.TrimStart(field.DisplayName).TrimStart(",", ".", "，", "。");
            if (des.IsNullOrWhiteSpace()) return Html.Raw(null);

            if (field.Type == typeof(Boolean))
                return Html.Label(field.Name, des);
            else
                return Html.Raw($"<span class=\"middle\">{des}</span>");
        }

        /// <summary>输出描述</summary>
        /// <param name="Html"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static IHtmlContent ForDescription(this IHtmlHelper Html, String name)
        {
            var entity = Html.ViewData.Model as IEntity;

            var fact = EntityFactory.CreateFactory(entity.GetType());
            var field = fact.Table.FindByName(name);

            return Html.ForDescription(field);
        }

        /// <summary>枚举</summary>
        /// <param name="Html"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="label"></param>
        /// <returns></returns>
        public static IHtmlContent ForEnum(this IHtmlHelper Html, String name, Object value, String label = null)
        {
            var dic = EnumHelper.GetDescriptions(value.GetType());
            var data = new SelectList(dic, "Key", "Value", value.ToInt());
            // 由于 Html.DropDownList 获取默认值，会从 ViewData，ViewData.Model，中获取name的值
            // 如果获取到了，则不会再看传入的selectlist的默认值，由于此处是枚举，所以通过 Html.ViewData.Eval(name) 会得到字符串值，所以导致绑定默认值失败
            // 通过 Html.ViewData[name]=(Int32)value，可以让  Html.DropDownList 优先拿到手动设置的值，就不会再从 ViewData.Model 里面找
            // 当然这里会有一个问题，如果外部同样设置ViewData[name]，则就会出现潜在的bug,所以把之前值保存到oldvalue
            var oldvalue = Html.ViewData[name];
            Html.ViewData[name] = value.ToInt();
            var hmstr = Html.DropDownList(name, data, label, new { @class = "multiselect" });

            //还原ViewData现场
            if (oldvalue != null)
                // 如果外部刚好设置这个值，则还原
                Html.ViewData[name] = oldvalue;
            else
                // 输出html后，删除垃圾
                Html.ViewData.Remove(name);
            return hmstr;
        }

        /// <summary>枚举多选</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Html"></param>
        /// <param name="name"></param>
        /// <param name="selectedValues">已选择项</param>
        /// <returns></returns>
        public static IHtmlContent ForEnum<T>(this IHtmlHelper Html, String name, String selectedValues)
        {
            var dic = EnumHelper.GetDescriptions(typeof(T));

            return Html.ForListBox(name, dic, selectedValues);
        }

        /// <summary>文件上传</summary>
        /// <param name="Html"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="itemType"></param>
        /// <returns></returns>
        public static IHtmlContent ForFile(this IHtmlHelper Html, String name, Object value, String itemType)
        {
            var accept = "";
            if (itemType.EqualIgnoreCase("image"))
                accept = "image/*";

            return Html.TextBox(name, value, new { type = "file", accept });
        }
        #endregion

        #region 下拉列表
        /// <summary>字典的下拉列表</summary>
        /// <param name="Html"></param>
        /// <param name="name"></param>
        /// <param name="items"></param>
        /// <param name="selectedValue">已选择项</param>
        /// <param name="optionLabel">默认空项的文本。此参数可以为 null。</param>
        /// <param name="autoPostback">自动回发</param>
        /// <returns></returns>
        public static IHtmlContent ForDropDownList(this IHtmlHelper Html, String name, IEnumerable items, Object selectedValue = null, String optionLabel = null, Boolean autoPostback = false)
        {
            SelectList data;
            if (items is IDictionary)
                data = new SelectList(items, "Key", "Value", selectedValue);
            else
                data = new SelectList(items, selectedValue);

            var atts = new Dictionary<String, Object>();
            if (Setting.Current.BootstrapSelect)
                atts.Add("class", "multiselect");
            else
                atts.Add("class", "form-control form-control-sm");

            // 处理自动回发
            //if (autoPostback) atts.Add("onchange", "$(':submit').click();");
            // 一个页面可能存在多个表单，比如搜索区和分页区
            // Support By Dark Li(858587868) / 老牛(65485989)
            if (autoPostback) atts.Add("onchange", "$(this).parents('form').submit();");

            return Html.DropDownList(name, data, optionLabel, atts);
        }

        /// <summary>实体列表的下拉列表。单选，自动匹配当前模型的选中项</summary>
        /// <param name="Html"></param>
        /// <param name="name"></param>
        /// <param name="list"></param>
        /// <param name="selectedValue">已选择项</param>
        /// <param name="optionLabel"></param>
        /// <param name="autoPostback">自动回发</param>
        /// <returns></returns>
        public static IHtmlContent ForDropDownList<T>(this IHtmlHelper Html, String name, IList<T> list, Object selectedValue = null, String optionLabel = null, Boolean autoPostback = false) where T : IEntity
        {
            var atts = new Dictionary<String, Object>();
            if (Setting.Current.BootstrapSelect)
                atts.Add("class", "multiselect");
            else
                atts.Add("class", "form-control form-control-sm");

            // 处理自动回发
            //if (autoPostback) atts.Add("onchange", "$(':submit').click();");
            if (autoPostback) atts.Add("onchange", "$(this).parents('form').submit();");

            // T有可能是IEntity，为了兼容老版本视图
            //var fact = typeof(T).AsFactory();
            var type = list?.FirstOrDefault()?.GetType() ?? typeof(T);
            var fact = type.AsFactory();
            var uk = fact?.Unique;
            if (uk == null) throw new InvalidDataException($"实体类[{type.FullName}]缺少唯一主键，无法使用下拉！");

            var master = fact.Master;
            var data = new SelectList(list, uk.Name, master?.Name, selectedValue + "");
            return Html.DropDownList(name, data, optionLabel, atts);
        }

        /// <summary>字典的下拉列表。多选</summary>
        /// <param name="Html"></param>
        /// <param name="name"></param>
        /// <param name="dic"></param>
        /// <param name="selectedValues">已选择项</param>
        /// <returns></returns>
        public static IHtmlContent ForListBox(this IHtmlHelper Html, String name, IDictionary dic, String selectedValues)
        {
            var atts = new RouteValueDictionary();
            if (Setting.Current.BootstrapSelect)
                atts.Add("class", "multiselect");
            else
                atts.Add("class", "form-control form-control-sm");

            atts.Add("multiple", "");

            return Html.ListBox(name, new MultiSelectList(dic, "Key", "Value", selectedValues?.Split(",")), atts);
        }

        /// <summary>实体列表的下拉列表。多选</summary>
        /// <param name="Html"></param>
        /// <param name="name"></param>
        /// <param name="list"></param>
        /// <param name="selectedValues">已选择项</param>
        /// <returns></returns>
        public static IHtmlContent ForListBox<TEntity>(this IHtmlHelper Html, String name, IList<TEntity> list, String selectedValues) where TEntity : IEntity
        {
            var atts = new RouteValueDictionary();
            if (Setting.Current.BootstrapSelect)
                atts.Add("class", "multiselect");
            else
                atts.Add("class", "form-control form-control-sm");
            atts.Add("multiple", "");

            return Html.ListBox(name, new MultiSelectList(list.ToDictionary(), "Key", "Value", selectedValues?.Split(",")), atts);
        }
        #endregion

        #region 辅助方法
        /// <summary>获取HTML字符串</summary>
        /// <param name="htmlContent"></param>
        /// <returns></returns>
        public static String GetString(this IHtmlContent htmlContent)
        {
            var writer = new System.IO.StringWriter();
            htmlContent.WriteTo(writer, HtmlEncoder.Default);
            return writer.ToString();
        }

        /// <summary>是否大文本字段</summary>
        /// <param name="field"></param>
        /// <returns></returns>
        public static Boolean IsBigText(this FieldItem field) => field.Length < 0 || field.Length >= 300 || field.Length >= 200 && field.Name.EqualIgnoreCase("Remark", "Description", "Comment");
        #endregion
    }
}