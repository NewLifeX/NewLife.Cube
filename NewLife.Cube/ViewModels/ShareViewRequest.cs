namespace NewLife.Cube.ViewModels;

/// <summary>分享当前视图/工作台请求（SPA embed + UserToken）</summary>
/// <remarks>
/// ViewId/Slug 必须可空：.NET 6+ 非空引用类型会被 MVC 隐式 [Required]；
/// 默认工作台只传 expireSeconds，列表分享只传 viewId，缺字段即 400。
/// </remarks>
public class ShareViewRequest
{
    /// <summary>命名视图 Id（可选）</summary>
    public String? ViewId { get; set; }

    /// <summary>命名工作台 slug；空表示默认工作台 /home</summary>
    public String? Slug { get; set; }

    /// <summary>有效秒数；未传或 ≤0 时用 CubeSetting.ShareExpire</summary>
    public Int32 ExpireSeconds { get; set; }
}
