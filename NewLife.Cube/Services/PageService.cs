using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.Modules;

namespace NewLife.Cube.Services;

/// <summary>页面服务</summary>
public class PageService
{
    private readonly ModuleManager _moduleManager;
    public PageService(ModuleManager moduleManager)
    {
        _moduleManager = moduleManager;
    }

    /// <summary>获取页面配置信息。列表页、表单页所需显示字段，以及各字段显示方式</summary>
    /// <param name="kind"></param>
    /// <param name="page"></param>
    /// <returns></returns>
    public Object GetPageInfo(String kind, String page)
    {
        /*
         * 页面配置生成流程：
         * 1，根据页面获取菜单，查询得到控制器
         * 2，从控制器中加载字段信息，加入结果集
         * 3，从配置参数中加载当前用户在该页面上的字段信息，加入结果集
         * 4，从配置参数中加载全局在该页面上的字段信息，加入结果集
         * 5，适配器处理结果集，返回最终结果
         */

        var adapter = _moduleManager.GetAdapter(kind);
        if (adapter == null) throw new ArgumentOutOfRangeException(nameof(kind), $"未找到适配器[{kind}]");

        return null;
    }

    /// <summary>保存页面配置信息。</summary>
    /// <param name="kind"></param>
    /// <param name="page"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public ActionResult SetPageInfo(String kind, String page, IDictionary<String, Object> value)
    {
        return null;
    }
}
