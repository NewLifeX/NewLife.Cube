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
                    var type = Type.GetType(item.ClassName);
                    if (type == null)
                    {
                        if (item.FilePath.IsNullOrEmpty() || !item.FilePath.EndsWithIgnoreCase(".dll")) continue;

                        var filePath = item.FilePath.GetFullPath();
                        if (!File.Exists(filePath)) continue;

                        type = Assembly.LoadFrom(item.FilePath).GetType(item.ClassName);
                    }

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
        foreach (var item in AssemblyX.FindAllPlugins(typeof(IModule), true, true))
        {
            var att = item.GetCustomAttribute<ModuleAttribute>();
            var name = att?.Name ?? item.Name.TrimEnd("Module", "Service");

            dic[name] = item;
        }

        return dic;
    }
    #endregion
}
