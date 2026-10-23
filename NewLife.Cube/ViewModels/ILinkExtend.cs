using NewLife.Data;

namespace NewLife.Cube.ViewModels;

/// <summary>超链接扩展</summary>
public interface ILinkExtend
{
    /// <summary>解析超链接HTML</summary>
    /// <param name="field"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    String Resolve(DataField field, IModel data);
}