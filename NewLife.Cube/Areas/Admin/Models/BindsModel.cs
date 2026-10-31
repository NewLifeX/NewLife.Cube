namespace NewLife.Cube.Areas.Admin.Models;

/// <summary>第三方绑定模型</summary>
public class BindsModel : ICubeModel
{
    /// <summary>用户名</summary>
    public String Name { get; set; }

    /// <summary>用户链接集合</summary>
    public IList<UserBindModel> Connects { get; set; }

    /// <summary>可选的第三方</summary>
    public IList<OAuthPlatformModel> OAuthItems { get; set; }
}

/// <summary>第三方绑定项。只回传展示信息，令牌等只写字段不下发</summary>
public class UserBindModel
{
    /// <summary>编号</summary>
    public Int32 ID { get; set; }

    /// <summary>平台标识</summary>
    public String Provider { get; set; }

    /// <summary>第三方昵称</summary>
    public String NickName { get; set; }

    /// <summary>第三方头像</summary>
    public String Avatar { get; set; }

    /// <summary>是否启用。解绑后为 false</summary>
    public Boolean Enable { get; set; }

    /// <summary>绑定时间</summary>
    public DateTime CreateTime { get; set; }
}

/// <summary>可绑定的第三方平台。只回传展示信息，应用密钥不下发</summary>
public class OAuthPlatformModel
{
    /// <summary>编号</summary>
    public Int32 ID { get; set; }

    /// <summary>平台标识。绑定/解绑 URL 使用</summary>
    public String Name { get; set; }

    /// <summary>平台显示名</summary>
    public String NickName { get; set; }

    /// <summary>平台 Logo</summary>
    public String Logo { get; set; }

    /// <summary>是否启用</summary>
    public Boolean Enable { get; set; }

    /// <summary>是否可见</summary>
    public Boolean Visible { get; set; }
}