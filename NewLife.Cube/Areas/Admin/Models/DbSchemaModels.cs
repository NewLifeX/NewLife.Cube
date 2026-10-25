namespace NewLife.Cube.Areas.Admin.Models;

/// <summary>数据表结构快照（表级 + 列级），供数据库管理页展示表结构/实体结构</summary>
public class DbSchemaTable
{
    /// <summary>名称。表结构取表显示名，实体结构取实体类名</summary>
    public String Name { get; set; }

    /// <summary>数据库表名</summary>
    public String TableName { get; set; }

    /// <summary>显示名</summary>
    public String DisplayName { get; set; }

    /// <summary>数据行数</summary>
    public Int64 Count { get; set; }

    /// <summary>列集合</summary>
    public IList<DbSchemaColumn> Columns { get; set; } = [];
}

/// <summary>数据列结构快照</summary>
public class DbSchemaColumn
{
    /// <summary>名称。表结构取列名，实体结构取属性名</summary>
    public String Name { get; set; }

    /// <summary>数据库列名</summary>
    public String ColumnName { get; set; }

    /// <summary>显示名</summary>
    public String DisplayName { get; set; }

    /// <summary>数据类型</summary>
    public String Type { get; set; }

    /// <summary>长度</summary>
    public Int32 Length { get; set; }

    /// <summary>精度</summary>
    public Int32 Precision { get; set; }

    /// <summary>小数位数</summary>
    public Int32 Scale { get; set; }

    /// <summary>主键</summary>
    public Boolean PrimaryKey { get; set; }

    /// <summary>自增</summary>
    public Boolean Identity { get; set; }

    /// <summary>允许空</summary>
    public Boolean Nullable { get; set; }

    /// <summary>唯一索引</summary>
    public Boolean Unique { get; set; }

    /// <summary>说明</summary>
    public String Description { get; set; }
}

/// <summary>模型差异条目：数据库表相对实体模型多出来的字段</summary>
public class DbDiffTable
{
    /// <summary>名称。有实体模型时取实体类名，否则取数据库表名</summary>
    public String Name { get; set; }

    /// <summary>数据库表名</summary>
    public String TableName { get; set; }

    /// <summary>显示名</summary>
    public String DisplayName { get; set; }

    /// <summary>是否存在对应实体模型</summary>
    public Boolean HasEntityModel { get; set; }

    /// <summary>多出来的列名集合</summary>
    public String[] ExtraColumns { get; set; } = [];

    /// <summary>可直接粘贴进 Model.xml 的 Column 片段</summary>
    public String XmlFragment { get; set; }
}
