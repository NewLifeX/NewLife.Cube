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
    /// <summary>导入模式</summary>
    /// <remarks>
    /// 默认 <see cref="ImportMode.Auto"/>，按当前逻辑自动处理（合并/插入/Upsert）。
    /// 也可指定仅插入/忽略冲突/覆盖插入/Upsert/合并。
    /// </remarks>
    public ImportMode Mode { get; set; } = ImportMode.Auto;

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

/// <summary>导入模式</summary>
public enum ImportMode
{
    /// <summary>自动。保持现有策略：空表插入；小表按主键合并；否则 Upsert</summary>
    Auto = 0,

    /// <summary>仅插入。高性能批量插入，冲突抛出异常</summary>
    Insert = 1,

    /// <summary>插入忽略。批量插入，遇到主键或唯一冲突时忽略该行</summary>
    InsertIgnore = 2,

    /// <summary>覆盖插入。批量替换，遇到冲突时覆盖</summary>
    Replace = 3,

    /// <summary>更新插入。Upsert，冲突时执行更新</summary>
    Upsert = 4,

    /// <summary>合并。按主键（或唯一键）查询并仅更新部分字段；大表退化为 Upsert</summary>
    Merge = 5,
}
