using XCode;
using XCode.DataAccessLayer;

namespace NewLife.Cube.Areas.Admin.Models;

public class DbEntitiesModel
{
    public String Name { get; set; }

    public IList<DbEntityModel> Entities { get; set; }
}

public class DbEntityModel
{
    public String Name { get; set; }

    public IEntityFactory Factory { get; set; }

    public IDataTable Table { get; set; }

    public Int64 Count { get; set; }
}