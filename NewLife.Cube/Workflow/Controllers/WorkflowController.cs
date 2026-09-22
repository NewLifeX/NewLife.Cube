using System.ComponentModel;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.Automation;
using NewLife.Cube.Entity;
using NewLife.Cube.Workflow.Entity;
using NewLife.Log;
using NewLife.Reflection;
using XCode;
using XCode.Membership;
using UserX = XCode.Membership.User;
using WorkflowComment = NewLife.Cube.Workflow.Entity.WorkflowComment;
using WorkflowDefinition = NewLife.Cube.Workflow.Entity.WorkflowDefinition;
using WorkflowInstance = NewLife.Cube.Workflow.Entity.WorkflowInstance;
using WorkflowSubject = NewLife.Cube.Workflow.Entity.WorkflowSubject;
using WorkflowTask = NewLife.Cube.Workflow.Entity.WorkflowTask;

namespace NewLife.Cube.Workflow.Controllers;

/// <summary>OA 审批流程 API（OSC-26090347f1）。前缀 /Cube/Workflow；非 Meta 动作默认需登录（ControllerBaseX 门禁）</summary>
[DisplayName("审批流程")]
[Route("Cube/Workflow")]
public class WorkflowController : ControllerBaseX
{
    #region 定义
    /// <summary>能力探测。匿名返回 { enabled }；登录附加待办数</summary>
    /// <returns>能力 JSON</returns>
    /// <remarks>
    /// 规范路径为 GET /Cube/Workflow/Meta（与前端 api-core 一致）。
    /// 同时保留 GET /Cube/Workflow（裸 [HttpGet]），避免旧客户端/书签仍指向控制器根。
    /// </remarks>
    [AllowAnonymous]
    [HttpGet]
    [HttpGet("Meta")]
    public Object Meta()
    {
        var user = ManageProvider.User;
        var data = new Dictionary<String, Object> { ["enabled"] = true };
        if (user != null) data["todoCount"] = WorkflowTask.CountTodoByUser(user.ID);
        return Json(0, null, data);
    }

    /// <summary>定义列表。可按 typePath 过滤</summary>
    /// <param name="typePath">实体路径（可空；设计器页加载全部定义时不传）</param>
    /// <returns>定义列表</returns>
    /// <remarks>
    /// typePath 必须可空：非空引用类型参数会被 MVC 隐式推断为 [Required]，
    /// 前端 cubeApi.workflow.definitions({}) 不带该参数时会直接 400 “The typePath field is required.”。
    /// </remarks>
    [HttpGet("Definitions")]
    public Object GetDefinitions(String? typePath = null)
    {
        IList<WorkflowDefinition> list;
        if (!typePath.IsNullOrEmpty())
        {
            var np = AutomationPaths.NormalizeTypePath(typePath);
            list = WorkflowDefinition.FindAll(WorkflowDefinition._.TypePath == np).OrderByDescending(e => e.Id).ToList();
        }
        else
        {
            list = WorkflowDefinition.FindAll().OrderByDescending(e => e.Id).ToList();
        }
        return Json(0, null, list.Select(ToDefView).ToList());
    }

    /// <summary>新建定义（草稿）</summary>
    /// <param name="model">定义</param>
    /// <returns>新定义</returns>
    [HttpPost("Definitions")]
    public Object AddDefinition([FromBody] WorkflowDefModel? model)
    {
        var user = ManageProvider.User;
        if (!WorkflowAuth.CanManage(user)) return Json(403, "无权管理流程定义");

        var def = new WorkflowDefinition();
        FillDef(def, model);
        def.Published = false;
        def.Version = 1;
        if (def.GraphJson.IsNullOrEmpty()) def.GraphJson = "{}";
        def.Insert();
        return Json(0, null, ToDefView(def));
    }

    /// <summary>保存定义草稿（不改发布快照）</summary>
    /// <param name="id">定义编号</param>
    /// <param name="model">定义内容</param>
    /// <returns>保存结果</returns>
    [HttpPut("Definitions/{id}")]
    public Object UpdateDefinition(Int64 id, [FromBody] WorkflowDefModel? model)
    {
        var user = ManageProvider.User;
        if (!WorkflowAuth.CanManage(user)) return Json(403, "无权管理流程定义");
        var def = WorkflowDefinition.FindById(id);
        if (def == null) return Json(404, "流程定义不存在");

        FillDef(def, model);
        def.Update();
        return Json(0, null, ToDefView(def));
    }

