using XCode;
using XCode.DataAccessLayer;

namespace NewLife.Cube.Areas.Admin.Models;

public class DbEntitiesModel
{
    public String Name { get; set; }

    public IList<IEntityFactory> Entities { get; set; }
}
