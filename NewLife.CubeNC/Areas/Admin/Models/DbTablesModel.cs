using XCode.DataAccessLayer;

namespace NewLife.Cube.Areas.Admin.Models;

/// <summary>数据库表模型</summary>
public class DbTablesModel
{
    /// <summary>名称</summary>
    public String Name { get; set; }

    /// <summary>数据表集合</summary>
    public IList<IDataTable> Tables { get; set; }
}