    /// <summary>发布定义：校验图 → 钉扎 PublishedGraphJson → Version++</summary>
    /// <param name="id">定义编号</param>
    /// <returns>发布结果</returns>
    [HttpPost("Definitions/{id}/Publish")]
    public Object PublishDefinition(Int64 id)
    {
        var user = ManageProvider.User;
        if (!WorkflowAuth.CanManage(user)) return Json(403, "无权管理流程定义");
        var def = WorkflowDefinition.FindById(id);
        if (def == null) return Json(404, "流程定义不存在");

        var graph = WorkflowGraph.Parse(def.GraphJson);
        if (graph == null) return Json(400, "设计图 JSON 非法");
        var errors = graph.Validate();
        if (errors.Count > 0) return Json(400, "发布校验失败：" + errors.Join("；"));
        def.Publish();
        return Json(0, "发布成功", ToDefView(def));
    }
    #endregion

    #region 实例
    /// <summary>发起一批</summary>
    /// <param name="model">发起请求</param>
    /// <returns>实例编号</returns>
    [HttpPost("Instances")]
    public Object Start([FromBody] StartModel? model)
    {
        var user = ManageProvider.User;
        if (user == null) return Json(401, "未授权");
        if (model == null || model.Keys == null || model.Keys.Length == 0) return Json(400, "未选择记录");
        var defId = model.DefinitionId.ToLong();
        if (defId <= 0) return Json(400, "流程定义无效");
        var def = WorkflowDefinition.FindById(defId);
        if (def == null) return Json(404, "流程定义不存在");
        // 发起权限 = 实体 Detail（design §6.4）；行级 StartFilter 由引擎按主体逐条再判
        if (!WorkflowPageOverlay.CanDetail(user, def.TypePath)) return Json(403, "无该实体的发起权限");

        try
        {
            var instance = WorkflowEngine.Start(def, model.Keys, user.ID, user.DisplayName ?? user.Name, model.Comment, model.Summary, model.Title);
            return Json(0, null, new { instanceId = instance.Id.ToString() });
        }
        catch (WorkflowException ex) { return Json(ex.Code, ex.Message); }
        catch (Exception ex) { return Json(500, "发起失败：" + ex.GetTrue().Message); }
    }

    /// <summary>发起人撤回</summary>
    /// <param name="id">实例编号</param>
    /// <param name="model">请求（意见）</param>
    /// <returns>结果</returns>
    [HttpPost("Instances/{id}/Withdraw")]
    public Object Withdraw(Int64 id, [FromBody] CommentModel? model)
    {
        var user = ManageProvider.User;
        if (user == null) return Json(401, "未授权");
        try
        {
            WorkflowEngine.Withdraw(id, user.ID, model?.Comment);
            return Json(0, "已撤回");
        }
        catch (WorkflowException ex) { return Json(ex.Code, ex.Message); }
        catch (Exception ex) { return Json(500, ex.GetTrue().Message); }
    }

    /// <summary>管理员作废</summary>
    /// <param name="id">实例编号</param>
    /// <param name="model">请求（意见）</param>
    /// <returns>结果</returns>
    [HttpPost("Instances/{id}/Cancel")]
    public Object Cancel(Int64 id, [FromBody] CommentModel? model)
    {
        var user = ManageProvider.User;
        if (user == null) return Json(401, "未授权");
        if (!WorkflowAuth.CanManage(user)) return Json(403, "无权作废流程");
        try
        {
            WorkflowEngine.Cancel(id, user.ID, model?.Comment);
            return Json(0, "已作废");
        }
        catch (WorkflowException ex) { return Json(ex.Code, ex.Message); }
        catch (Exception ex) { return Json(500, ex.GetTrue().Message); }
    }

    /// <summary>指定节点跳转（管理员）</summary>
    /// <param name="id">实例编号</param>
    /// <param name="model">目标节点</param>
    /// <returns>结果</returns>
    [HttpPost("Instances/{id}/Jump")]
    public Object Jump(Int64 id, [FromBody] JumpModel? model)
    {
        var user = ManageProvider.User;
        if (user == null) return Json(401, "未授权");
        if (!WorkflowAuth.CanManage(user)) return Json(403, "无权跳转流程");
        try
        {
            WorkflowEngine.Jump(id, user.ID, model?.TargetNodeId, model?.Comment);
            return Json(0, "已跳转");
        }
        catch (WorkflowException ex) { return Json(ex.Code, ex.Message); }
        catch (Exception ex) { return Json(500, ex.GetTrue().Message); }
    }

