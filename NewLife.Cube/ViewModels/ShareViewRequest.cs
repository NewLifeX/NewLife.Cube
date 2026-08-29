namespace NewLife.Cube.ViewModels;

/// <summary>分享当前视图请求（SPA embed + UserToken）</summary>
public class ShareViewRequest
{
    /// <summary>命名视图 Id（可选）</summary>
    public String ViewId { get; set; }

    /// <summary>有效秒数；未传或 ≤0 时用 CubeSetting.ShareExpire</summary>
    public Int32 ExpireSeconds { get; set; }
}
