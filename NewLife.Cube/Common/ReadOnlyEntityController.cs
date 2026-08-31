using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.AI;
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
    [HttpGet("/api/[area]/[controller]")]
    public virtual ApiListResponse<TEntity> Index()
    {
        var p = new Pager(WebHelper.Params)
        {
            // 需要总记录数来分页；海量数据可关闭，免查总数后TotalCount恒为0，由客户端按PageIndex翻页
            RetrieveTotalCount = PageSetting.EnableTotalCount
        };

        var list = SearchData(p).ToList();

        // 复刻 MVC ListField 按行计算值：虚拟字段/GetValue 字段写入实体扩展，随行 JSON 内联输出
        OnFillListValues(list);

        //return list.ToOkApiResponse().WithList(p); 
        return new ApiListResponse<TEntity>
        {
            Data = list,
            Page = p.ToModel(),
            Stat = (TEntity)p.State,
            TraceId = DefaultSpan.Current?.TraceId,
        };
    }

    /// <summary>填充列表字段计算值。复刻 MVC 版 ListField 按行计算单元格值的能力</summary>
    /// <remarks>
    /// MVC 版在视图渲染时调用 ListField.GetLink/GetLinkName 按行计算单元格值；API 版无视图，
    /// 在 Index 序列化前统一计算并写入实体扩展字典（<see cref="IExtend.Items"/>），
    /// 由 JsonWriter 内联到行 JSON 顶层，前端字段元数据驱动的通用渲染即可显示，无需修改前端代码。
    /// 仅处理虚拟字段（Field 为空，如 AddListField 创建的 AvatarImage）或设置了 GetValue 委托的字段，
    /// 普通列不参与，避免额外开销。
    /// </remarks>
    /// <param name="list">数据列表</param>
    /// <param name="fields">列表字段。为空时使用当前列表字段集合（OnGetFields）</param>
    protected virtual void OnFillListValues(IEnumerable<TEntity> list, IList<DataField>? fields = null)
    {
        fields ??= OnGetFields(ViewKinds.List, null);

        // 只收集需要计算值的字段：虚拟字段（非实体列）或设置了 GetValue 委托的字段
        var lfs = fields.OfType<ListField>().Where(e => e.Field == null || e.GetValue != null).ToArray();
        if (lfs.Length == 0) return;

        foreach (var entity in list)
        {
            if (entity is not IExtend ext) continue;

            foreach (var df in lfs)
            {
                // 可见性控制：DataVisible 为 false 时不输出该列值
                if (df.DataVisible != null && !df.DataVisible(entity)) continue;

                var value = df.GetValue?.Invoke(entity) ?? entity[df.Name];
                if (value != null) ext.Items[df.Name] = value;
            }
        }
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
        };

        var list = OnGetFields(ViewKinds.List, null);
        // 全部可用列表字段（应用用户列配置前，供前端列设置面板使用）
        var allList = OnGetFields(ViewKinds.List, null);
        var addForm = OnGetFields(ViewKinds.AddForm, null);
        var editForm = OnGetFields(ViewKinds.EditForm, null);
        var detail = OnGetFields(ViewKinds.Detail, null);
        var search = OnGetFields(ViewKinds.Search, null);

        // 应用当前用户列配置：重排列顺序、标记隐藏字段 Visible=false（React 皮肤列设置）
        ApplyColumnConfig(list);

        var data = new
        {
            setting,
            list,
            allList,
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
    public virtual List<DataField> GetFields(ViewKinds kind) => OnGetFields(kind, null);
    #endregion

    #region 列配置
    /// <summary>应用当前用户的列配置（Parameter 表 Page-React 分类）。重排列顺序、标记隐藏字段 Visible=false</summary>
    /// <param name="list">列表字段集合（就地修改）</param>
    protected virtual void ApplyColumnConfig(List<DataField> list)
    {
        // 页面路径：优先当前菜单 URL（如 /Cube/App）；菜单不可用时从路由推导（/api/Cube/App/GetPage → /Cube/App）
        var page = Menu?.Url;
        if (page.IsNullOrEmpty())
        {
            var area = RouteData.Values["area"] + "";
            var controller = RouteData.Values["controller"] + "";
            page = area.IsNullOrEmpty() ? $"/{controller}" : $"/{area}/{controller}";
        }
        if (page.IsNullOrEmpty() || CurrentUser == null || list == null || list.Count == 0) return;

        var cfg = LoadColumnConfig("Page-React", page, CurrentUser.ID);
        if (cfg == null || cfg.Count == 0) return;

        // 列顺序：listOrder 字段名数组。未列出的字段保持原顺序排在后面（新增字段自动可见）
        if (cfg.TryGetValue("listOrder", out var orderObj) && orderObj is System.Collections.IList order)
        {
            var names = order.Cast<Object>().Select(e => e + "").ToList();
            var dic = new Dictionary<String, DataField>(StringComparer.OrdinalIgnoreCase);
            foreach (var f in list) dic[f.Name] = f;

            var newList = new List<DataField>(list.Count);
            foreach (var name in names)
            {
                if (dic.TryGetValue(name, out var f))
                {
                    newList.Add(f);
                    dic.Remove(name);
                }
            }
            foreach (var f in list)
            {
                if (dic.ContainsKey(f.Name)) newList.Add(f);
            }

            list.Clear();
            list.AddRange(newList);
        }

        // 隐藏列：listHidden 字段名数组 → 从列表移除（前端不再渲染；allList 仍含全部供列设置面板）。
        // 注意不能仅标记 Visible=false——DataField 序列化只输出 visible=true，false 不发到前端无法区分
        if (cfg.TryGetValue("listHidden", out var hiddenObj) && hiddenObj is System.Collections.IList hidden)
        {
            var hs = new HashSet<String>(hidden.Cast<Object>().Select(e => e + ""), StringComparer.OrdinalIgnoreCase);
            list.RemoveAll(f => hs.Contains(f.Name));
        }
    }

    /// <summary>加载指定页面列配置。用户级优先，全局兜底</summary>
    /// <param name="category">配置分类，如 Page-React</param>
    /// <param name="page">页面路径，如 /Cube/Area</param>
    /// <param name="userId">当前用户编号</param>
    /// <returns>配置字典</returns>
    protected virtual IDictionary<String, Object> LoadColumnConfig(String category, String page, Int32 userId)
    {
        var p = Parameter.Find(Parameter._.Category == category & Parameter._.Name == page & Parameter._.UserID == userId)
            ?? Parameter.Find(Parameter._.Category == category & Parameter._.Name == page & Parameter._.UserID == 0);
        if (p == null) return null;

        var value = !p.Value.IsNullOrEmpty() ? p.Value : p.LongValue;
        if (value.IsNullOrEmpty()) return null;

        return value.DecodeJson() as IDictionary<String, Object>;
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