    /// <summary>实例详情：主体 + 任务 + 意见时间轴。发起人/候选人/实体 Detail 可看</summary>
    /// <param name="id">实例编号</param>
    /// <returns>实例详情</returns>
    [HttpGet("Instances/{id}")]
    public Object InstanceDetail(Int64 id)
    {
        var user = ManageProvider.User;
        if (user == null) return Json(401, "未授权");
        var instance = WorkflowInstance.FindById(id);
        if (instance == null) return Json(404, "流程实例不存在");
        if (!CanView(instance, user)) return Json(403, "无权查看该流程");

        var def = WorkflowDefinition.FindById(instance.DefinitionId);
        var subjects = WorkflowSubject.FindAll(WorkflowSubject._.InstanceId == id).OrderBy(e => e.Id).ToList();
        var tasks = WorkflowTask.FindAll(WorkflowTask._.InstanceId == id).OrderBy(e => e.Id).ToList();
        var comments = WorkflowComment.FindAll(WorkflowComment._.InstanceId == id).OrderBy(e => e.Id).ToList();

        return Json(0, null, new
        {
            id = instance.Id.ToString(),
            instance.TypePath,
            instance.Status,
            definition = def == null
                ? null
                : new
                {
                    id = def.Id.ToString(),
                    def.Name,
                    // 钉扎版本：进度展示用实例 DefinitionVersion，避免 republish 后误显最新版
                    version = instance.DefinitionVersion,
                    def.LockPolicy,
                },
            instance.StarterId,
            instance.Title,
            instance.StartComment,
            instance.Summary,
            definitionVersion = instance.DefinitionVersion,
            instance.CreateTime,
            instance.FinishTime,
            graphSnapshot = instance.GraphSnapshot,
            writableFields = WorkflowWriteInterceptor.CurrentWritable(instance, user.ID).ToList(),
            subjects = subjects.Select(s => new { id = s.Id.ToString(), s.EntityKey, s.Title }).ToList(),
            tasks = tasks.Select(ToTaskView).ToList(),
            comments = comments.Select(c => new { id = c.Id.ToString(), taskId = c.TaskId.ToString(), c.Action, c.Content, c.CreateUser, c.CreateTime }).ToList(),
            attachments = ListWorkflowAttachments(id),
        });
    }

    /// <summary>查看权限：发起人 / 候选人 / 实体 Detail</summary>
    /// <param name="instance">实例</param>
    /// <param name="user">用户</param>
    /// <returns>是否可看</returns>
    static Boolean CanView(WorkflowInstance instance, IUser user)
    {
        if (instance.StarterId == user.ID) return true;
        var tasks = WorkflowTask.FindAll(WorkflowTask._.InstanceId == instance.Id);
        foreach (var task in tasks)
        {
            if (task.AssigneeId == user.ID) return true;
            if (WorkflowHelper.ReadIntArray(JsonNode.Parse(task.CandidateJson.IsNullOrEmpty() ? "[]" : task.CandidateJson) as JsonArray).Contains(user.ID)) return true;
        }
        return WorkflowPageOverlay.CanDetail(user, instance.TypePath);
    }
    #endregion

    #region 任务
    /// <summary>认领（或签）</summary>
    /// <param name="id">任务编号</param>
    /// <returns>结果</returns>
    [HttpPost("Tasks/{id}/Claim")]
    public Object Claim(Int64 id)
    {
        var user = ManageProvider.User;
        if (user == null) return Json(401, "未授权");
        try { WorkflowEngine.Claim(id, user.ID); return Json(0, "已认领"); }
        catch (WorkflowException ex) { return Json(ex.Code, ex.Message); }
        catch (Exception ex) { return Json(500, ex.GetTrue().Message); }
    }

    /// <summary>同意</summary>
    /// <param name="id">任务编号</param>
    /// <param name="model">意见</param>
    /// <returns>结果</returns>
    [HttpPost("Tasks/{id}/Approve")]
    public Object Approve(Int64 id, [FromBody] CommentModel? model)
    {
        var user = ManageProvider.User;
        if (user == null) return Json(401, "未授权");
        try { WorkflowEngine.Approve(id, user.ID, model?.Comment); return Json(0, "已同意"); }
        catch (WorkflowException ex) { return Json(ex.Code, ex.Message); }
        catch (Exception ex) { return Json(500, ex.GetTrue().Message); }
    }

