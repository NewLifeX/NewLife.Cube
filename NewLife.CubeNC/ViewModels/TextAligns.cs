namespace NewLife.Cube.ViewModels;

/// <summary>文本对齐方式</summary>
/// <remarks>
/// 可选 text-center/text-left/text-right/text-justify
/// </remarks>
public enum TextAligns
{
    /// <summary>默认，文本左对齐，数字右对齐</summary>
    Default = 0,

    /// <summary>左对齐</summary>
    Left = 1,

    /// <summary>居中对齐</summary>
    Center = 2,

    /// <summary>右对齐</summary>
    Right = 3,

    /// <summary>两端对齐</summary>
    Justify = 4,

    /// <summary>阻止文字折行</summary>
    Nowrap = 5,
}
