using XCode.Membership;

namespace NewLife.Cube.Widgets.System;

/// <summary>个人信息。当前登录用户的账号信息，所有用户可见，普通用户工作台的默认首页组件</summary>
[Widget("Profile", "个人信息", Icon = "fa-user-circle", Cols = 6, Sort = 5, Category = "个人")]
public class ProfileWidget : IWidget
{
    /// <summary>获取组件数据</summary>
    /// <returns>个人信息匿名对象</returns>
    public Object GetData()
    {
        var user = ManageProvider.User as User;

        var display = user?.DisplayName;
        if (display.IsNullOrEmpty()) display = user?.Name;

        return new
        {
            Name = user?.Name,
            DisplayName = display,
            RoleNames = user?.RoleNames,
            Online = user?.Online == true,
            Logins = user?.Logins ?? 0,
            LastLogin = user != null && user.LastLogin.Year > 2000 ? user.LastLogin.ToFullString() : "",
            LastLoginIP = user?.LastLoginIP,
            RegisterTime = user != null && user.RegisterTime.Year > 2000 ? user.RegisterTime.ToFullString() : "",
        };
    }
}