    /// <summary>驳回</summary>
    /// <param name="id">任务编号</param>
    /// <param name="model">意见</param>
    /// <returns>结果</returns>
    [HttpPost("Tasks/{id}/Reject")]
    public Object Reject(Int64 id, [FromBody] CommentModel? model)
    {
        var user = ManageProvider.User;
        if (user == null) return Json(401, "未授权");
        try { WorkflowEngine.Reject(id, user.ID, model?.Comment); return Json(0, "已驳回"); }
        catch (WorkflowException ex) { return Json(ex.Code, ex.Message); }
        catch (Exception ex) { return Json(500, ex.GetTrue().Message); }
    }

    /// <summary>前/后加签</summary>
    /// <param name="id">任务编号</param>
    /// <param name="model">加签参数</param>
    /// <returns>结果</returns>
    [HttpPost("Tasks/{id}/AddSign")]
    public Object AddSign(Int64 id, [FromBody] AddSignModel? model)
    {
        var user = ManageProvider.User;
        if (user == null) return Json(401, "未授权");
        if (model == null || model.To == null) return Json(400, "缺少加签接收人");
        try { WorkflowEngine.AddSign(id, user.ID, model.Before, model.To, model.Comment); return Json(0, "已加签"); }
        catch (WorkflowException ex) { return Json(ex.Code, ex.Message); }
        catch (Exception ex) { return Json(500, ex.GetTrue().Message); }
    }

    /// <summary>转办（单人）</summary>
    /// <param name="id">任务编号</param>
    /// <param name="model">转办参数</param>
    /// <returns>结果</returns>
    [HttpPost("Tasks/{id}/Transfer")]
    public Object Transfer(Int64 id, [FromBody] ToModel? model)
    {
        var user = ManageProvider.User;
        if (user == null) return Json(401, "未授权");
        if (model == null || model.To == null) return Json(400, "缺少转办接收人");
        try { WorkflowEngine.Transfer(id, user.ID, model.To, model.Comment); return Json(0, "已转办"); }
        catch (WorkflowException ex) { return Json(ex.Code, ex.Message); }
        catch (Exception ex) { return Json(500, ex.GetTrue().Message); }
    }

    /// <summary>知会</summary>
    /// <param name="id">任务编号</param>
    /// <param name="model">知会参数</param>
    /// <returns>结果</returns>
    [HttpPost("Tasks/{id}/Cc")]
    public Object Cc(Int64 id, [FromBody] ToModel? model)
    {
        var user = ManageProvider.User;
        if (user == null) return Json(401, "未授权");
        if (model == null || model.To == null) return Json(400, "缺少知会接收人");
        try { WorkflowEngine.Cc(id, user.ID, model.To, model.Comment); return Json(0, "已知会"); }
        catch (WorkflowException ex) { return Json(ex.Code, ex.Message); }
        catch (Exception ex) { return Json(500, ex.GetTrue().Message); }
    }

    /// <summary>回退到已办节点</summary>
    /// <param name="id">任务编号</param>
    /// <param name="model">回退参数</param>
    /// <returns>结果</returns>
    [HttpPost("Tasks/{id}/Rollback")]
    public Object Rollback(Int64 id, [FromBody] RollbackModel? model)
    {
        var user = ManageProvider.User;
        if (user == null) return Json(401, "未授权");
        if (model == null || model.TargetNodeId.IsNullOrEmpty()) return Json(400, "缺少回退目标节点");
        try { WorkflowEngine.Rollback(id, user.ID, model.TargetNodeId, model.Comment); return Json(0, "已回退"); }
        catch (WorkflowException ex) { return Json(ex.Code, ex.Message); }
        catch (Exception ex) { return Json(500, ex.GetTrue().Message); }
    }

    /// <summary>批量同意（≤50，部分成功）</summary>
    /// <param name="model">批量参数</param>
    /// <returns>逐条结果</returns>
    [HttpPost("Tasks/BatchApprove")]
    public Object BatchApprove([FromBody] BatchModel? model)
    {
        var user = ManageProvider.User;
        if (user == null) return Json(401, "未授权");
        var ids = model?.Ids ?? [];
        if (ids.Length == 0) return Json(400, "未选择任务");
        if (ids.Length > 50) return Json(400, "一次最多批量处理 50 条");

        var rs = new List<Object>();
        var ok = 0;
        foreach (var id in ids)
        {
            try
            {
                WorkflowEngine.Approve(id, user.ID, model?.Comment);
                rs.Add(new { id, ok = true });
                ok++;
            }
            catch (WorkflowException ex)
            {
                rs.Add(new { id, ok = false, error = ex.Message });
            }
            catch (Exception ex)
            {
                rs.Add(new { id, ok = false, error = ex.GetTrue().Message });
            }
        }
        return Json(0, $"成功 {ok}/{ids.Length}", rs);
    }

