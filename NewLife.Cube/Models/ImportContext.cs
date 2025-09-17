using System.ComponentModel;
using System.Web.Script.Serialization;
using System.Xml.Serialization;
using NewLife.Data;
using NewLife.Web;
using XCode.Configuration;

namespace NewLife.Cube.Models;

/// <summary>导入上下文</summary>
public class ImportContext : IExtend
{
    #region 属性
    /// <summary>文件名</summary>
    public String Name { get; set; }

    /// <summary>数据流</summary>
    public Stream Stream { get; set; }

    /// <summary>分页请求参数</summary>
    public Pager Page { get; set; }

    /// <summary>表头</summary>
    public String[] Headers { get; set; }

    /// <summary>字段列表</summary>
    public FieldItem[] Fields { get; set; }

    /// <summary>扩展字段</summary>
    [XmlIgnore, ScriptIgnore]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public IDictionary<String, Object> Items { get; set; } = new Dictionary<String, Object>();

    /// <summary>索引器</summary>
    public Object this[String key] { get => Items.TryGetValue(key, out var value) ? value : null; set => Items[key] = value; }
    #endregion
}
