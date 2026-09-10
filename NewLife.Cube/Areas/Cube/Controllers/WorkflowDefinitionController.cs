using System.ComponentModel;
using NewLife.Cube.Workflow.Entity;
using XCode.Membership;

namespace NewLife.Cube.Areas.Cube.Controllers;

/// <summary>流程定义管理。标准实体多维页（GetPage/GetList/Object 契约），供「流程审批」一级菜单下「流程定义」挂载</summary>
/// <remarks>
/// 放 Areas/Cube/Controllers 以便 CubeArea 自动扫描（建菜单/权限子项/EntityPageRegistry），
/// WorkflowHost.EnsureMenus 再把它收编到一级菜单「流程审批」下，避免出现在魔方管理里。
/// 可视化设计仍在 /Cube/Workflow/Designer（图 JSON 不在列表直读）。
/// </remarks>
[CubeArea]
[Menu(60, true, Icon = "Common")]
public class WorkflowDefinitionController : EntityController<WorkflowDefinition>
{
    /// <summary>实例化</summary>
    static WorkflowDefinitionController()
    {
        // 大 JSON / 租户 / 审计列不进列表，保持多维视图清爽
        ListFields.RemoveField("StartFilter", "GraphJson", "PublishedGraphJson");
        ListFields.RemoveField("TenantId", "CreateUser", "CreateUserID", "CreateIP", "CreateTime");
        ListFields.RemoveField("UpdateUser", "UpdateUserID", "UpdateIP");

        // 行内跳转流程设计器，避免在列表里直接编辑图 JSON
        var df = ListFields.AddListField("Design");
        df.DisplayName = "设计";
        df.Title = "打开流程设计器";
        df.Url = "/Cube/Workflow/Designer?id={Id}";
    }
}
