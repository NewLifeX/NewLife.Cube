using NewLife.Log;
using XCode;
using XCode.DataAccessLayer;
using XCode.Membership;
using NewLife.Cube.Workflow.Entity;

namespace NewLife.Cube.Workflow;

/// <summary>工作流宿主。把审批写锁拦截器挂到全局实体拦截器（WebAPI 核心库随 AddCube 生效）</summary>
/// <remarks>
/// 编译进 NewLife.Cube（WebAPI 版）：引用 NewLife.Cube 的宿主调用 AddCube 即具备工作流能力；
/// NewLife.CubeNC/CubeDemoNC 不 Link 本目录，MVC 版无此类型、无副作用。
/// </remarks>
public static class WorkflowHost
{
    private static Int32 _inited;

    /// <summary>注册全局写锁拦截器（幂等）。测试可直接调 Register 或自行 Add</summary>
    public static void Register()
    {
        if (Interlocked.Exchange(ref _inited, 1) != 0) return;

        EntityInterceptors.Global.Add(new WorkflowWriteInterceptor());
        XTrace.WriteLine("启用工作流写锁拦截器[WorkflowWriteInterceptor]");

        // 确保 Workflow 库建表/加列（Title/Summary 等）；Full 可 ALTER，无需删库
        try
        {
            var dal = DAL.Create("Workflow");
            dal.Db.Migration = Migration.Full;
            _ = WorkflowInstance.Meta.Count;
            _ = WorkflowDefinition.Meta.Count;
        }
        catch (Exception ex)
        {
            XTrace.WriteLine("Workflow 库初始化：{0}", ex.Message);
        }

        try
        {
            EnsureMenus();
        }
        catch (Exception ex)
        {
            XTrace.WriteException(ex);
        }
    }

    /// <summary>测试重置</summary>
    public static void Reset() => _inited = 0;

    /// <summary>把「流程审批」挂为系统一级菜单，叶子对齐各页面</summary>
    /// <remarks>
    /// WorkflowController 不在 Cube.Controllers 命名空间，MenuHelper.ScanController 扫不到，故手工播种；
    /// WorkflowDefinitionController 位于 Areas/Cube/Controllers（被 CubeArea 扫描建菜单/权限/实体页注册），
    /// 本方法再把它收编到「流程审批」下，避免出现在魔方管理里。
    /// 父节点为一级目录无 URL（避免点到 Meta JSON）；叶子 URL 与前端路由对齐：
    /// 定义 /Cube/WorkflowDefinition（实体多维页），任务 /Cube/Workflow/{Todo|Done|Started|Designer}。
    /// </remarks>
    public static void EnsureMenus()
    {
        var mf = ManageProvider.Menu;
        if (mf == null) return;

        var root = mf.Root;
        if (root == null) return;

        // 一级目录：流程审批（Workflow），与 Cube/Admin 平级
        var parent = root.Childs?.FirstOrDefault(e => e.Name.EqualIgnoreCase("Workflow"));
        if (parent == null)
        {
            // 兼容旧版：早期把 Workflow 挂在 Cube 下，这里提升为一级
            var cube = root.Childs?.FirstOrDefault(e => e.Name.EqualIgnoreCase("Cube"));
            parent = cube?.FindByPath("Workflow");
            if (parent != null && parent.ParentID != 0)
            {
                parent.ParentID = 0;
                (parent as IEntity)?.Save();
            }
        }
        if (parent == null)
            parent = root.Add("Workflow", "流程审批", typeof(Controllers.WorkflowController).FullName, null);

        parent.DisplayName = "流程审批";
        parent.Visible = true;
        if (parent.Icon.IsNullOrEmpty()) parent.Icon = "Stamp";
        if (parent.Sort == 0) parent.Sort = 90;
        // 父节点不要指向 /Cube/Workflow（那是 Meta JSON）
        if (!parent.Url.IsNullOrEmpty()) parent.Url = null;
        (parent as IEntity)?.Save();

        var wfc = typeof(Controllers.WorkflowController).FullName;
        var defc = typeof(NewLife.Cube.Areas.Cube.Controllers.WorkflowDefinitionController).FullName;
        EnsureLeaf(mf, parent, "Definition", "流程定义", "/Cube/WorkflowDefinition", 60, "Common", defc);
        EnsureLeaf(mf, parent, "Designer", "流程设计", "/Cube/Workflow/Designer", 50, "Share", $"{wfc}.Designer");
        EnsureLeaf(mf, parent, "Todo", "我的待办", "/Cube/Workflow/Todo", 40, "Checked", $"{wfc}.Todo");
        EnsureLeaf(mf, parent, "Started", "我发起的", "/Cube/Workflow/Started", 30, "Promotion", $"{wfc}.Started");
        EnsureLeaf(mf, parent, "Done", "已办", "/Cube/Workflow/Done", 20, "Select", $"{wfc}.Done");
    }

    static void EnsureLeaf(IMenuFactory mf, IMenu parent, String name, String display, String url, Int32 sort, String icon, String fullName)
    {
        var node = parent.FindByPath(name);
        node ??= parent.Childs?.FirstOrDefault(e => e.Name.EqualIgnoreCase(name) || e.Url.EqualIgnoreCase(url));
        // 实体控制器可能已被 CubeArea 扫描到 Cube 下（如 WorkflowDefinition），按 URL 全局收编到本目录
        node ??= mf.FindByUrl(url);
        if (node == null)
            node = parent.Add(name, display, fullName, url);
        if (node.ParentID != parent.ID)
        {
            node.ParentID = parent.ID;
        }
        node.Url = url;
        node.FullName = fullName;
        node.DisplayName = display;
        node.Visible = true;
        if (node.Icon.IsNullOrEmpty()) node.Icon = icon;
        if (node.Sort == 0) node.Sort = sort;
        (node as IEntity)?.Save();
    }
}
