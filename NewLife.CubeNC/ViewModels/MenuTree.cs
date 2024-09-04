namespace NewLife.Cube.ViewModels;

/// <summary>菜单树</summary>
public class MenuTree
{
    /// <summary>编号</summary>
    public Int32 ID { get; set; }

    /// <summary>名称</summary>
    public String Name { get; set; }

    /// <summary>显示名</summary>
    public String DisplayName { get; set; }

    /// <summary>全名</summary>
    public String FullName { get; set; }

    /// <summary>父级编号</summary>
    public Int32? ParentID { get; set; }

    /// <summary>链接</summary>
    public String Url { get; set; }

    /// <summary>图标</summary>
    public String Icon { get; set; }

    /// <summary>是否可见</summary>
    public Boolean Visible { get; set; }

    /// <summary>是否新窗口打开</summary>
    public Boolean NewWindow { get; set; }

    /// <summary>可选权限子项</summary>
    public Dictionary<Int32, String> Permissions { get; set; }

    /// <summary>子菜单</summary>
    public IList<MenuTree> Children { get => GetChildren?.Invoke(this) ?? null; set { } }

    /// <summary>
    /// 获取子菜单的方法
    /// </summary>
    private static Func<MenuTree, IList<MenuTree>> GetChildren;

    /// <summary>
    /// 获取菜单树
    /// </summary>
    /// <param name="getChildrenSrc">自定义的获取子菜单需要数据的方法</param>
    /// <param name="getMenuList">获取菜单列表的方法</param>
    /// <param name="src">获取菜单列表的初始数据来源</param>
    /// <returns></returns>
    public static IList<MenuTree> GetMenuTree<T>(Func<MenuTree, T> getChildrenSrc, Func<T, IList<MenuTree>> getMenuList, T src) where T : class
    {
        GetChildren = m => getMenuList?.Invoke(getChildrenSrc(m));

        return getMenuList?.Invoke(src);
    }

    /// <summary>
    /// 已重载。
    /// </summary>
    /// <returns></returns>
    public override String ToString() => Name;
}