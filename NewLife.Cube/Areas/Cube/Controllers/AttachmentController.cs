using NewLife.Cube.Entity;
using NewLife.Cube.ViewModels;
using NewLife.Cube.Web;
using NewLife.Web;
using XCode;
using XCode.Membership;

namespace NewLife.Cube.Areas.Cube.Controllers;

/// <summary>附件管理</summary>
[CubeArea]
[Menu(38, true, Icon = "Document")]
public class AttachmentController : EntityController<Attachment, AttachmentModel>
{
    static AttachmentController()
    {
        ListFields.RemoveField("Hash", "Url", "Source", "UpdateUserID", "UpdateIP", "Remark");
        ListFields.RemoveCreateField();

        {
            var df = ListFields.GetField("Category") as ListField;
            df.Url = "/Cube/Area?category={Category}";
        }
        {
            var df = ListFields.GetField("Key") as ListField;
            df.Url = "/Cube/Area?category={Category}&key={Key}";
        }
        {
            var df = ListFields.GetField("Extension") as ListField;
            df.Url = "/Cube/Area?ext={Extension}";
        }

        {
            var df = ListFields.GetField("Storage") as ListField;
            df.GetValue = e => (e as Attachment).StorageName;
        }

        {
            var df = ListFields.AddListField("Info", null, "Title");
            df.DisplayName = "信息页";
            df.Url = "{Url}";
            df.DataVisible = e => !(e as Attachment).Url.IsNullOrEmpty();
        }

        {
            var df = ListFields.AddListField("cloudUrl", null, "Title");
            df.DisplayName = "云地址";
            df.Target = "_blank";
            df.DataVisible = e => !(e as Attachment).IsLocalStorage();
            df.GetValue = e => AttachmentProvider.Provider.GetUrl((e as Attachment).FilePath) + "";
        }

        {
            var df = ListFields.AddListField("down", null, "Title");
            df.DisplayName = "下载";
            df.Url = "/cube/file/{Id}{Extension}";
            df.Target = "blank";
        }

        {
            SearchFields.AddField("Storage");
            var sf = SearchFields.GetField("Storage") as SearchField;
            sf.DataSource = e => new Dictionary<String, String>
            {
                ["Local"] = "本地磁盘",
                ["Oss"] = "阿里云OSS",
                ["Cos"] = "腾讯云COS",
                ["Qiniu"] = "七牛",
                ["EasyIO"] = "EasyIO",
            };
        }
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

        return Attachment.Search(category, key, ext, start, end, p["Q"], p, p["Storage"]);
    }
}