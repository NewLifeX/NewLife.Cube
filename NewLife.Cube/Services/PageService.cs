using System;
using NewLife.Cube.Modules;
using NewLife.Reflection;
using NewLife.Serialization;
using XCode.Membership;
using NewLife.Cube.ViewModels;

namespace NewLife.Cube.Services;

/// <summary>页面服务</summary>
public class PageService
{
    private readonly ModuleManager _moduleManager;
    /// <summary>实例化页面服务</summary>
    /// <param name="moduleManager"></param>
    public PageService(ModuleManager moduleManager)
    {
        _moduleManager = moduleManager;
    }

    /// <summary>获取页面配置信息。列表页、表单页所需显示字段，以及各字段显示方式</summary>
    /// <param name="kind">种类。用于区分不同的前端类型，如Vue/Antd/QuickVue</param>
    /// <param name="page">页面路径。如/admin/user</param>
    /// <param name="userId">当前用户</param>
    /// <returns></returns>
    public Object GetPageConfig(String kind, String page, Int32 userId)
    {
        if (page.IsNullOrEmpty()) throw new ArgumentNullException(nameof(page));

        /*
             * 页面配置生成流程：
             * 1，根据页面获取菜单，查询得到控制器
             * 2，从控制器中加载字段信息，加入结果集
             * 3，从配置参数中加载当前用户在该页面上的字段信息，加入结果集
             * 4，从配置参数中加载全局在该页面上的字段信息，加入结果集
             * 5，适配器处理结果集，返回最终结果
             */

        if (kind.IsNullOrEmpty()) kind = "Default";
        var adapter = _moduleManager.GetAdapter(kind) ??
            throw new ArgumentOutOfRangeException(nameof(kind), $"未找到适配器[{kind}]");

        var dic = new Dictionary<String, Object>();
        var fdic = new Dictionary<ViewKinds, FieldCollection>();

        // 从页面控制器中加载字段信息
        var mf = ManageProvider.Menu;
        var menu = mf.FindByFullName(page.Trim('/').Replace('/', '.'));
        menu ??= mf.FindByUrl(page);
        menu ??= mf.FindByUrl("~" + page.EnsureStart("/"));
        if (menu != null && !menu.FullName.IsNullOrEmpty())
        {
            var ss = menu.FullName.Split(".");
            var type = !ss[^1].EndsWith("Controller") ? ss.Take(ss.Length - 1).Join(".").GetTypeEx() : menu.FullName.GetTypeEx();
            if (type != null)
            {
                fdic[ViewKinds.List] = type.GetValue("ListFields") as FieldCollection;
                fdic[ViewKinds.AddForm] = type.GetValue("AddFormFields") as FieldCollection;
                fdic[ViewKinds.EditForm] = type.GetValue("EditFormFields") as FieldCollection;
                fdic[ViewKinds.Detail] = type.GetValue("DetailFields") as FieldCollection;
                fdic[ViewKinds.Search] = type.GetValue("SearchFields") as FieldCollection;
            }
        }

        var ps = Parameter.FindAllByCategoryAndName($"Page-{kind}", page);

        // 加载用户配置
        if (userId > 0)
        {
            var dic2 = LoadConfig(userId, ps);
            if (dic2 != null && dic2.Count > 0) dic.Merge(dic2);
        }

        // 加载全局配置
        {
            var dic2 = LoadConfig(0, ps);
            if (dic2 != null && dic2.Count > 0) dic.Merge(dic2);
        }

        var rs = adapter.Encode(dic, fdic);

        return rs;
    }

    private IDictionary<String, Object> LoadConfig(Int32 userId, IList<Parameter> ps)
    {
        var p = ps.FirstOrDefault(e => e.UserID == userId);
        if (p != null)
        {
            var value = !p.Value.IsNullOrEmpty() ? p.Value : p.LongValue;
            if (!value.IsNullOrEmpty())
            {
                var dic2 = value.DecodeJson();
                if (dic2 != null && dic2.Count > 0)
                    return dic2;
            }
        }

        return null;
    }

    /// <summary>保存页面配置信息。需要自定义部分字段，其它信息由字段列表和适配器动态生成</summary>
    /// <param name="kind"></param>
    /// <param name="page"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public Object SetPageConfig(String kind, String page, IDictionary<String, Object> value)
    {
        if (page.IsNullOrEmpty()) throw new ArgumentNullException(nameof(page));

        if (kind.IsNullOrEmpty()) kind = "Default";
        var adapter = _moduleManager.GetAdapter(kind) ??
            throw new ArgumentOutOfRangeException(nameof(kind), $"未找到适配器[{kind}]");

        var rs = adapter.Decode(value);

        //todo 需要一个机制，识别是用户级配置还是全局配置，这里暂时作为全局配置
        var p = Parameter.GetOrAdd(0, $"Page-{kind}", page);
        p.LongValue = rs as String ?? rs.ToJson(true);
        p.Update();

        return p.ID;
    }
}
