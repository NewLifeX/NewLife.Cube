using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.Entity;
using NewLife.Cube.Extensions;
using NewLife.Cube.ViewModels;
using NewLife.Cube.Web;
using NewLife.Web;
using XCode;
using XCode.Membership;

namespace NewLife.Cube.Areas.Cube.Controllers;

/// <summary>附件管理</summary>
[DataPermission(null, "CreateUserID={#userId}")]
[CubeArea]
[Menu(38, true, Icon = "fa-file-text")]
public class AttachmentController : EntityController<Attachment, AttachmentModel>
{
    static AttachmentController()
    {
        ListFields.RemoveField("Id", "Hash", "Url", "Source", "UpdateUserID", "UpdateIP", "Remark");
        ListFields.RemoveCreateField();

        {
            var df = ListFields.GetField("Category") as ListField;
            df.Url = "/Cube/Attachment?category={Category}";
        }
        {
            var df = ListFields.GetField("Key") as ListField;
            df.Url = "/Cube/Attachment?category={Category}&key={Key}";
        }
        {
            var df = ListFields.GetField("Extension") as ListField;
            df.Url = "/Cube/Attachment?ext={Extension}";
        }

        {
            var df = ListFields.GetField("Storage") as ListField;
            df.GetValue = e => (e as Attachment).StorageName;
        }

        {
            var df = ListFields.AddListField("Info", null, "Title");
            df.DisplayName = "Info page";
            df.Url = "{Url}";
            df.DataVisible = e => !(e as Attachment).Url.IsNullOrEmpty();
            df.Target = "_blank";
        }

        {
            var df = ListFields.AddListField("cloudUrl", null, "Title");
            df.DisplayName = "Cloud URL";
            df.Target = "_blank";
            df.DataVisible = e => !(e as Attachment).IsLocalStorage();
            df.GetValue = e => AttachmentProvider.Provider.GetUrl((e as Attachment).FilePath) + "";
        }

        {
            var df = ListFields.AddListField("down", null, "Title");
            df.DisplayName = "Download";
            df.Url = "/cube/file/{Id}{Extension}";
            df.Target = "_blank";
        }

        {
            SearchFields.AddField("Storage");
            var sf = SearchFields.GetField("Storage") as SearchField;
            sf.DataSource = e => new Dictionary<String, String>
            {
                ["Local"] = "Local disk",
                ["Oss"] = "Aliyun OSS",
                ["Cos"] = "Tencent COS",
                ["Qiniu"] = "Qiniu",
                ["EasyIO"] = "EasyIO",
            };
        }

        ListFields.TraceUrl();
    }

    /// <summary>搜索</summary>
    /// <param name="p"></param>
    /// <returns></returns>
    protected override IEnumerable<Attachment> Search(Pager p)
    {
        var category = p["category"];
        var key = p["key"];
        var ext = p["ext"];

        var start = p["dtStart"].ToDateTime();
        var end = p["dtEnd"].ToDateTime();

        if (p.Sort.IsNullOrEmpty()) p.Sort = AppLog._.Id.Desc();

        return Attachment.Search(category, ext, p["Q"], p["Storage"], key, start, end, p);
    }
}