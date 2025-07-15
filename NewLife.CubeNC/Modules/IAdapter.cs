using NewLife.Cube.ViewModels;

namespace NewLife.Cube.Modules;

/// <summary>魔方适配器插件。用户给前端提供合适的页面配置</summary>
public interface IAdapter
{
    /// <summary>编码配置对象</summary>
    Object Encode(Dictionary<String, Object> dic, Dictionary<ViewKinds, FieldCollection> fieldCollections);

    /// <summary>序列化配置对象</summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    Object Decode(Object obj);
}
