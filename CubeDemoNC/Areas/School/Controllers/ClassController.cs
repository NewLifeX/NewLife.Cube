using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using NewLife;
using NewLife.Cube;
using NewLife.Cube.ViewModels;
using NewLife.Log;
using NewLife.School.Entity;
using NewLife.Web;
using XCode;
using XCode.Membership;

namespace CubeDemo.Areas.School.Controllers;

[SchoolArea]
[DisplayName("班级")]
[Menu(0, true, Mode = MenuModes.Admin | MenuModes.Tenant)]
public class ClassController : EntityController<Class, ClassModel>
{
    private readonly ITracer _tracer;

    static ClassController()
    {
        {
            var df = ListFields.AddListField("test", null, "ID");
            df.DisplayName = "AJAX操作请求";
            df.Url = "/School/Class/Hello";
            df.DataAction = "action";
            df.Title= "Title";
        }

        {
            var df = ListFields.AddListField("test1", null, "ID");
            df.DisplayName = "当前页打开";
            df.Url = "/School/Class";
            df.Target = TargetEnum._self + "";
        }

        {
            var df = ListFields.AddListField("test1", null, "ID");
            df.DisplayName = "浏览器多标签页打开";
            df.Url = "/School/Student";
            df.Target = TargetEnum._blank + "";
            df.Title = "Title";
        }
    }

    public ClassController(IServiceProvider provider)
    {
        PageSetting.EnableTableDoubleClick = true;

        _tracer = provider?.GetService<ITracer>();
    }

    public IActionResult Hello()
    {
        return Json(new
        {
            code = "200",
            data = "ok"
        });
    }

    protected override IEnumerable<Class> Search(Pager p)
    {
        using var span = _tracer?.NewSpan(nameof(Search), p);

        var id = p["Id"].ToInt(-1);
        if (id > 0)
        {
            var entity = Class.FindById(id);
            return entity == null ? new List<Class>() : new List<Class> { entity };
        }

        var start = p["dtStart"].ToDateTime();
        var end = p["dtEnd"].ToDateTime();

        var exp = new WhereExpression();
        // var  list = Class.Search(start, end, p["Q"], p);
        var list = Class.FindAll(exp, p);
        return list;
    }

    /// <summary>获取字段信息</summary>
    /// <param name="kind"></param>
    /// <param name="model"></param>
    /// <returns></returns>
    protected override FieldCollection OnGetFields(ViewKinds kind, Object model)
    {
        var rs = base.OnGetFields(kind, model);

        if (TenantContext.CurrentId > 0)
        {
            switch (kind)
            {
                case ViewKinds.Detail:
                case ViewKinds.AddForm:
                case ViewKinds.EditForm:
                    rs.RemoveField("TenantId", "TenantName");
                    break;
                default:
                    break;
            }
        }

        return rs;
    }

    /// <summary>验证数据</summary>
    /// <param name="entity"></param>
    /// <param name="type"></param>
    /// <param name="post"></param>
    /// <returns></returns>
    protected override Boolean Valid(Class entity, DataObjectMethodType type, Boolean post)
    {
        if (/*!post &&*/ type == DataObjectMethodType.Insert)
        {
            if (entity.TenantId == 0) entity.TenantId = TenantContext.CurrentId;
        }

        return base.Valid(entity, type, post);
    }
}