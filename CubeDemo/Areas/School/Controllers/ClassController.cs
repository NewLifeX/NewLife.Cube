using NewLife.Cube;
using NewLife.School.Entity;

namespace CubeDemo.Areas.School.Controllers
{
    public class ClassController : EntityController<Class>
    {
        static ClassController()
        {
            ListFields.RemoveField("Remark");
        }
    }
}