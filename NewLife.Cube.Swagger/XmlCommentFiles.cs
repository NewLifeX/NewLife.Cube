using System;
using System.Collections.Generic;
using System.IO;
using NewLife;

namespace NewLife.Cube.Swagger;

/// <summary>XML 注释文件收集：并联「已加载程序集」中与程序集同目录同名的 .xml（开 <c>GenerateDocumentationFile</c> 即随构建产出）。
/// 项目侧零接线——框架统一导入，Swagger/Scalar 与构建期契约（内建 OpenAPI）据此显示项目侧中文注释。
/// 用 AppDomain 扫描而非 <c>Assembly.GetEntryAssembly()</c>：构建期导出 host（ApiDescription.Server）里后者不可靠
/// （同 AreaBase 扫描的坑，见 project-init contract-workflow 坑⑤）。</summary>
public static class XmlCommentFiles
{
    /// <summary>收集可用的 XML 注释文件路径（去重）。</summary>
    /// <returns>文件路径集合</returns>
    public static IList<String> Collect()
    {
        var list = new List<String>();
        var seen = new HashSet<String>(StringComparer.OrdinalIgnoreCase);
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                if (asm.IsDynamic) continue;

                var file = asm.Location;
                if (file.IsNullOrEmpty()) continue;

                var xml = Path.ChangeExtension(file, ".xml");
                if (!File.Exists(xml)) continue;

                if (seen.Add(Path.GetFullPath(xml))) list.Add(xml);
            }
            catch
            {
                // 个别程序集 Location 可能抛错（动态/单文件加载等），跳过即可
            }
        }

        return list;
    }
}
