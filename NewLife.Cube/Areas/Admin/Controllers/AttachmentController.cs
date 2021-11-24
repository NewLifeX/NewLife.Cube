using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.Entity;
using XCode.Membership;

namespace NewLife.Cube.Admin.Controllers
{
    /// <summary>附件管理</summary>
    [Area("Admin")]
    [Menu(38, false, Icon = "fa-file-text")]
    public class AttachmentController : EntityController<Attachment>
    {
    }
}