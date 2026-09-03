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

    /// <summary>是否可管理工作流（系统管理员，或对 /Cube/Workflow 菜单有 Update 权）</summary>
    /// <param name="user">用户</param>
    /// <returns>是否可管理</returns>
    public static Boolean CanManage(IUser user)
    {
        if (user == null) return false;
        if (IsAdmin(user)) return true;
        try
        {
            var menu = AutomationAuth.FindMenu("Cube/Workflow");
            return menu != null && user.Has(menu, PermissionFlags.Update);
        }
        catch
        {
            return false;
        }
    }
}
