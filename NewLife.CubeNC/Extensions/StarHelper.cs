using NewLife.Cube.ViewModels;
using NewLife.Data;
using XCode;

namespace NewLife.Cube.Extensions
{
    /// <summary>星尘助手</summary>
    public static class StarHelper
    {
        /// <summary>
        /// 生成星尘调用链Url
        /// </summary>
        /// <param name="traceId"></param>
        /// <returns></returns>
        public static String BuildUrl(String traceId)
        {
            if (traceId.IsNullOrEmpty()) return null;

            var web = Setting.Current.StarWeb;
            if (web.IsNullOrEmpty()) return null;

            if (web.Contains("{traceId}")) return web.Replace("{traceId}", traceId);

            return $"{web}/trace?id={traceId}";
        }

        /// <summary>设置星尘监控链接</summary>
        /// <param name="fields"></param>
        /// <param name="fieldName"></param>
        /// <param name="display"></param>
        public static ListField TraceUrl(this FieldCollection fields, String fieldName, String display) => TraceUrl(fields, fieldName, display, false);

        /// <summary>设置星尘监控链接</summary>
        /// <param name="fields"></param>
        /// <param name="fieldName"></param>
        /// <param name="display"></param>
        /// <param name="newWindow"></param>
        public static ListField TraceUrl(this FieldCollection fields, String fieldName = "TraceId", String display = "追踪", Boolean newWindow = false)
        {
            if (fieldName.IsNullOrEmpty()) fieldName = "TraceId";
            if (fields.GetField(fieldName) is not ListField df) return null;

            if (display.IsNullOrEmpty()) display = "追踪";
            df.Text = display;

            //df.Url = BuildUrl("{" + fieldName + "}");
            if (newWindow) df.Target = "_blank";

            df.Title = "链路追踪，用于APM性能追踪定位，还原该事件的调用链";
            df.DataVisible = e => !((e as IEntity)[df.Name] as String).IsNullOrEmpty();
            df.AddService(new StarUrlExtend());

            return df;
        }

        private class StarUrlExtend : IUrlExtend
        {
            /// <summary>解析Url地址</summary>
            /// <param name="field"></param>
            /// <param name="data"></param>
            /// <returns></returns>
            public String Resolve(DataField field, IExtend data) => BuildUrl(data[field.Name] as String);
        }
    }
}