using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.Extensions;
using NewLife.Cube.ViewModels;
using NewLife.Data;
using NewLife.Log;
using NewLife.Serialization;
using NewLife.Web;
using XCode;
using XCode.Configuration;
using XCode.Membership;

namespace NewLife.Cube;

/// <summary>只读实体控制器基类</summary>
/// <typeparam name="TEntity"></typeparam>
public partial class ReadOnlyEntityController<TEntity> : ControllerBaseX where TEntity : Entity<TEntity>, new()
{
    #region 构造
    /// <summary>动作执行前</summary>
    /// <param name="filterContext"></param>
    public override void OnActionExecuting(Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext filterContext)
    {
        // 多选框强制使用Form提交数据，未选中时不会提交数据，但也要强行覆盖Url参数
        if (Request.HasFormContentType)
        {
            if (filterContext.ActionArguments.TryGetValue("p", out var aa) && aa is Pager p)
            {
                foreach (var item in OnGetFields(ViewKinds.Search, null))
                {
                    if (item is SearchField sf && sf.Multiple)
                    {
                        p[sf.Name] = Request.Form.TryGetValue(sf.Name, out var vs) ? (String)vs : null;
                        //// 以下写法，Form没有数据时，也会返回空字符串，而不是null
                        //p[sf.Name] = Request.Form[sf.Name];
                    }
                }
            }
        }

        base.OnActionExecuting(filterContext);
    }
    #endregion

    #region 默认Action
    /// <summary>多行数据列表</summary>
    /// <returns></returns>
    [EntityAuthorize(PermissionFlags.Detail)]
    [DisplayName("{type}管理")]
    [HttpGet("/[area]/[controller]")]
    public virtual ApiListResponse<TEntity> Index()
    {
        var p = new Pager(WebHelper.Params)
        {
            // 需要总记录数来分页
            RetrieveTotalCount = true
        };

        var list = SearchData(p);
        //return list.ToOkApiResponse().WithList(p); 
        return new ApiListResponse<TEntity>
        {
            Data = list.ToList(),
            Page = p.ToModel(),
            Stat = (TEntity)p.State,
            TraceId = DefaultSpan.Current?.TraceId,
        };
    }

    /// <summary>查看单行数据</summary>
    /// <param name="id">主键。可能为空（表示添加），所以用字符串而不是整数</param>
    /// <returns></returns>
    [EntityAuthorize(PermissionFlags.Detail)]
    [DisplayName("查看{type}")]
    [HttpGet]
    public virtual ApiResponse<TEntity> Detail([Required] String id)
    {
        var entity = FindData(id);
        if (entity == null || (entity as IEntity).IsNullKey) throw new XException("要查看的数据[{0}]不存在！", id);

        // 验证数据权限
        Valid(entity, DataObjectMethodType.Select, false);

        //return entity.ToOkApiResponse();
        return new ApiResponse<TEntity> { Data = entity };
    }
    #endregion

    #region 列表字段和表单字段
    /// <summary>获取字段信息。支持用户重载并根据上下文定制界面</summary>
    /// <param name="kind">字段类型：1-列表List、2-详情Detail、3-添加AddForm、4-编辑EditForm、5-搜索Search</param>
    /// <returns></returns>
    [AllowAnonymous]
    [HttpGet]
    public virtual List<DataField> GetFields(ViewKinds kind) => OnGetFields(kind, null);
    #endregion

    #region 图表
    /// <summary>获取图表数据。子控制器可重写OnGetChartData来提供图表配置</summary>
    /// <returns></returns>
    [EntityAuthorize(PermissionFlags.Detail)]
    [DisplayName("图表{type}")]
    [HttpGet]
    public virtual Object[] GetChartData()
    {
        var p = new Pager(WebHelper.Params)
        {
            RetrieveTotalCount = false,
            PageSize = 1000,
        };

        var list = SearchData(p);

        return OnGetChartData(list);
    }

    /// <summary>构建图表数据。子控制器重写此方法以返回ECharts配置</summary>
    /// <param name="data">搜索得到的数据列表</param>
    /// <returns>ECharts配置数组，每个元素包含 title/option 等属性。无图表时返回空数组</returns>
    [NonAction]
    protected virtual Object[] OnGetChartData(IEnumerable<TEntity> data) => [];
    #endregion

    #region 导出
    /// <summary>统一导出接口。根据 format 参数分发到不同格式的导出逻辑</summary>
    /// <param name="format">导出格式，支持 excel/csv/json/xml，默认 excel</param>
    /// <returns>文件流</returns>
    [EntityAuthorize(PermissionFlags.Detail)]
    [DisplayName("导出{type}")]
    [HttpGet]
    public virtual IActionResult ExportFile(String format = "excel")
    {
        if (format.IsNullOrEmpty()) format = "excel";

        return format.ToLower() switch
        {
            "excel" or "xlsx" => OnExportExcel(),
            "csv" => OnExportCsv(),
            "json" => OnExportJson(),
            "xml" => OnExportXml(),
            _ => throw new ArgumentOutOfRangeException(nameof(format), $"不支持的导出格式：{format}"),
        };
    }

