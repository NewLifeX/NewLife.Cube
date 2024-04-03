using XCode.DataAccessLayer;

namespace NewLife.Cube.Areas.Admin.Models;

public class DbTablesModel
{
    public String Name { get; set; }

    public IList<IDataTable> Tables { get; set; }
}