    /// <summary>我的待办（仅当前用户：已认领本人，或或签未认领且候选含本人）</summary>
    /// <param name="pageSize">页大小</param>
    /// <returns>待办列表</returns>
    [HttpGet("Todo")]
    public Object Todo(Int32 pageSize = 50)
    {
        var user = ManageProvider.User;
        if (user == null) return Json(401, "未授权");
        var tasks = WorkflowTask.FindTodoByUser(user.ID).OrderByDescending(e => e.CreateTime).Take(pageSize).ToList();
        return Json(0, null, tasks.Select(ToTaskView).ToList());
    }

    /// <summary>我发起的</summary>
    /// <param name="pageSize">页大小</param>
    /// <returns>发起列表</returns>
    [HttpGet("Started")]
    public Object Started(Int32 pageSize = 50)
    {
        var user = ManageProvider.User;
        if (user == null) return Json(401, "未授权");
        var list = WorkflowInstance.FindAll(WorkflowInstance._.StarterId == user.ID)
            .OrderByDescending(e => e.Id).Take(pageSize).ToList();
        return Json(0, null, list.Select(ToInstanceView).ToList());
    }

    /// <summary>我已办（仅当前用户办理过的任务：Done/Transferred/Rejected 且 AssigneeId=本人）</summary>
    /// <param name="pageSize">页大小</param>
    /// <returns>已办列表</returns>
    [HttpGet("Done")]
    public Object Done(Int32 pageSize = 50)
    {
        var user = ManageProvider.User;
        if (user == null) return Json(401, "未授权");
        var tasks = WorkflowTask.FindDoneByUser(user.ID)
            .OrderByDescending(e => e.FinishTime).Take(pageSize).ToList();
        return Json(0, null, tasks.Select(ToTaskView).ToList());
    }
    #endregion

    #region 常用语与流程通道
    /// <summary>读取常用语</summary>
    /// <returns>常用语数组</returns>
    [HttpGet("Phrases")]
    public Object GetPhrases()
    {
        var list = WorkflowHelper.PhraseList(TenantContext.CurrentId);
        return Json(0, null, list);
    }

    /// <summary>保存常用语</summary>
    /// <param name="model">常用语列表 [{text}]</param>
    /// <returns>结果</returns>
    [HttpPut("Phrases")]
    public Object PutPhrases([FromBody] PhraseModel? model)
    {
        var user = ManageProvider.User;
        if (user == null) return Json(401, "未授权");
        if (!WorkflowAuth.CanManage(user)) return Json(403, "无权管理常用语");
        WorkflowHelper.SavePhrases(TenantContext.CurrentId, model?.Texts ?? []);
        return Json(0, "已保存");
    }

    /// <summary>审批通道改字段（节点可写字段 PATCH）。进入 WorkflowWriteScope，仅当前节点候选人可写字段生效</summary>
    /// <param name="typePath">实体路径</param>
    /// <param name="key">主键</param>
    /// <param name="model">字段键值</param>
    /// <returns>更新结果</returns>
    [HttpPost("Entities/{key}/Patch")]
    public Object Patch(String? typePath, String? key, [FromBody] Dictionary<String, Object>? model)
    {
        var user = ManageProvider.User;
        if (user == null) return Json(401, "未授权");
        if (typePath.IsNullOrEmpty()) return Json(400, "缺少实体路径");
        if (model == null || model.Count == 0) return Json(400, "没有可更新字段");

        var np = AutomationPaths.NormalizeTypePath(typePath);
        var subject = WorkflowSubject.FindRunning(np, key);
        if (subject == null) return Json(409, "该记录没有在途审批");
        var instance = WorkflowInstance.FindById(subject.InstanceId);
        if (instance == null || instance.Status != WorkflowStatuses.Running) return Json(409, "流程已结束");

        // 当前用户在实例上的节点可写字段
        var writable = WorkflowWriteInterceptor.CurrentWritable(instance, user.ID);
        if (writable.Count == 0) return Json(403, "当前用户不是节点候选人，不能修改");
        var patch = model.Where(e => writable.Contains(e.Key)).ToDictionary(e => e.Key, e => e.Value, StringComparer.OrdinalIgnoreCase);
        if (patch.Count == 0) return Json(403, "没有可写字段");

        var factory = WorkflowEngine.ResolveFactory(np);
        var entity = factory.FindByKey(key);
        if (entity == null) return Json(404, "记录不存在");

        // 按字段类型转换并赋值（含脏字段跟踪，拦截器据此放行）
        foreach (var kv in patch)
        {
            var fi = factory.Fields.FirstOrDefault(e => e.Name.EqualIgnoreCase(kv.Key));
            if (fi == null) continue;
            entity.SetItem(fi.Name, ChangeTypeValue(kv.Value, fi.Type));
        }
        using (WorkflowWriteScope.Enter(instance.Id, null, writable))
        {
            entity.Update();
        }
        return Json(0, "已更新");
    }
    #endregion

