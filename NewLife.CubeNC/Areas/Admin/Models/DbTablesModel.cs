using XCode;
using XCode.DataAccessLayer;

namespace NewLife.Cube.Areas.Admin.Models;

/// <summary>数据库表模型</summary>
public class DbTablesModel
{
    /// <summary>名称</summary>
    public String Name { get; set; }

    /// <summary>数据表集合</summary>
    public IList<DbTableModel> Tables { get; set; }
}

/// <summary>数据库表模型</summary>
public class DbTableModel
{
    /// <summary>名称</summary>
    public String Name { get; set; }

    /// <summary>数据表</summary>
    public IDataTable Table { get; set; }

    /// <summary>数据行数</summary>
    public Int64 Count { get; set; }
}