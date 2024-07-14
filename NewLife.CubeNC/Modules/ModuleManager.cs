using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
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
    /// <summary>模块插件集合</summary>
    public IDictionary<String, IModule> Modules { get; private set; }

    /// <summary>模块插件集合</summary>
    public IDictionary<String, IAdapter> Adapters { get; private set; }

    /// <summary>
    /// 加载所有插件
    /// </summary>
    public IDictionary<String, IModule> LoadAll(IServiceCollection services = null)
    {
        if (Modules != null) return Modules;

        var modules = new Dictionary<String, IModule>();
        var adapters = new Dictionary<String, IAdapter>();
        var list = AppModule.FindAllWithCache();
        foreach (var item in list)
        {
            if (!item.Enable || item.ClassName.IsNullOrEmpty()) continue;

            try
            {
                var type = Type.GetType(item.ClassName);
                if (type == null)
                {
                    if (item.FilePath.IsNullOrEmpty() || !item.FilePath.EndsWithIgnoreCase(".dll")) continue;

                    type = item.ClassName.GetTypeEx();
                    if (type == null)
                    {
                        var filePath = item.FilePath.GetFullPath();
                        if (!File.Exists(filePath)) continue;

                        var assembly = Assembly.LoadFrom(filePath);
                        type = assembly.GetType(item.ClassName);
                    }

                    if (item.Type == "Module")
                    {
                        services?.AddMvc()
                                .ConfigureApplicationPartManager(_ =>
                                {
                                    _.ApplicationParts.Add(new CompiledRazorAssemblyPart(type.Assembly));
                                });
                    }
                }

                if (type != null)
                {
                    if (item.Type == "Module")
                    {
                        if (Activator.CreateInstance(type) is IModule module) modules[item.Name] = module;
                    }
                    else if (item.Type == "Adapter")
                    {
                        if (Activator.CreateInstance(type) is IAdapter module) adapters[item.Name] = module;
                    }
                }
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);
            }
        }

        Adapters = adapters;

        return Modules = modules;
    }

    /// <summary>获取指定名称的适配器</summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public IAdapter GetAdapter(String name)
    {
        if (name.IsNullOrEmpty()) name = "Default";
        if (Adapters.TryGetValue(name, out var module)) return module;

        return null;
    }
    #endregion

    #region 插件
    /// <summary>
    /// 扫描所有程序集，加载插件
    /// </summary>
    public static IDictionary<String, Type> ScanAllModules()
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

    /// <summary>扫描加载适配器插件</summary>
    public static IDictionary<String, Type> ScanAllAdapters()
    {
        var dic = new Dictionary<String, Type>();
        foreach (var item in AssemblyX.FindAllPlugins(typeof(IAdapter), true, true))
        {
            var att = item.GetCustomAttribute<ModuleAttribute>();
            var name = att?.Name ?? item.Name.TrimEnd("Adapter");

            dic[name] = item;
        }

        return dic;
    }

    /// <summary>合并插件数据</summary>
    /// <param name="name"></param>
    /// <param name="type"></param>
    /// <param name="modules"></param>
    /// <param name="kind"></param>
    public static void Merge(String name, Type type, IList<AppModule> modules, String kind)
    {
        var drv = modules.FirstOrDefault(e => e.Name.EqualIgnoreCase(name));
        drv ??= new AppModule { Name = name, Enable = true };

        if (drv.DisplayName.IsNullOrEmpty() || drv.DisplayName == drv.Name)
        {
            var dname = type.GetDisplayName();
            if (!dname.IsNullOrEmpty()) drv.DisplayName = dname;
        }

        if (drv.Type.IsNullOrEmpty()) drv.Type = kind;
        drv.ClassName = type.FullName;

        var file = type.Assembly?.Location;
        if (!file.IsNullOrEmpty())
        {
            var root = ".".GetFullPath();
            if (file.StartsWithIgnoreCase(root))
                file = file[root.Length..].TrimStart('/', '\\');
        }
        if (!file.IsNullOrEmpty()) drv.FilePath = file;

        drv.Save();

        modules.Add(drv);
    }
    #endregion
}