    #region 模型
    // 以下请求模型的成员一律可空：项目开启 Nullable 标注且 MVC 默认
    // SuppressImplicitRequiredAttributeForNonNullableReferenceTypes=false，非空引用类型属性会被隐式推断为 [Required]，
    // 而前端（api-core）会省略可选字段甚至不带 body（如 approve(id)），导致请求被模型校验拦下 400。
    // 必填语义一律在动作内部用中文提示校验。

    /// <summary>定义写入模型</summary>
    public class WorkflowDefModel
    {
        /// <summary>实体路径</summary>
        public String? TypePath { get; set; }

        /// <summary>名称</summary>
        public String? Name { get; set; }

        /// <summary>启用</summary>
        public Boolean Enable { get; set; } = true;

        /// <summary>锁策略</summary>
        public String? LockPolicy { get; set; }

        /// <summary>发起过滤</summary>
        public String? StartFilter { get; set; } = "{}";

        /// <summary>图 JSON</summary>
        public String? GraphJson { get; set; }

        /// <summary>备注</summary>
        public String? Remark { get; set; }
    }

    /// <summary>发起请求</summary>
    public class StartModel
    {
        /// <summary>实体路径</summary>
        public String? TypePath { get; set; }

        /// <summary>业务主键数组</summary>
        public String[]? Keys { get; set; }

        /// <summary>定义编号（雪花 Id：前端必须字符串透传，避免 JS Number 丢精度）</summary>
        public String? DefinitionId { get; set; }

        /// <summary>发起意见</summary>
        public String? Comment { get; set; }

        /// <summary>流程摘要（Markdown/富文本）</summary>
        public String? Summary { get; set; }

        /// <summary>流程标题</summary>
        public String? Title { get; set; }
    }

    /// <summary>意见请求</summary>
    public class CommentModel
    {
        /// <summary>意见</summary>
        public String? Comment { get; set; }

        /// <summary>附件 Id（同意/驳回时绑定到本次意见，Category=WorkflowComment）</summary>
        public Int64[]? AttachmentIds { get; set; }
    }

    /// <summary>跳转请求</summary>
    public class JumpModel
    {
        /// <summary>目标节点</summary>
        public String? TargetNodeId { get; set; }

        /// <summary>意见</summary>
        public String? Comment { get; set; }
    }

    /// <summary>加签请求</summary>
    public class AddSignModel
    {
        /// <summary>是否前加签</summary>
        public Boolean Before { get; set; }

        /// <summary>接收人</summary>
        public JsonObject? To { get; set; }

        /// <summary>意见</summary>
        public String? Comment { get; set; }
    }

    /// <summary>转办/知会请求</summary>
    public class ToModel
    {
        /// <summary>接收人</summary>
        public JsonObject? To { get; set; }

        /// <summary>意见</summary>
        public String? Comment { get; set; }
    }

    /// <summary>回退请求</summary>
    public class RollbackModel
    {
        /// <summary>目标节点</summary>
        public String? TargetNodeId { get; set; }

        /// <summary>意见</summary>
        public String? Comment { get; set; }
    }

    /// <summary>批量请求</summary>
    public class BatchModel
    {
        /// <summary>任务编号数组</summary>
        public Int64[]? Ids { get; set; }

        /// <summary>意见</summary>
        public String? Comment { get; set; }
    }

    /// <summary>常用语请求</summary>
    public class PhraseModel
    {
        /// <summary>常用语文本</summary>
        public String[]? Texts { get; set; }
    }
    #endregion

