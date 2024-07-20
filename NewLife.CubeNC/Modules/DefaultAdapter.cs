using System.ComponentModel;
using NewLife.Serialization;
using NewLife.Cube.ViewModels;

namespace NewLife.Cube.Modules;

/// <summary>默认适配器</summary>
[DisplayName("魔方适配器")]
public class DefaultAdapter : IAdapter
{
    public Object Encode(Dictionary<String, Object> dic, Dictionary<ViewKinds, FieldCollection> fieldCollections) => dic;

    /// <summary>序列化配置对象</summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public Object Decode(Object obj) => obj.ToJson(true);
}