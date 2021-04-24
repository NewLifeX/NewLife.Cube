using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.Entity;

namespace NewLife.Cube.Admin.Controllers
{
    /// <summary>委托代理</summary>
    [Area("Admin")]
    public class PrincipalAgentController : EntityController<PrincipalAgent>
    {
    }
}