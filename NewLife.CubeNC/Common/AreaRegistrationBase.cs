using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using NewLife.Cube.Membership;
using NewLife.Log;
using NewLife.Reflection;
using XCode;
using XCode.Membership;

namespace NewLife.Cube;

/// <summary>区域特性基类</summary>
/// <remarks>
/// 提供以下功能：
/// 1，区域名称。从类名中截取。其中DisplayName特性作为菜单中文名。
/// 2，静态构造注册一次视图引擎、绑定提供者、过滤器
/// 3，注册区域默认路由
/// </remarks>
public class AreaBase : AreaAttribute
{
    private static readonly ConcurrentDictionary<Type, Type> _areas = new();

    /// <summary>实例化区域注册</summary>
    public AreaBase(String areaName) : base(areaName) => RegisterArea(GetType());

    /// <summary>注册区域，每个继承此区域特性的类的静态构造函数都调用此方法，以进行相关注册</summary>
    public static void RegisterArea<T>() where T : AreaBase => RegisterArea(typeof(T));

    /// <summary>注册区域，每个继承此区域特性的类的静态构造函数都调用此方法，以进行相关注册</summary>
    public static void RegisterArea(Type areaType)
    {
        if (!_areas.TryAdd(areaType, areaType)) return;

        var ns = areaType.Namespace + ".Controllers";
        var areaName = areaType.Name.TrimEnd("Area");
        XTrace.WriteLine("开始注册权限管理区域[{0}]，控制器命名空间[{1}]", areaName, ns);

        // 更新区域名集合
        var rs = CubeService.AreaNames?.ToList() ?? new List<String>();
        if (!rs.Contains(areaName))
        {
            rs.Add(areaName);
            CubeService.AreaNames = rs.ToArray();
        }

        // 自动检查并添加菜单
        var task = Task.Run(() =>
        {
            using var span = DefaultTracer.Instance?.NewSpan(nameof(ScanController), areaType.FullName);
            try
            {
                ScanController(areaType);
            }
            catch (Exception ex)
            {
                span?.SetError(ex, null);
                XTrace.WriteException(ex);
            }
        });
        task.Wait(5_000);
    }

    /// <summary>自动扫描控制器，并添加到菜单</summary>
    /// <remarks>默认操作当前注册区域的下一级Controllers命名空间</remarks>
    protected static void ScanController(Type areaType)
    {
        var areaName = areaType.Name.TrimEnd("Area");
        XTrace.WriteLine("start------初始化[{0}]的菜单体系------start", areaName);

        var mf = ManageProvider.Menu;
        if (mf == null) return;

        // 初始化数据库
        _ = Menu.Meta.Count;
        //_ = ModelTable.Meta.Count;
        //_ = ModelColumn.Meta.Count;

        //using var tran = (mf as IEntityFactory).Session.CreateTrans();

        //var menus = mf.ScanController(areaName, areaType.Assembly, areaType.Namespace + ".Controllers");
        var menus = MenuHelper.ScanController(mf, areaName, areaType);

        // 更新区域名称为友好中文名
        var menu = mf.Root.FindByPath(areaName);
        if (menu != null && menu.DisplayName.IsNullOrEmpty())
        {
            var dis = areaType.GetDisplayName();
            var des = areaType.GetDescription();

            if (!dis.IsNullOrEmpty()) menu.DisplayName = dis;
            if (!des.IsNullOrEmpty()) menu.Remark = des;

            (menu as IEntity).Update();
        }

        //tran.Commit();

        //// 扫描模型表
        //ScanModel(areaName, menus);

        // 再次检查菜单权限，因为上面的ScanController里开启菜单权限检查时，菜单可能还没有生成
        var task = Task.Run(() =>
        {
            //Thread.Sleep(1000);
            XTrace.WriteLine("新增了菜单，需要检查权限。二次检查，双重保障");
            typeof(Role).Invoke("CheckRole");
        });
        task.Wait(5_000);

        XTrace.WriteLine("end---------初始化[{0}]的菜单体系---------end", areaName);
    }

    private static ICollection<String> _namespaces;

    /// <summary>判断控制器是否归属于魔方管辖</summary>
    /// <param name="controllerActionDescriptor"></param>
    /// <returns></returns>
    public static Boolean Contains(ControllerActionDescriptor controllerActionDescriptor)
    {
        // 判断控制器是否在管辖范围之内
        var controller = controllerActionDescriptor.ControllerTypeInfo;
        var ns = controller.Namespace;
        if (!ns.EndsWith(".Controllers")) return false;

        _namespaces ??= new HashSet<String>(_areas.Keys.Select(e => e.Namespace));

        // 该控制器父级命名空间必须有对应的区域注册类，才会拦截其异常
        ns = ns.TrimEnd(".Controllers");
        return _namespaces.Contains(ns);
    }

    /// <summary>获取所有区域</summary>
    /// <returns></returns>
    public static ICollection<Type> GetAreas() => _areas.Keys;
}