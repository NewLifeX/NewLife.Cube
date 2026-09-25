using System.ComponentModel;
using NewLife.Cube.Automation;
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

        // 添加表单只留 名称/实体/备注：启用、版本、锁策略、发起条件、图 JSON 等由默认值与设计器维护，避免小白直接面对图 JSON
        // 注意 TenantId 经 SetRelation 被 Map 扩展属性 TenantName 替换，两者都要移除
        AddFormFields.RemoveField("TenantId", "TenantName", "Enable", "Published", "Version", "LockPolicy", "StartFilter", "GraphJson", "PublishedGraphJson");
        AddFormFields.RemoveCreateField();
        // 名称置顶：添加表单第一字段应为流程名称
        AddFormFields.RemoveField("Name");
        AddFormFields.AddDataField("Name", beforeName: "TypePath");

        // 实体路径改为下拉候选（显示菜单中文友好名，与 /Cube/Automation/Entities 同口径）；编辑表单同样友好，
        // 且编辑表单字段集保持原样：编辑保存为整表 PUT 回填，删字段会把图 JSON 等未提交列清空
        AddFormFields.GetField("TypePath").DataSource = _ => CandidateEntities();
        EditFormFields.GetField("TypePath").DataSource = _ => CandidateEntities();
    }

    /// <summary>新建实体。添加表单只提供 名称/实体/备注，启用与图草稿在此补默认</summary>
    /// <param name="entity">流程定义</param>
    /// <returns>插入行数</returns>
    /// <remarks>
    /// 只在表单通道（Add）补齐，不写进实体 Valid：引擎/测试直插实体不应被代为修改，
    /// 否则停用定义与空图草稿场景会被污染（曾导致既有矩阵用例大面积失败）。
    /// </remarks>
    protected override Int32 OnInsert(WorkflowDefinition entity)
    {
        if (!entity.Enable) entity.Enable = true;
        if (entity.GraphJson.IsNullOrEmpty()) entity.GraphJson = "{}";
        return base.OnInsert(entity);
    }

    /// <summary>可挂载流程的实体候选：类型路径 → 友好名</summary>
    /// <returns>类型路径字典。菜单显示名（中文）优先，其次实体类 DisplayName，最后类名</returns>
    /// <remarks>
    /// 与 AutomationController.Entities 同口径：遍历实体页注册表，按 Update 权限过滤（系统管理员全量）。
    /// 无登录上下文（单测/后台任务）时不过滤；进入本页已由 Cube 登录与菜单权限门禁保护。
    /// </remarks>
    static Dictionary<String, String> CandidateEntities()
    {
        var user = ManageProvider.User as IUser;
        var dic = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in EntityPageRegistry.GetAll())
        {
            var url = kv.Value?.Url;
            if (url.IsNullOrEmpty()) continue;
            var typePath = AutomationPaths.NormalizeTypePath(url);
            if (typePath.IsNullOrEmpty() || dic.ContainsKey(typePath)) continue;
            if (user != null && !AutomationAuth.HasPermission(user, typePath, PermissionFlags.Update)) continue;
            var display = AutomationAuth.FindMenu(typePath)?.DisplayName;
            if (display.IsNullOrEmpty()) display = kv.Key.GetDisplayName();
            if (display.IsNullOrEmpty()) display = kv.Key.Name;
            dic[typePath] = display;
        }
        return dic;
    }
}
