using System.Reflection;
using NewLife.Log;
using XCode.Membership;

namespace NewLife.Cube.Widgets;

/// <summary>扫描并缓存 ICubeWidget</summary>
public static class CubeWidgetManager
{
    static readonly Object _lock = new();
    static List<CubeWidgetRegistration> _all;

    /// <summary>全部已扫描注册</summary>
    public static IList<CubeWidgetRegistration> All
    {
        get
        {
            if (_all != null) return _all;
            lock (_lock)
            {
                _all ??= Scan();
            }
            return _all;
        }
    }

    /// <summary>测试重置</summary>
    public static void Reset()
    {
        lock (_lock) _all = null;
    }

    /// <summary>按名查找</summary>
    public static CubeWidgetRegistration Find(String name)
    {
        if (name.IsNullOrEmpty()) return null;
        return All.FirstOrDefault(e => e.Name.EqualIgnoreCase(name));
    }

    /// <summary>当前用户可见的 named 目录</summary>
    public static IList<CubeWidgetRegistration> CatalogFor(IUser user) =>
        All.Where(e => Visible(e, user)).ToList();

    /// <summary>是否对该用户可见</summary>
    public static Boolean Visible(CubeWidgetRegistration reg, IUser user)
    {
        if (reg == null || user == null) return false;
        if (reg.AdminOnly && (user.Roles == null || !user.Roles.Any(r => r.IsSystem))) return false;
        if (reg.Permission.IsNullOrEmpty()) return true;
        var names = reg.Permission.Split(',', ';').Select(e => e.Trim()).Where(e => e.Length > 0).ToArray();
        if (names.Length == 0) return true;
        return user.Roles != null && user.Roles.Any(r => names.Contains(r.Name, StringComparer.OrdinalIgnoreCase));
    }

    static List<CubeWidgetRegistration> Scan()
    {
        var list = new List<CubeWidgetRegistration>();
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type[] types;
            try { types = asm.GetTypes(); }
            catch { continue; }
            foreach (var t in types)
            {
                if (t.IsAbstract || t.IsInterface) continue;
                if (!typeof(ICubeWidget).IsAssignableFrom(t)) continue;
                var att = t.GetCustomAttribute<CubeWidgetAttribute>();
                if (att == null || att.Name.IsNullOrEmpty()) continue;
                list.Add(new CubeWidgetRegistration
                {
                    Name = att.Name,
                    Title = att.Title.IsNullOrEmpty() ? att.Name : att.Title,
                    Kind = att.Kind.IsNullOrEmpty() ? "metricCard" : att.Kind,
                    Cols = att.Cols <= 0 ? 3 : att.Cols,
                    AdminOnly = att.AdminOnly,
                    Permission = att.Permission,
                    Surfaces = att.Surfaces,
                    Type = t,
                });
            }
        }
        XTrace.WriteLine("CubeWidget 扫描到 {0} 个 named 部件", list.Count);
        return list;
    }
}

/// <summary>named Widget 注册项</summary>
public sealed class CubeWidgetRegistration
{
    /// <summary>名称</summary>
    public String Name { get; set; }

    /// <summary>标题</summary>
    public String Title { get; set; }

    /// <summary>kind</summary>
    public String Kind { get; set; }

    /// <summary>栅格</summary>
    public Int32 Cols { get; set; }

    /// <summary>仅管理员</summary>
    public Boolean AdminOnly { get; set; }

    /// <summary>角色</summary>
    public String Permission { get; set; }

    /// <summary>表面</summary>
    public String Surfaces { get; set; }

    /// <summary>实现类型</summary>
    public Type Type { get; set; }

    /// <summary>创建实例</summary>
    public ICubeWidget Create() => Activator.CreateInstance(Type) as ICubeWidget;
}
