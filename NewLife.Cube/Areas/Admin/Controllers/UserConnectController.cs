using System.ComponentModel;
using NewLife.Cube.Entity;
using NewLife.Cube.ViewModels;
using NewLife.Web;
using XCode.Membership;

namespace NewLife.Cube.Areas.Admin.Controllers;

/// <summary>用户链接控制器</summary>
[DataPermission(null, "UserID={#userId}")]
[DisplayName("用户链接")]
[Description("第三方登录信息")]
[AdminArea]
[Menu(0, false)]
public class UserConnectController : EntityController<UserConnect, UserConnectModel>
{
    static UserConnectController()
    {
        ListFields.RemoveField("AccessToken", "RefreshToken", "Avatar", "UpdateUserID");
        ListFields.RemoveCreateField();

        // 提供者列，增加查询
        {
            var df = ListFields.GetField("Provider") as ListField;
            //df.DisplayName = "{Provider}";
            df.Url = "/Admin/UserConnect?provider={Provider}";
        }

        // 用户列，增加连接
        {
            var df = ListFields.GetField("UserName") as ListField;
            df.Header = "用户";
            df.HeaderTitle = "对应的本地用户信息";
            //df.DisplayName = "{UserName}";
            df.Url = "/Admin/User?id={UserID}";
        }

        {
            var df = ListFields.AddListField("OAuthLog", "Enable");
            //df.Header = "OAuth日志";
            df.DisplayName = "OAuth日志";
            df.Url = "/Admin/OAuthLog?connectId={ID}";
        }

        //// 插入一列
        //{
        //    var df = ListFields.AddDataField("用户信息", "CreateUserID");
        //    df.DisplayName = "用户信息";
        //    df.Url = "User?id={UserID}";
        //}
    }

    /// <summary>构造</summary>
    public UserConnectController() => PageSetting.EnableAdd = false;

    /// <summary>搜索数据集</summary>
    /// <param name="p"></param>
    /// <returns></returns>
    protected override IEnumerable<UserConnect> Search(Pager p)
    {
        var key = p["Q"];
        var userid = p["userid"].ToInt();
        var provider = p["provider"];
        var start = p["dtStart"].ToDateTime();
        var end = p["dtEnd"].ToDateTime();

        return UserConnect.Search(provider, userid, start, end, key, p);
    }
}