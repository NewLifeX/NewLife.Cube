using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Script.Serialization;
using NewLife.Collections;
using NewLife.Data;

namespace NewLife.Cube.Charts
{
    /// <summary>系列。一组数值以及他们映射成的图</summary>
    public class Series : IExtend3
    {
        #region 属性
        /// <summary>图表类型</summary>
        public String Type { get; set; }
        //public SeriesTypes Type { get; set; }

        /// <summary>名称</summary>
        public String Name { get; set; }

        /// <summary>数据</summary>
        public Object Data { get; set; }

        /// <summary>折线光滑</summary>
        public Boolean Smooth { get; set; }

        ///// <summary>标记点。例如最大最小值</summary>
        //public Object MarkPoint { get; set; }

        ///// <summary>标记线。例如平均线</summary>
        //public Object MarkLine { get; set; }

        /// <summary>扩展字典</summary>
        [ScriptIgnore]
        public IDictionary<String, Object> Items { get; set; } = new NullableDictionary<String, Object>(StringComparer.OrdinalIgnoreCase);

        /// <summary>扩展数据</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public Object this[String key] { get => Items[key]; set => Items[key] = value; }
        #endregion

        #region 方法
        /// <summary>标记最大最小值</summary>
        /// <param name="max"></param>
        /// <param name="min"></param>
        public void MarkPoint(Boolean max, Boolean min)
        {
            var typeNames = new Dictionary<String, String>();

            if (max) typeNames["max"] = "Max";
            if (min) typeNames["min"] = "Min";

            MarkPoint(typeNames);
        }

        /// <summary>标记点。例如最大最小值</summary>
        /// <param name="typeNames"></param>
        public void MarkPoint(IDictionary<String, String> typeNames)
        {
            Items["markPoint"] = new { data = typeNames.Select(e => new { type = e.Key, name = e.Value }).ToArray() };
        }

        /// <summary>标记平均线</summary>
        /// <param name="avg"></param>
        public void MarkLine(Boolean avg)
        {
            var typeNames = new Dictionary<String, String>();

            if (avg) typeNames["average"] = "Avg";

            MarkLine(typeNames);
        }

        /// <summary>标记线</summary>
        /// <param name="typeNames"></param>
        public void MarkLine(IDictionary<String, String> typeNames)
        {
            Items["markLine"] = new { data = typeNames.Select(e => new { type = e.Key, name = e.Value }).ToArray() };
        }
        #endregion
    }
}