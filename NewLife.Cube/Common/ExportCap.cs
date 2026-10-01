namespace NewLife.Cube.Common;

/// <summary>导出行数封顶。硬顶 100 万行，配置更小的正数上限仍然有效。</summary>
public static class ExportCap
{
    /// <summary>单次导出硬顶。</summary>
    public const Int32 ExportHardCap = 1_000_000;

    /// <summary>
    /// 解析本次导出上限。
    /// 请求正数且不超过硬顶时用请求值；超过硬顶时用硬顶。
    /// 未请求时，配置正数且不超过硬顶则用配置，否则用硬顶。
    /// </summary>
    /// <param name="requested">调用方传入的行数，0 表示未指定</param>
    /// <param name="settingMax">CubeSetting.MaxExport</param>
    /// <returns>本次实际上限</returns>
    public static Int32 ResolveExportCap(Int32 requested, Int32 settingMax)
    {
        if (requested > ExportHardCap) return ExportHardCap;
        if (requested > 0) return requested;
        if (settingMax > 0 && settingMax <= ExportHardCap) return settingMax;
        return ExportHardCap;
    }
}
