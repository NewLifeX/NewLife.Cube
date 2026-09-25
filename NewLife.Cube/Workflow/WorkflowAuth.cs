using NewLife.Cube.Automation;
using XCode.Membership;

namespace NewLife.Cube.Workflow;

/// <summary>工作流管理权限。定义管理/作废/跳转/常用语等平台操作需管理权</summary>
public static class WorkflowAuth
{
    /// <summary>是否系统管理员</summary>
    /// <param name="user">用户</param>
    /// <returns>是否系统</returns>
    public static Boolean IsAdmin(IUser user) => user != null && AutomationAuth.IsSystem(user);

    /// <summary>是否可管理工作流（系统管理员，或对「流程定义」菜单有 Update 权）</summary>
    /// <remarks>
    /// 流程审批已是一级菜单（父节点无 URL，FindMenu("Cube/Workflow") 不可达），
    /// 平台管理权收敛到「流程定义」实体菜单（/Cube/WorkflowDefinition）的 Update；
    /// 兼容旧菜单 Cube/Workflow（升级前播种的版本）。
    /// </remarks>
    /// <param name="user">用户</param>
    /// <returns>是否可管理</returns>
    public static Boolean CanManage(IUser user)
    {
        if (user == null) return false;
        if (IsAdmin(user)) return true;
        try
        {
            var menu = AutomationAuth.FindMenu("Cube/WorkflowDefinition");
            menu ??= AutomationAuth.FindMenu("Cube/Workflow");
            return menu != null && user.Has(menu, PermissionFlags.Update);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>是否可看效率页（系统管理员，或对「效率」菜单有 Detail 权）</summary>
    /// <remarks>design §6.1：无 Detail 权的人菜单不出现，GET /Cube/Workflow/Efficiency 返回 403。</remarks>
    /// <param name="user">用户</param>
    /// <returns>是否可看</returns>
    public static Boolean CanEfficiency(IUser user)
    {
        if (user == null) return false;
        if (IsAdmin(user)) return true;
        try
        {
            var menu = AutomationAuth.FindMenu("Cube/Workflow/Efficiency");
            return menu != null && user.Has(menu, PermissionFlags.Detail);
        }
        catch
        {
            return false;
        }
    }
}
