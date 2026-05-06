using XCode.DataAccessLayer;

namespace NewLife.Cube.Areas.Admin.Models;

/// <summary>数据库模型差异结果</summary>
public class DbDiffModel
{
    /// <summary>连接名</summary>
    public String Name { get; set; }

    /// <summary>差异表集合</summary>
    public IList<DbTableDiffItem> Tables { get; set; }
}

/// <summary>单张表的模型差异</summary>
public class DbTableDiffItem
{
    /// <summary>实体类名（无对应实体时等于数据库表名）</summary>
    public String Name { get; set; }

    /// <summary>数据库表名</summary>
    public String TableName { get; set; }

    /// <summary>显示名</summary>
    public String DisplayName { get; set; }

    /// <summary>是否存在对应实体模型。false 表示该表在数据库中存在但没有对应实体类</summary>
    public Boolean HasEntityModel { get; set; }

    /// <summary>数据库多出的字段列表</summary>
    public IList<IDataColumn> ExtraColumns { get; set; }

    /// <summary>可直接粘贴到 Model.xml 的 XML 字段片段</summary>
    public String XmlFragment { get; set; }
}
