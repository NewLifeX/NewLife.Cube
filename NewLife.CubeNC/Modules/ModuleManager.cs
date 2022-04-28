using System.Reflection;
using NewLife.Cube.Entity;
using NewLife.Log;
using NewLife.Reflection;

namespace NewLife.Cube.Modules;

/// <summary>
/// 模块管理
/// </summary>
public class ModuleManager
{
    #region 工厂
    /// <summary>
    /// 插件集合
    /// </summary>
    public IDictionary<String, IModule> Modules { get; private set; }

    /// <summary>
    /// 加载所有插件
    /// </summary>
    public IDictionary<String, IModule> LoadAll()
    {
        if (Modules != null) return Modules;

        var dic = new Dictionary<String, IModule>();
        var list = AppModule.FindAllWithCache();
        foreach (var item in list)
        {
            if (item.Enable && !item.ClassName.IsNullOrEmpty())
            {
                try
                {
                    var type = !item.FilePath.IsNullOrEmpty() && item.FilePath.EndsWithIgnoreCase(".dll") ?
                        Assembly.LoadFrom(item.FilePath).GetType(item.ClassName) :
                        Type.GetType(item.ClassName);

                    if (type != null)
                    {
                        if (Activator.CreateInstance(type) is IModule module) dic[item.Name] = module;
                    }
                }
                catch (Exception ex)
                {
                    XTrace.WriteException(ex);
                }
            }
        }

        return Modules = dic;
    }
    #endregion

    #region 插件
    /// <summary>
    /// 扫描所有程序集，加载插件
    /// </summary>
    public static IDictionary<String, Type> ScanAll()
    {
        var dic = new Dictionary<String, Type>();
        foreach (var item in FindAllPlugins(typeof(IModule), true, true))
        {
            var att = item.GetCustomAttribute<ModuleAttribute>();
            var name = att?.Name ?? item.Name.TrimEnd("Module", "Service");

            dic[name] = item;
        }

        return dic;
    }

    /// <summary>查找所有非系统程序集中的所有插件</summary>
    /// <remarks>继承类所在的程序集会引用baseType所在的程序集，利用这一点可以做一定程度的性能优化。</remarks>
    /// <param name="baseType"></param>
    /// <param name="isLoadAssembly">是否从未加载程序集中获取类型。使用仅反射的方法检查目标类型，如果存在，则进行常规加载</param>
    /// <param name="excludeGlobalTypes">指示是否应检查来自所有引用程序集的类型。如果为 false，则检查来自所有引用程序集的类型。 否则，只检查来自非全局程序集缓存 (GAC) 引用的程序集的类型。</param>
    /// <returns></returns>
    private static IEnumerable<Type> FindAllPlugins(Type baseType, Boolean isLoadAssembly = false, Boolean excludeGlobalTypes = true)
    {
        var baseAssemblyName = baseType.Assembly.GetName().Name;

        // 如果基类所在程序集没有强命名，则搜索时跳过所有强命名程序集
        // 因为继承类程序集的强命名要求基类程序集必须强命名
        var signs = baseType.Assembly.GetName().GetPublicKey();
        var hasNotSign = signs == null || signs.Length <= 0;

        var list = new List<Type>();
        foreach (var item in AssemblyX.GetAssemblies())
        {
            signs = item.Asm.GetName().GetPublicKey();
            if (hasNotSign && signs != null && signs.Length > 0) continue;

            // 如果excludeGlobalTypes为true，则指检查来自非GAC引用的程序集
            if (excludeGlobalTypes && item.Asm.GlobalAssemblyCache) continue;

            // 不搜索系统程序集，不搜索未引用基类所在程序集的程序集，优化性能
            if (item.IsSystemAssembly || !IsReferencedFrom(item.Asm, baseAssemblyName)) continue;

            var ts = FindPlugins(baseType, item.Types);
            foreach (var elm in ts)
                if (!list.Contains(elm))
                {
                    list.Add(elm);
                    yield return elm;
                }
        }
        if (isLoadAssembly)
            foreach (var item in AssemblyX.ReflectionOnlyGetAssemblies())
            {
                // 如果excludeGlobalTypes为true，则指检查来自非GAC引用的程序集
                if (excludeGlobalTypes && item.Asm.GlobalAssemblyCache) continue;

                // 不搜索系统程序集，不搜索未引用基类所在程序集的程序集，优化性能
                if (item.IsSystemAssembly || !IsReferencedFrom(item.Asm, baseAssemblyName)) continue;

                var ts = FindPlugins(baseType, item.Types);
                if (ts.Any())
                {
                    // 真实加载
                    if (XTrace.Debug)
                    {
                        // 如果是本目录的程序集，去掉目录前缀
                        var file = item.Asm.Location;
                        var root = ".".GetFullPath();
                        if (file.StartsWithIgnoreCase(root)) file = file.Substring(root.Length).TrimStart("\\");
                        XTrace.WriteLine("AssemblyX.FindAllPlugins(\"{0}\") => {1}", baseType.FullName, file);
                    }
                    var asm2 = Assembly.LoadFile(item.Asm.Location);
                    ts = FindPlugins(baseType, asm2.GetTypes());

                    foreach (var elm in ts)
                        if (!list.Contains(elm))
                        {
                            list.Add(elm);
                            yield return elm;
                        }
                }
            }
    }

    /// <summary><paramref name="asm"/> 是否引用了 <paramref name="baseAsmName"/></summary>
    /// <param name="asm">程序集</param>
    /// <param name="baseAsmName">被引用程序集全名</param>
    /// <returns></returns>
    private static Boolean IsReferencedFrom(Assembly asm, String baseAsmName)
    {
        //if (asm.FullName.EqualIgnoreCase(baseAsmName)) return true;
        if (asm.GetName().Name.EqualIgnoreCase(baseAsmName)) return true;

        foreach (var item in asm.GetReferencedAssemblies())
            //if (item.FullName.EqualIgnoreCase(baseAsmName)) return true;
            if (item.Name.EqualIgnoreCase(baseAsmName)) return true;

        return false;
    }

    /// <summary>查找插件，带缓存</summary>
    /// <param name="baseType">类型</param>
    /// <param name="types">所有类型</param>
    /// <returns></returns>
    private static IEnumerable<Type> FindPlugins(Type baseType, IEnumerable<Type> types)
    {
        foreach (var item in types)
        {
            if (item.IsInterface || item.IsAbstract || item.IsGenericType) continue;
            if (item != baseType && item.As(baseType)) yield return item;
        }
    }
    #endregion
}
