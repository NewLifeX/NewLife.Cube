using System.ComponentModel;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using NewLife.Cube.ViewModels;
using NewLife.Reflection;
using NewLife.Web;
using XCode;
using XCode.Membership;

namespace NewLife.Cube;

/// <summary>实体树控制器基类</summary>
/// <typeparam name="TEntity"></typeparam>
public class EntityTreeController<TEntity> : EntityTreeController<TEntity, TEntity> where TEntity : EntityTree<TEntity>, new() { }

/// <summary>实体树控制器基类</summary>
/// <typeparam name="TEntity"></typeparam>
/// <typeparam name="TModel"></typeparam>
public class EntityTreeController<TEntity, TModel> : EntityController<TEntity, TModel> where TEntity : EntityTree<TEntity>, new()
{
    static EntityTreeController()
    {
        var type = typeof(TEntity);
        var all = Entity<TEntity>.Meta.AllFields;
        //var list = new List<FieldItem>();
        var list = ListFields;
        var set = type.GetValue("Setting") as IEntityTreeSetting;
        var k = 0;
        var names = new String[] { set.Key, "TreeNodeName" };
        foreach (var item in names)
        {
            var fi = all.FirstOrDefault(e => e.Name.EqualIgnoreCase(item));
            if (fi != null)
            {
                list.RemoveField(item);
                list.Insert(k++, list.Create(fi));
            }
        }

        foreach (var item in all)
        {
            if (set != null && item.Name.EqualIgnoreCase(set.Name, set.Parent))
            {
                list.RemoveField(item.Name);
                continue;
            }

            var pi = type.GetProperty(item.Name);
            if (pi == null || pi.GetCustomAttribute<DisplayNameAttribute>() == null)
            {
                list.RemoveField(item.Name);
                continue;
            }

            //if (!list.Contains(item)) list.Insert(k++, item);
        }

        //ListFields.Clear();
        //ListFields.AddRange(list);
    }

    /// <summary>验证实体对象</summary>
    /// <param name="entity"></param>
    /// <param name="type"></param>
    /// <param name="post"></param>
    /// <returns></returns>
    protected override Boolean Valid(TEntity entity, DataObjectMethodType type, Boolean post)
    {
        var rs = base.Valid(entity, type, post);

        // 清空缓存
        if (post) Factory.Session.ClearCache($"{type}-{entity}", true);

        return rs;
    }

    /// <summary>设置字段列表</summary>
    /// <param name="filterContext"></param>
    public override void OnActionExecuting(ActionExecutingContext filterContext)
    {
        ViewBag.Fields = OnGetFields(ViewKinds.List, null);

        base.OnActionExecuting(filterContext);
    }

    /// <summary>实体树配置</summary>
    /// <returns></returns>
    protected IEntityTreeSetting GetSetting()
    {
        //var set = EntityTree<TEntity>.Setting;
        var set = typeof(EntityTree<TEntity>).GetValue("Setting") as IEntityTreeSetting;
        return set;
    }

    /// <summary>列表页视图。子控制器可重载，以传递更多信息给视图，比如修改要显示的列</summary>
    /// <param name="p"></param>
    /// <returns></returns>
    protected override ActionResult IndexView(Pager p)
    {
        var list = SearchData(p);

        // 用于显示的列
        ViewBag.Fields = OnGetFields(ViewKinds.List, list);
        ViewBag.SearchFields = OnGetFields(ViewKinds.Search, list);

        // Json输出
        if (IsJsonRequest) return Json(0, null, list, new { page = p });

        return View("ListTree", list);
    }

    /// <summary>搜索数据集</summary>
    /// <param name="p"></param>
    /// <returns></returns>
    protected override IEnumerable<TEntity> Search(Pager p)
    {
        // 一页显示全部菜单，取自缓存
        if (p.PageSize == 20) p.PageSize = 10000;

        var set = GetSetting();
        if (set != null && !set.Parent.IsNullOrEmpty())
        {
            var pkey = p[set.Parent].ToInt(-1);
            if (pkey >= 0)
                return EntityTree<TEntity>.FindAllChildsByParent(pkey);
        }

        return EntityTree<TEntity>.Root.AllChilds;
    }

    ///// <summary>要导出Xml的对象</summary>
    ///// <returns></returns>
    //protected override Object OnExportXml()
    //{
    //    return EntityTree<TEntity>.Root.Childs;
    //}

    ///// <summary>要导出Json的对象</summary>
    ///// <returns></returns>
    //protected override Object OnExportJson()
    //{
    //    return EntityTree<TEntity>.Root.Childs;
    //}

    /// <summary>上升</summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [DisplayName("上升")]
    [EntityAuthorize(PermissionFlags.Update)]
    public ActionResult Up(Int32 id)
    {
        var menu = FindByID(id);

        if (Valid(menu, DataObjectMethodType.Update, true))
            menu.Up();

        var set = GetSetting();
        if (set != null && !set.Parent.IsNullOrEmpty())
        {
            var p = WebHelper.Params;
            var pkey = p[set.Parent].ToInt(-1);
            if (pkey >= 0)
            {
                var dic = new RouteValueDictionary
                {
                    [set.Parent] = pkey
                };
                return RedirectToAction("Index", dic);
            }
        }

        return RedirectToAction("Index");
    }

    /// <summary>下降</summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [DisplayName("下降")]
    [EntityAuthorize(PermissionFlags.Update)]
    public ActionResult Down(Int32 id)
    {
        var menu = FindByID(id);

        if (Valid(menu, DataObjectMethodType.Update, true))
            menu.Down();

        var set = GetSetting();
        if (set != null && !set.Parent.IsNullOrEmpty())
        {
            var p = WebHelper.Params;
            var pkey = p[set.Parent].ToInt(-1);
            if (pkey >= 0)
            {
                var dic = new RouteValueDictionary
                {
                    [set.Parent] = pkey
                };
                return RedirectToAction("Index", dic);
            }
        }

        return RedirectToAction("Index");
    }

    /// <summary>根据ID查找节点</summary>
    /// <param name="id"></param>
    /// <returns></returns>
    protected static TEntity FindByID(Int32 id)
    {
        var key = EntityTree<TEntity>.Meta.Unique.Name;
        return EntityTree<TEntity>.Meta.Cache.Find(e => (Int32)e[key] == id);
    }
}