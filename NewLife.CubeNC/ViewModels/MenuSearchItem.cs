namespace NewLife.Cube.ViewModels;

/// <summary>菜单搜索项。外壳页面内置下发，供前端做菜单模糊搜索</summary>
public class MenuSearchItem
{
    /// <summary>编号</summary>
    public Int32 ID { get; set; }

    /// <summary>名称</summary>
    public String Name { get; set; }

    /// <summary>显示名</summary>
    public String DisplayName { get; set; }

    /// <summary>链接</summary>
    public String Url { get; set; }

    /// <summary>图标</summary>
    public String Icon { get; set; }

    /// <summary>是否新窗口打开</summary>
    public Boolean NewWindow { get; set; }

    /// <summary>所属模块。顶层菜单名；非模块菜单归入 base</summary>
    public String Module { get; set; }

    /// <summary>层级路径。从顶层到当前菜单的显示名</summary>
    public String Path { get; set; }

    /// <summary>拼音全拼。按库中源文显示名计算，支持 yonghu 这类输入</summary>
    public String PinYin { get; set; }

    /// <summary>拼音首字母。按库中源文显示名计算，支持 yh 这类输入</summary>
    public String Initials { get; set; }

    /// <summary>层级路径拼音全拼</summary>
    public String PathPinYin { get; set; }

    /// <summary>层级路径拼音首字母</summary>
    public String PathInitials { get; set; }
}
