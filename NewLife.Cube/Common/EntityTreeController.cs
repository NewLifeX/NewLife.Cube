using System.ComponentModel;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
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
/// <typeparam name="TModel">数据模型，用于接口数据传输</typeparam>
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

    /// <summary>实体树的数据来自缓存</summary>
    /// <param name="p"></param>
    /// <returns></returns>
    protected override IEnumerable<TEntity> Search(Pager p)
    {
        // 一页显示全部菜单，取自缓存
        p.PageSize = 10000;

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
    [HttpPatch]
    public ActionResult Up(Int32 id)
    {
        var menu = FindByID(id);

        if (Valid(menu, DataObjectMethodType.Update, true))
            menu.Up();

        return RedirectToAction("Index");
    }

    /// <summary>下降</summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [DisplayName("下降")]
    [EntityAuthorize(PermissionFlags.Update)]
    [HttpPatch]
    public ActionResult Down(Int32 id)
    {
        var menu = FindByID(id);

        if (Valid(menu, DataObjectMethodType.Update, true))
            menu.Down();

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