    /// <summary>导出Excel</summary>
    /// <returns></returns>
    [NonAction]
    protected virtual IActionResult OnExportExcel()
    {
        // 准备需要输出的列
        var fs = new List<FieldItem>();
        foreach (var fi in Factory.AllFields)
        {
            if (Type.GetTypeCode(fi.Type) == TypeCode.Object) continue;
            if (!fi.IsDataObjectField)
            {
                var pi = Factory.EntityType.GetProperty(fi.Name);
                if (pi != null && pi.GetCustomAttribute<XmlIgnoreAttribute>() != null) continue;
            }

            fs.Add(fi);
        }

        // 基本属性与扩展属性对调顺序
        for (var i = 0; i < fs.Count; i++)
        {
            var fi = fs[i];
            if (fi.OriField != null)
            {
                var k = fs.IndexOf(fi.OriField);
                if (k >= 0)
                {
                    fs[i] = fs[k];
                    fs[k] = fi;
                }
            }
        }

        var name = MakeExportFileName(".csv");

        var list = ExportData();

        // WebAPI 版使用 CSV 格式导出，兼容所有平台
        var ms = new MemoryStream();
        ExportCsvToStream(fs, list, ms);
        ms.Position = 0;

        return new FileStreamResult(ms, "text/csv") { FileDownloadName = name };
    }

    /// <summary>导出Csv</summary>
    /// <returns></returns>
    [NonAction]
    protected virtual IActionResult OnExportCsv()
    {
        var name = MakeExportFileName(".csv");
        var list = ExportData();

        var fs = Factory.Fields.ToList();

        var ms = new MemoryStream();
        ExportCsvToStream(fs, list, ms);
        ms.Position = 0;

        return new FileStreamResult(ms, "text/csv") { FileDownloadName = name };
    }

    /// <summary>导出Json</summary>
    /// <returns></returns>
    [NonAction]
    protected virtual IActionResult OnExportJson()
    {
        var name = MakeExportFileName(".json");
        var list = ExportData().ToList();

        var json = list.ToJson(true);
        return new FileContentResult(json.GetBytes(), "application/json") { FileDownloadName = name };
    }

    /// <summary>导出Xml</summary>
    /// <returns></returns>
    [NonAction]
    protected virtual IActionResult OnExportXml()
    {
        var name = MakeExportFileName(".xml");
        var list = ExportData().ToList();

        var xml = list.ToJson(true);
        return new FileContentResult(xml.GetBytes(), "application/xml") { FileDownloadName = name };
    }

    /// <summary>将数据写入CSV流</summary>
    /// <param name="fields">字段列表</param>
    /// <param name="data">数据</param>
    /// <param name="stream">目标流</param>
    [NonAction]
    protected void ExportCsvToStream(IList<FieldItem> fields, IEnumerable<TEntity> data, Stream stream)
    {
        using var writer = new StreamWriter(stream, System.Text.Encoding.UTF8, 1024, leaveOpen: true);
        // 表头
        writer.WriteLine(String.Join(",", fields.Select(f => $"\"{f.DisplayName ?? f.Name}\"")));
        // 数据行
        foreach (var entity in data)
        {
            var values = fields.Select(f =>
            {
                var val = entity[f.Name]?.ToString() ?? "";
                // CSV 规范：含逗号/引号/换行的字段用引号包裹
                if (val.Contains(',') || val.Contains('"') || val.Contains('\n'))
                    val = $"\"{val.Replace("\"", "\"\"")}\"";
                return val;
            });
            writer.WriteLine(String.Join(",", values));
        }
    }

    /// <summary>生成导出文件名</summary>
    /// <param name="ext">扩展名，如 .xlsx</param>
    /// <returns></returns>
    [NonAction]
    protected virtual String MakeExportFileName(String ext)
    {
        var name = GetType().GetDisplayName();
        if (name.IsNullOrEmpty()) name = Factory.EntityType.GetDisplayName();
        if (name.IsNullOrEmpty()) name = Factory.Table.DataTable.DisplayName;
        if (name.IsNullOrEmpty()) name = GetType().Name.TrimEnd("Controller");
        if (!ext.IsNullOrEmpty()) ext = ext.EnsureStart(".");

        return $"{name}_{DateTime.Now:yyyyMMddHHmmss}{ext}";
    }
    #endregion
}