    #region 辅助
    static void FillDef(WorkflowDefinition def, WorkflowDefModel? model)
    {
        if (model == null) throw new WorkflowException(400, "请求数据为空");
        var typePath = (model.TypePath ?? "").Trim();
        if (typePath.IsNullOrEmpty()) throw new WorkflowException(400, "实体路径不能为空");
        var name = (model.Name ?? "").Trim();
        if (name.IsNullOrEmpty()) throw new WorkflowException(400, "名称不能为空");
        def.TypePath = AutomationPaths.NormalizeTypePath(typePath);
        def.Name = name;
        def.Enable = model.Enable;
        def.LockPolicy = model.LockPolicy ?? WorkflowStatuses.LockFull;
        def.StartFilter = model.StartFilter ?? "{}";
        def.GraphJson = model.GraphJson ?? def.GraphJson;
        def.Remark = model.Remark;
    }

    /// <summary>类型转换</summary>
    /// <param name="value">值</param>
    /// <param name="type">目标类型</param>
    /// <returns>转换结果</returns>
    static Object ChangeTypeValue(Object value, Type type)
    {
        if (value == null) return null;
        if (type == typeof(String)) return Convert.ToString(value);
        if (type == typeof(Int32)) return value.ToInt();
        if (type == typeof(Int64)) return value.ToLong();
        if (type == typeof(Double)) return value.ToDouble();
        if (type == typeof(Decimal)) return value.ToDecimal();
        if (type == typeof(Boolean)) return value.ToBoolean();
        if (type == typeof(DateTime)) return value.ToDateTime();
        return value;
    }

    static Object ToDefView(WorkflowDefinition def) => new
    {
        def.Id,
        def.TypePath,
        def.Name,
        def.Enable,
        def.Published,
        def.Version,
        def.LockPolicy,
        def.StartFilter,
        graphJson = def.GraphJson,
        def.Remark,
        def.UpdateTime,
    };

    static Object ToInstanceView(WorkflowInstance instance)
    {
        var subject = WorkflowSubject.FindAll(WorkflowSubject._.InstanceId == instance.Id).OrderBy(e => e.Id).FirstOrDefault();
        return new
        {
            id = instance.Id.ToString(),
            instance.TypePath,
            typeName = ResolveTypeName(instance.TypePath),
            instance.Status,
            definitionId = instance.DefinitionId.ToString(),
            instance.DefinitionVersion,
            title = !instance.Title.IsNullOrEmpty() ? instance.Title : (subject?.Title ?? instance.TypePath),
            instance.StarterId,
            instance.StartComment,
            instance.Summary,
            currentApprover = ResolveCurrentApprover(instance.Id, instance.Status),
            instance.CreateTime,
            instance.FinishTime,
        };
    }

    static Object ToTaskView(WorkflowTask task)
    {
        var instance = WorkflowInstance.FindById(task.InstanceId);
        var subject = WorkflowSubject.FindAll(WorkflowSubject._.InstanceId == task.InstanceId).OrderBy(e => e.Id).FirstOrDefault();
        var typePath = instance?.TypePath;
        return new
        {
            id = task.Id.ToString(),
            instanceId = task.InstanceId.ToString(),
            task.NodeId,
            task.Mode,
            task.AssigneeId,
            candidate = WorkflowHelper.ReadIntArray(JsonNode.Parse(task.CandidateJson.IsNullOrEmpty() ? "[]" : task.CandidateJson) as JsonArray),
            task.SequenceIndex,
            task.Visible,
            task.Status,
            task.DueTime,
            task.TimeoutAction,
            task.ClaimTime,
            task.FinishTime,
            task.CreateTime,
            task.UpdateTime,
            instanceStatus = instance?.Status,
            typePath,
            typeName = ResolveTypeName(typePath),
            title = instance != null && !instance.Title.IsNullOrEmpty() ? instance.Title : subject?.Title,
            summary = instance?.Summary,
            currentApprover = instance == null ? "" : ResolveCurrentApprover(instance.Id, instance.Status),
        };
    }

