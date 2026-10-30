using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using NewLife.Log;
using NewLife.Reflection;

namespace NewLife.Cube.Modules;

/// <summary>模块管理器。自动发现已引用程序集中的功能模块与适配器，并按声明顺序装配</summary>
/// <remarks>
/// 魔方采用「引用即安装」：程序集引用是唯一的安装事实源，模块不需要在数据库登记。
/// 自动发现扫描所有引用了魔方的程序集；裁剪/AOT 或单元测试场景可改用
/// <see cref="CubeModuleExtensions.AddCubeModule{T}(IServiceCollection)"/> 显式注册。
/// </remarks>
public class ModuleManager
{
    #region 属性
    private readonly Object _lock = new();
    private volatile IDictionary<String, IModule>? _modules;
    private volatile IDictionary<String, IAdapter>? _adapters;

    /// <summary>模块集合，键为模块名称</summary>
    public IDictionary<String, IModule> Modules => _modules ??= Load();

    /// <summary>适配器集合，键为适配器名称。始终包含 Default</summary>
    public IDictionary<String, IAdapter> Adapters => _adapters ??= LoadAdapters();
    #endregion

    #region 加载
    /// <summary>加载所有模块。扫描已引用程序集并合并显式注册，结果缓存，可重复调用</summary>
    /// <param name="services">服务集合，用于读取通过 AddCubeModule 显式注册的模块</param>
    /// <returns>模块集合，键为模块名称</returns>
    public IDictionary<String, IModule> Load(IServiceCollection? services = null)
    {
        var modules = _modules;
        if (modules != null) return modules;

        lock (_lock)
        {
            if (_modules != null) return _modules;

            var list = new List<ModuleInfo>();
            foreach (var type in FindModuleTypes(services))
            {
                try
                {
                    if (type.CreateInstance() is not IModule module) continue;

                    var att = type.GetCustomAttribute<ModuleAttribute>();
                    list.Add(new ModuleInfo(GetName(att, type), att?.Order ?? 0, module));
                }
                catch (Exception ex)
                {
                    XTrace.WriteException(ex);
                }
            }

            // 顺序稳定：Order 升序，其次名称。重名保留第一个并告警，避免装配结果随扫描顺序漂移
            var dic = new Dictionary<String, IModule>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in list.OrderBy(e => e.Order).ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase))
            {
                if (dic.ContainsKey(item.Name))
                {
                    XTrace.WriteLine("Duplicate module [{0}] was ignored, keep the first one", item.Name);
                    continue;
                }

                dic[item.Name] = item.Module;
            }

