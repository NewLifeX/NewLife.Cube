namespace NewLife.Cube;

/// <summary>菜单模式</summary>
[Flags]
public enum MenuModes
{
    /// <summary>管理后台可见</summary>
    Admin = 1,

    /// <summary>租户可见</summary>
    Tenant = 2,
}

/// <summary>菜单特性</summary>
public class MenuAttribute : Attribute
{
    #region 属性
    /// <summary>
    /// 顺序。较大者在前面
    /// </summary>
    public Int32 Order { get; set; }

    /// <summary>
    /// 可见
    /// </summary>
    public Boolean Visible { get; set; } = true;

    /// <summary>菜单模式。控制在管理后台和租户模式下是否可见</summary>
    public MenuModes Mode { get; set; }

    /// <summary>
    /// 图标
    /// </summary>
    public String Icon { get; set; }

    /// <summary>帮助文档地址</summary>
    public String HelpUrl { get; set; }

    /// <summary>最后更新时间。小于该更新时间的菜单设置将被覆盖。</summary>
    /// <remarks>一般应用于区域类，表明代码已修改菜单参数，希望强行覆盖已有菜单设置</remarks>
    public String LastUpdate { get; set; }
    #endregion

    /// <summary>
    /// 设置菜单特性
    /// </summary>
    /// <param name="order"></param>
    /// <param name="visible"></param>
    public MenuAttribute(Int32 order, Boolean visible = true)
    {
        Order = order;
        Visible = visible;
    }
}