    /// <summary>实体友好名：菜单 DisplayName → 实体类型 DisplayName → 路径末段</summary>
    static String ResolveTypeName(String? typePath)
    {
        if (typePath.IsNullOrEmpty()) return "";
        var menu = AutomationAuth.FindMenu(typePath);
        if (menu != null)
        {
            if (!menu.DisplayName.IsNullOrEmpty()) return menu.DisplayName;
            if (!menu.Name.IsNullOrEmpty()) return menu.Name;
        }
        var np = AutomationPaths.NormalizeTypePath(typePath);
        foreach (var kv in EntityPageRegistry.GetAll())
        {
            var url = AutomationPaths.NormalizeTypePath(kv.Value?.Url);
            if (!url.EqualIgnoreCase(np)) continue;
            var display = kv.Key.GetDisplayName();
            return display.IsNullOrEmpty() ? kv.Key.Name : display;
        }
        var slash = np.LastIndexOf('/');
        return slash >= 0 && slash < np.Length - 1 ? np[(slash + 1)..] : np;
    }

    /// <summary>在途实例当前审批人（已认领显示办理人，未认领显示候选人；已结束为空）</summary>
    static String ResolveCurrentApprover(Int64 instanceId, String? status)
    {
        if (!status.EqualIgnoreCase(WorkflowStatuses.Running)) return "";
        var open = WorkflowTask.FindAll(WorkflowTask._.InstanceId == instanceId)
            .Where(t => t.Visible && t.Status is WorkflowStatuses.Pending or WorkflowStatuses.Active)
            .ToList();
        var ids = new List<Int32>();
        foreach (var t in open)
        {
            if (t.AssigneeId > 0)
            {
                if (!ids.Contains(t.AssigneeId)) ids.Add(t.AssigneeId);
                continue;
            }
            foreach (var id in WorkflowHelper.ReadIntArray(JsonNode.Parse(t.CandidateJson.IsNullOrEmpty() ? "[]" : t.CandidateJson) as JsonArray))
            {
                if (id > 0 && !ids.Contains(id)) ids.Add(id);
            }
        }
        if (ids.Count == 0) return "";
        var names = new List<String>(ids.Count);
        foreach (var id in ids)
        {
            var u = UserX.FindByID(id);
            names.Add(u == null ? $"#{id}" : (u.DisplayName.IsNullOrEmpty() ? u.Name : u.DisplayName));
        }
        return String.Join("、", names);
    }

    /// <summary>列出实例关联附件（Key=实例Id；兼容历史 Key=实例Id:任务Id）</summary>
    static Object ListWorkflowAttachments(Int64 instanceId)
    {
        var key = instanceId.ToString();
        var list = Attachment.FindAll(Attachment._.Category == "WorkflowComment")
            .Where(a => a.Key == key || (a.Key != null && a.Key.StartsWith(key + ":")))
            .OrderBy(e => e.Id)
            .Select(a => new
            {
                id = a.Id.ToString(),
                a.Title,
                a.FileName,
                a.Size,
                a.Url,
                a.Key,
                a.CreateTime,
            }).ToList();
        return list;
    }

    /// <summary>上传流程附件（发起时）。Category=WorkflowComment，Key=实例Id。审批节点不再上传。</summary>
    [HttpPost("Attachments")]
    [RequestSizeLimit(50 * 1024 * 1024)]
    public async Task<Object> UploadAttachment(IFormFile? file, String? instanceId, String? taskId = null)
    {
        var user = ManageProvider.User;
        if (user == null) return Json(401, "未授权");
        if (file == null || file.Length <= 0) return Json(400, "未选择文件");
        // 审批节点不再接受按任务挂附件；统一挂到实例，供后续审批人查看
        if (!taskId.IsNullOrEmpty()) return Json(400, "审批节点不支持上传附件，请在发起时上传");
        var iid = instanceId.ToLong();
        if (iid <= 0) return Json(400, "实例无效");
        var instance = WorkflowInstance.FindById(iid);
        if (instance == null) return Json(404, "流程实例不存在");
        if (!CanView(instance, user)) return Json(403, "无权上传该流程附件");

        try
        {
            var att = new Attachment
            {
                Category = "WorkflowComment",
                Key = iid.ToString(),
                Title = file.FileName,
                FileName = file.FileName,
                ContentType = file.ContentType,
                Size = file.Length,
                Enable = true,
                UploadTime = DateTime.Now,
            };
            await using var stream = file.OpenReadStream();
            await att.SaveFile(stream);
            return Json(0, null, new { id = att.Id.ToString(), att.FileName, att.Url, att.Size });
        }
        catch (Exception ex)
        {
            return Json(500, "上传失败：" + ex.GetTrue().Message);
        }
    }
    #endregion
}
