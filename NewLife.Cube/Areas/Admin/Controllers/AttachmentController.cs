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
        static AttachmentController()
        {
            ListFields.RemoveField("ID", "Hash", "Url", "Source", "UpdateUserID", "UpdateIP", "Remark");
            ListFields.RemoveCreateField();

            {
                var df = ListFields.AddListField("Info", null, "Title");
                df.DisplayName = "信息页";
                df.Url = "{Url}";
                df.DataVisible = (e, f) => !(e as Attachment).Url.IsNullOrEmpty();
            }
        }
    }
}