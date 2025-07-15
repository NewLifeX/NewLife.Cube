using XCode;
using XCode.DataAccessLayer;

namespace NewLife.Cube.Areas.Admin.Models;

/// <summary>数据库实体模型</summary>
public class DbEntitiesModel
{
    /// <summary>名称</summary>
    public String Name { get; set; }

    /// <summary>实体集合</summary>
    public IList<DbEntityModel> Entities { get; set; }
}

/// <summary>数据库实体模型</summary>
public class DbEntityModel
{
    /// <summary>名称</summary>
    public String Name { get; set; }

    /// <summary>实体工厂</summary>
    public IEntityFactory Factory { get; set; }

    /// <summary>数据表</summary>
    public IDataTable Table { get; set; }

    /// <summary>数据行数</summary>
    public Int64 Count { get; set; }
}