using System.ComponentModel;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using NewLife.Cube.Common;
using NewLife.Cube.Extensions;
using NewLife.Cube.ViewModels;
using NewLife.Reflection;
using NewLife.Web;
using XCode.Membership;

namespace NewLife.Cube;

/// <summary>对象控制器</summary>
public abstract class ObjectController<TObject> : ControllerBaseX
{
    /// <summary>要展现和修改的对象</summary>
    protected abstract TObject Value { get; set; }

    /// <summary>菜单顺序。扫描时会反射读取</summary>
    protected static Int32 MenuOrder { get; set; }

    /// <summary>实例化</summary>
    public ObjectController() => PageSetting.EnableNavbar = false;

    ///// <summary>动作执行前</summary>
    ///// <param name="filterContext"></param>
    //public override void OnActionExecuting(Remoting.ControllerContext filterContext)
    //{
    //    base.OnActionExecuting(filterContext);

    //    // 显示名和描述
    //    var name = GetType().GetDisplayName() ?? typeof(TObject).GetDisplayName() ?? typeof(TObject).Name;
    //    var des = GetType().GetDescription() ?? typeof(TObject).GetDescription();

    //    ViewBag.Title = name;
    //    ViewBag.HeaderTitle = name;

    //    var txt = "";
    //    if (txt.IsNullOrEmpty()) txt = Menu?.Remark;
    //    if (txt.IsNullOrEmpty()) txt = des;
    //    ViewBag.HeaderContent = txt;
    //}

    ///// <summary>执行后</summary>
    ///// <param name="filterContext"></param>
    //public override void OnActionExecuted(Remoting.ControllerContext filterContext)
    //{
    //    base.OnActionExecuted(filterContext);

    //    var title = ViewBag.Title + "";
    //    HttpContext.Items["Title"] = title;
    //}

    /// <summary>显示对象</summary>
    /// <param name="formatType">0-小驼峰，1-小写，2-保持默认</param>
    /// <returns></returns>
    [EntityAuthorize(PermissionFlags.Detail)]
    public ActionResult Index(FormatType formatType = FormatType.CamelCase)
    {
        var list = GetMembers(Value?.GetType());

        list = list.Select(e => e.Clone()).ToList();
        foreach (var item in list)
        {
            item.Name = FormatHelper.FormatName(item.Name, formatType);
            //item.FormatType = formatType;
        }
        var dic = list
            .GroupBy(e => e.Category + "")
            .ToDictionary(e => e.Key, e => e.ToList());

        //model.Properties = dic;

        return Ok(data: new { Value, Properties = dic });
    }

    /// <summary>保存对象</summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    //[HttpPost]
    //[DisplayName("修改")]
    [EntityAuthorize(PermissionFlags.Update)]
    public ActionResult Update(TObject obj)
    {
        WriteLog(obj, UserHost);

        // 反射处理内部复杂成员
        var keys = WebHelper.Params.Keys;
        foreach (var item in obj.GetType().GetProperties(true))
        {
            if (Type.GetTypeCode(item.PropertyType) == TypeCode.Object)
            {
                var pv = obj.GetValue(item);
                foreach (var pi in item.PropertyType.GetProperties(true))
                {
                    if (keys.Contains(pi.Name))
                    {
                        var v = (Object)Request.GetRequestValue(pi.Name);
                        if (pi.PropertyType == typeof(Boolean)) v = GetBool(pi.Name);

                        pv.SetValue(pi, v);
                    }
                }
            }
        }

        Value = obj;

        if (IsJsonRequest)
            return Ok("保存成功");
        else
            return Redirect("Index");
    }

    private Boolean GetBool(String name)
    {
        var v = Request.GetRequestValue(name);
        if (v.IsNullOrEmpty()) return false;

        v = v.Split(",")[0];

        if (!v.EqualIgnoreCase("true", "false")) throw new XException("非法布尔值Request[{0}]={1}", name, v);

        return v.ToBoolean();
    }

    /// <summary>写日志</summary>
    /// <param name="obj"></param>
    /// <param name="ip"></param>
    protected virtual void WriteLog(TObject obj, String ip = null)
    {
        // 构造修改日志
        var sb = new StringBuilder();
        var cfg = Value;
        foreach (var pi in obj.GetType().GetProperties(true))
        {
            if (!pi.CanWrite) continue;

            var v1 = obj.GetValue(pi);
            var v2 = cfg.GetValue(pi);
            if (!Equals(v1, v2) && (pi.PropertyType != typeof(String) || v1 + "" != v2 + ""))
            {
                if (sb.Length > 0) sb.Append(", ");

                var name = pi.GetDisplayName();
                if (name.IsNullOrEmpty()) name = pi.Name;
                sb.AppendFormat("{0}:{1}=>{2}", name, v2, v1);
            }
        }
        WriteLog("修改", true, sb.ToString());
    }

    private static readonly Dictionary<Type, IList<DataField>> _cache = new();
    /// <summary>获取指定类型的成员集合（带缓存）</summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static IList<DataField> GetMembers(Type type)
    {
        if (_cache.TryGetValue(type, out var rs)) return rs;

        var pis = type.GetProperties(true);
        var list = new List<DataField>();
        foreach (var pi in pis)
        {
            var cat = pi.GetCustomAttribute<CategoryAttribute>();
            var category = cat?.Category ?? "";
            var dis = pi.GetDisplayName();
            var des = pi.GetDescription();
            if (dis.IsNullOrEmpty() && !des.IsNullOrEmpty()) { dis = des; des = null; }
            if (!dis.IsNullOrEmpty() && des.IsNullOrEmpty() && dis.Contains("。"))
            {
                des = dis.Substring("。");
                dis = dis.Substring(null, "。");
            }

            var df = new DataField
            {
                Name = pi.Name,
                DisplayName = dis ?? pi.Name,
                Description = des,
                Type = pi.PropertyType,
                //DataType = pi.PropertyType.FullName,
                Category = category,
            };

            list.Add(df);
        }

        _cache[type] = list;

        return list;
    }
}