using System.Collections;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewLife.Caching;
using NewLife.Cube.AI;
using NewLife.Cube.Entity;
using NewLife.Cube.Extensions;
using NewLife.Cube.ViewModels;
using NewLife.Log;
using NewLife.Serialization;
using NewLife.Web;
using XCode;
using XCode.Configuration;
using XCode.Membership;

namespace NewLife.Cube;

/// <summary>只读实体控制器基类</summary>
/// <typeparam name="TEntity"></typeparam>
public partial class ReadOnlyEntityController<TEntity> : ControllerBaseX, IEntityAiContext where TEntity : Entity<TEntity>, new()
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
    [HttpGet("api/[area]/[controller]")]
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
    /// <summary>获取页面元数据。包含页面设置以及列表/表单/搜索字段</summary>
    /// <returns></returns>
    [AllowAnonymous]
    [HttpGet]
    public virtual ApiResponse<Object> GetPage()
    {
        // 主时间字段信息（OSC-0016）：供 SPA 搜索面板渲染主时间范围控件；无 MasterTime 时两键为 null
        var masterTime = Factory.MasterTime;
        var setting = new
        {
            PageSetting.NavView,
            PageSetting.EnableNavbar,
            PageSetting.EnableToolbar,
            PageSetting.EnableAdd,
            PageSetting.EnableKey,
            PageSetting.EnableSelect,
            PageSetting.EnableFooter,
            PageSetting.IsReadOnly,
            PageSetting.EnableTableDoubleClick,
            PageSetting.OrderByKey,
            PageSetting.DoubleDelete,
            masterTimeName = masterTime?.Name,
            masterTimeDisplayName = masterTime?.DisplayName,
        };

        var list = PrepareFieldsForApi(OnGetFields(ViewKinds.List, null));
        var addForm = PrepareFieldsForApi(OnGetFields(ViewKinds.AddForm, null));
        var editForm = PrepareFieldsForApi(OnGetFields(ViewKinds.EditForm, null));
        var detail = PrepareFieldsForApi(OnGetFields(ViewKinds.Detail, null));
        var search = PrepareFieldsForApi(OnGetFields(ViewKinds.Search, null));
        // Map 外键字段候选补全：SearchBuilder 仅输出表字段（Map 关联多在扩展属性上），外键字段（如 User.DepartmentID）无候选
        FixSearchMapCandidates(search);

        var data = new
        {
            setting,
            list,
            addForm,
            editForm,
            detail,
            search,
        };

        return new ApiResponse<Object>
        {
            Data = data,
            TraceId = DefaultSpan.Current?.TraceId,
        };
    }

    /// <summary>获取字段信息。支持用户重载并根据上下文定制界面</summary>
    /// <param name="kind">字段类型：1-列表List、2-详情Detail、3-添加AddForm、4-编辑EditForm、5-搜索Search</param>
    /// <returns></returns>
    [AllowAnonymous]
    [HttpGet]
    public virtual List<DataField> GetFields(ViewKinds kind)
    {
        var fields = PrepareFieldsForApi(OnGetFields(kind, null)).ToList();
        // 搜索字段同样补全 Map 外键候选，与 GetPage 保持一致
        if (kind == ViewKinds.Search) FixSearchMapCandidates(fields);
        return fields;
    }

    /// <summary>补全搜索字段的 Map 外键候选。SearchBuilder 仅输出表字段，而 Map 关联多在扩展属性上
    /// （如 User.DepartmentID 的 [Map] 标在 User.Department 扩展属性），导致外键字段无候选、前端渲染数字框。
    /// 这里从 Factory.AllFields 找 MapField 匹配的扩展字段补全：小表（≤MaxDropDownList）内联 DataSourceMap，大表注册 Entity. 值集；手工已设 LovCode/DataSourceMap 优先不覆盖。</summary>
    /// <param name="search">GetPage/GetFields 返回的搜索字段列表</param>
    protected virtual void FixSearchMapCandidates(IList<DataField> search)
    {
        if (search == null || search.Count == 0) return;

        // MapField(表字段名，如 DepartmentID) → 扩展字段（带 Map 的实体属性，如 User.Department）
        var mapFields = Factory.AllFields
            .Where(e => e.Map != null && !e.Map.Name.IsNullOrEmpty())
            .GroupBy(e => e.Map.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        if (mapFields.Count == 0) return;

        foreach (var df in search)
        {
            if (df is not SearchField sf) continue;
            if (!mapFields.TryGetValue(sf.Name, out var ext)) continue;

            var provider = ext.Map.Provider;
            if (provider == null || provider.EntityType == null) continue;

            // 手工已设 LovCode 且值集已注册 → 优先保留；值集未配置（如 UserController 手动设的 "Role" 但未注册）→ 用 Map 实体候选覆盖
            if (!sf.LovCode.IsNullOrEmpty())
            {
                // 值集是否已注册走 MemoryCache 60s（"1"=已注册，"0"=未注册），GetPage 高频接口避免逐字段查库
                var lovKey = "LovRegistered:" + sf.LovCode;
                var mark = MemoryCache.Instance.Get<String>(lovKey);
                if (mark.IsNullOrEmpty())
                {
                    var lov = LovDefinition.Find(LovDefinition._.LovCode == sf.LovCode);
                    mark = lov != null ? "1" : "0";
                    MemoryCache.Instance.Set(lovKey, mark, 60);
                }
                if (mark == "1") continue;
            }
            // 已内联 DataSourceMap → 保留
            if (sf.DataSourceMap != null && sf.DataSourceMap.Count > 0) continue;

            var entityType = provider.EntityType;
            // 行数判断走 MemoryCache 60s，避免每个请求都 Count
            var cacheKey = "LovMapCount:" + entityType.FullName;
            var count = MemoryCache.Instance.Get<Int32>(cacheKey);
            if (count <= 0)
            {
                var fact = EntityFactory.CreateFactory(entityType);
                count = fact.Session.Count;
                MemoryCache.Instance.Set(cacheKey, count, 60);
            }

            if (count <= CubeSetting.Current.MaxDropDownList)
            {
                // 小表：内联数据源，前端渲染本地下拉
                var dic = provider.GetDataSource();
                if (dic == null) continue;
                var map2 = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
                foreach (var de in dic)
                {
                    if (de.Key == null) continue;
                    map2[de.Key + ""] = de.Value + "";
                }
                if (map2.Count > 0) sf.DataSourceMap = map2;
            }
            else
            {
                // 大表：注册 Entity. 值集，前端 LovSelect 远程搜索
                sf.LovCode = "Entity." + entityType.FullName;
            }
        }
    }

    /// <summary>物化字段数据源字典，供 SPA 列表徽章/表单下拉复用，避免反复拉值集</summary>
    [NonAction]
    protected virtual IList<DataField> PrepareFieldsForApi(IList<DataField> fields)
    {
        if (fields == null) return fields;
        foreach (var df in fields) df?.PrepareForApi();
        return fields;
    }
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
        if (name.IsNullOrEmpty()) name = GetType().Name.TrimSuffix("Controller");
        if (!ext.IsNullOrEmpty()) ext = ext.EnsureStart(".");

        return $"{name}_{DateTime.Now:yyyyMMddHHmmss}{ext}";
    }
    #endregion
}