            return _modules = dic;
        }
    }

    /// <summary>加载所有模块。兼容旧入口，等价于 <see cref="Load(IServiceCollection?)"/></summary>
    /// <param name="services">服务集合</param>
    /// <returns>模块集合</returns>
    public IDictionary<String, IModule> LoadAll(IServiceCollection? services = null) => Load(services);

    /// <summary>使用所有模块。在路由注册之前调用，用于注册中间件</summary>
    /// <param name="app">应用管道</param>
    /// <param name="env">Web宿主环境</param>
    public void Use(IApplicationBuilder app, IWebHostEnvironment env)
    {
        foreach (var item in Modules)
        {
            try
            {
                item.Value.Use(app, env);
            }
            catch (Exception ex)
            {
                XTrace.WriteLine("Failed to use module [{0}]", item.Key);
                XTrace.WriteException(ex);
            }
        }
    }

    /// <summary>添加所有模块。在依赖注入注册阶段调用，单个模块异常不影响其它模块</summary>
    /// <param name="services">服务集合</param>
    public void Add(IServiceCollection services)
    {
        foreach (var item in Modules)
        {
            try
            {
                item.Value.Add(services);
            }
            catch (Exception ex)
            {
                XTrace.WriteLine("Failed to add module [{0}]", item.Key);
                XTrace.WriteException(ex);
            }
        }
    }

    /// <summary>映射所有模块端点。在路由注册内部调用，位于 SPA 回退之前</summary>
    /// <param name="endpoints">端点路由</param>
    public void Map(IEndpointRouteBuilder endpoints)
    {
        foreach (var item in Modules)
        {
            if (item.Value is not IModuleEndpoint module) continue;

            try
            {
                module.Map(endpoints);
            }
            catch (Exception ex)
            {
                XTrace.WriteLine("Failed to map module [{0}]", item.Key);
                XTrace.WriteException(ex);
            }
        }
    }

    /// <summary>按名称查找适配器</summary>
    /// <param name="name">适配器名称，忽略大小写</param>
    /// <returns></returns>
    public IAdapter? GetAdapter(String? name) => name.IsNullOrEmpty() ? null : Adapters.TryGetValue(name, out var adapter) ? adapter : null;
    #endregion

    #region 辅助
    private static String GetName(ModuleAttribute? att, Type type)
    {
        if (att != null && !att.Name.IsNullOrEmpty()) return att.Name;

        return type.Name.TrimSuffix("Module").TrimSuffix("Service").TrimSuffix("Adapter");
    }

    private static IEnumerable<Type> FindModuleTypes(IServiceCollection? services)
    {
        var set = new HashSet<Type>();
        if (services != null)
        {
            foreach (var item in services)
            {
                if (item.ServiceType == typeof(CubeModuleType) && item.ImplementationInstance is CubeModuleType module) set.Add(module.Type);
            }
        }

        foreach (var type in FindAllModules()) set.Add(type);

        return set;
    }

    [UnconditionalSuppressMessage("AOT", "IL2026", Justification = "运行时扫描模块程序集；AOT 下需显式注册模块")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "运行时扫描模块程序集；AOT 下需显式注册模块")]
    private static IEnumerable<Type> FindAllModules() => AssemblyX.FindAllPlugins(typeof(IModule), true, true);

    /// <summary>扫描所有程序集中的模块类型</summary>
    /// <returns>模块名称到类型的字典</returns>
    [UnconditionalSuppressMessage("AOT", "IL2026", Justification = "运行时扫描模块程序集；AOT 下需显式注册模块")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "运行时扫描模块程序集；AOT 下需显式注册模块")]
    public static IDictionary<String, Type> ScanAllModules()
    {
        var dic = new Dictionary<String, Type>(StringComparer.OrdinalIgnoreCase);
        foreach (var type in FindAllModules())
        {
            var att = type.GetCustomAttribute<ModuleAttribute>();
            dic[GetName(att, type)] = type;
        }

        return dic;
    }

    /// <summary>扫描所有程序集中的适配器类型</summary>
    /// <returns>适配器名称到类型的字典</returns>
    [UnconditionalSuppressMessage("AOT", "IL2026", Justification = "运行时扫描适配器程序集；AOT 下需显式注册适配器")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "运行时扫描适配器程序集；AOT 下需显式注册适配器")]
    public static IDictionary<String, Type> ScanAllAdapters()
    {
        var dic = new Dictionary<String, Type>(StringComparer.OrdinalIgnoreCase);
        foreach (var type in AssemblyX.FindAllPlugins(typeof(IAdapter), true, true))
        {
            var att = type.GetCustomAttribute<ModuleAttribute>();
            dic[GetName(att, type)] = type;
        }

        return dic;
    }

    private IDictionary<String, IAdapter> LoadAdapters()
    {
        var adapters = _adapters;
        if (adapters != null) return adapters;

        lock (_lock)
        {
            if (_adapters != null) return _adapters;

            var dic = new Dictionary<String, IAdapter>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in ScanAllAdapters())
            {
                try
                {
                    if (item.Value.CreateInstance() is IAdapter adapter) dic[item.Key] = adapter;
                }
                catch (Exception ex)
                {
                    XTrace.WriteException(ex);
                }
            }

            // 默认适配器必须始终可用，避免页面配置接口在未注册适配器时退化
            if (!dic.ContainsKey("Default")) dic["Default"] = new DefaultAdapter();

            return _adapters = dic;
        }
    }

    private sealed class ModuleInfo(String name, Int32 order, IModule module)
    {
        public String Name { get; } = name;

        public Int32 Order { get; } = order;

        public IModule Module { get; } = module;
    }
    #endregion
}
