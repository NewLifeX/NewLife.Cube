using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.Entity;
using XCode.Membership;

namespace NewLife.Cube.Cube.Controllers
{
    /// <summary>附件管理</summary>
    [Area("Cube")]
    [Menu(38, true, Icon = "fa-file-text")]
    public class AttachmentController : EntityController<Attachment>
    {
    }
}