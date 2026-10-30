using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.FileProviders;
using NewLife.Log;
using NewLife.Reflection;

namespace NewLife.Cube;

/// <summary>前端皮肤管理器。自动发现所有皮肤包实现，并按当前设置解析生效皮肤</summary>
/// <remarks>
/// 皮肤标识来自各皮肤包的 <see cref="ICubeSkin.Name"/>，默认忽略大小写匹配。
/// 解析优先级：显式强制皮肤（UseVue/UseReact 等） → 设置项 <see cref="CubeSetting.SpaSkin"/> → 内置默认（Vue，其次按名称排序首个）。
/// </remarks>
public class CubeSkinManager
{
    #region 属性
    private readonly Object _lock = new();
    private volatile IDictionary<String, ICubeSkin>? _skins;
    private readonly ConcurrentDictionary<String, IFileProvider> _providers = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>已发现的皮肤集合，键为皮肤标识（忽略大小写）</summary>
    public IDictionary<String, ICubeSkin> Skins
    {
        get
        {
            var skins = _skins;
            if (skins != null) return skins;

            lock (_lock)
            {
                return _skins ??= LoadSkins();
            }
        }
    }

    /// <summary>强制皮肤标识。显式调用 UseVue/UseReact 时指定，优先于设置项</summary>
    public String? Forced { get; set; }

    /// <summary>当前皮肤标识。每次访问按设置实时解析，修改设置后立即生效</summary>
    public String? CurrentName
    {
        get
        {
            if (!Forced.IsNullOrEmpty()) return Forced;

            var name = CubeSetting.Current.SpaSkin;
            if (!name.IsNullOrEmpty())
            {
                if (Skins.ContainsKey(name)) return name;

                XTrace.WriteLine("Configured SPA skin [{0}] was not found, fallback to the default skin", name);
            }

            // 默认优先官方 Vue 皮肤，其它情况按名称排序取第一个，保证多个皮肤包同时引用时行为确定
            if (Skins.ContainsKey("Vue")) return "Vue";

            return Skins.Keys.OrderBy(e => e, StringComparer.OrdinalIgnoreCase).FirstOrDefault();
        }
    }

    /// <summary>当前皮肤。未加载到任何皮肤包时返回空</summary>
    public ICubeSkin? Current => CurrentName is { } name && Skins.TryGetValue(name, out var skin) ? skin : null;
    #endregion

    #region 方法
    /// <summary>重新扫描所有皮肤包，并清空文件提供者缓存</summary>
    public void Load()
    {
        lock (_lock)
        {
            _skins = LoadSkins();
            _providers.Clear();
        }
    }

    /// <summary>按名称查找皮肤</summary>
    /// <param name="name">皮肤标识，忽略大小写</param>
    /// <returns></returns>
    public ICubeSkin? Find(String? name) => name.IsNullOrEmpty() ? null : Skins.TryGetValue(name, out var skin) ? skin : null;

    /// <summary>获取皮肤的静态资源提供者，创建后缓存</summary>
    /// <param name="skin">皮肤</param>
    /// <returns></returns>
    public IFileProvider? GetFileProvider(ICubeSkin skin)
    {
        if (skin == null) return null;

        return _providers.GetOrAdd(skin.Name, _ => skin.CreateFileProvider());
    }

    private static IDictionary<String, ICubeSkin> LoadSkins()
    {
        var skins = new Dictionary<String, ICubeSkin>(StringComparer.OrdinalIgnoreCase);
        foreach (var type in FindAllSkins())
        {
            try
            {
                if (type.CreateInstance() is ICubeSkin skin && !skin.Name.IsNullOrEmpty()) skins[skin.Name] = skin;
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);
            }
        }

        return skins;
    }

    [UnconditionalSuppressMessage("AOT", "IL2026", Justification = "运行时扫描皮肤包程序集；AOT 下需显式注册皮肤")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "运行时扫描皮肤包程序集；AOT 下需显式注册皮肤")]
    private static IEnumerable<Type> FindAllSkins() => AssemblyX.FindAllPlugins(typeof(ICubeSkin), true, true);
    #endregion
}
