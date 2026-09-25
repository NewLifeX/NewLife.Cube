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
            var picks = new Dictionary<String, List<Int32>>();
            if (model.Picks != null)
            {
                foreach (var kv in model.Picks)
                {
                    if (kv.Key.IsNullOrEmpty() || kv.Value == null) continue;
                    picks[kv.Key] = kv.Value.Where(e => e > 0).ToList();
                }
            }
            var instance = WorkflowEngine.Start(def, model.Keys, user.ID, user.DisplayName ?? user.Name, model.Comment, model.Summary, model.Title, picks);
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
        try
        {
            WorkflowEngine.Approve(id, user.ID, model?.Comment);
            BindCommentAttachments(id, model?.AttachmentIds);
            return Json(0, "已同意");
        }
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
        try
        {
            WorkflowEngine.Reject(id, user.ID, model?.Comment);
            BindCommentAttachments(id, model?.AttachmentIds);
            return Json(0, "已驳回");
        }
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

    /// <summary>批量驳回（≤50，部分成功；跳过办理任务并返回条数）</summary>
    /// <param name="model">批量参数</param>
    /// <returns>逐条结果（办理任务条目带 skipped=true）</returns>
    [HttpPost("Tasks/BatchReject")]
    public Object BatchReject([FromBody] BatchModel? model)
    {
        var user = ManageProvider.User;
        if (user == null) return Json(401, "未授权");
        var ids = model?.Ids ?? [];
        if (ids.Length == 0) return Json(400, "未选择任务");
        if (ids.Length > 50) return Json(400, "一次最多批量处理 50 条");

        var rs = new List<Object>();
        var ok = 0;
        var skipped = 0;
        foreach (var id in ids)
        {
            try
            {
                var task = WorkflowTask.FindById(id);
                // 办理节点不能驳回：跳过并在结果里标记，完成后由前端提示条数
                if (task != null && WorkflowEngine.IsHandleNodeTask(task))
                {
                    rs.Add(new { id, ok = false, skipped = true, error = "办理任务不能驳回，已跳过" });
                    skipped++;
                    continue;
                }
                WorkflowEngine.Reject(id, user.ID, model?.Comment);
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
        var msg = $"成功 {ok}/{ids.Length}" + (skipped > 0 ? $"，已跳过 {skipped} 条办理任务" : "");
        return Json(0, msg, rs);
    }

    /// <summary>我的待办（仅当前用户：已认领本人，或或签未认领且候选含本人）</summary>
    /// <param name="page">页码，从 1 开始</param>
    /// <param name="pageSize">页大小</param>
    /// <param name="q">标题 / 摘要 / 业务对象</param>
    /// <returns>分页列表</returns>
    [HttpGet("Todo")]
    public Object Todo(Int32 page = 1, Int32 pageSize = 20, String? q = null)
    {
        var user = ManageProvider.User;
        if (user == null) return Json(401, "未授权");
        var tasks = WorkflowTask.FindTodoByUser(user.ID).OrderByDescending(e => e.CreateTime).ToList();
        return Json(0, null, PageTasks(tasks, page, pageSize, q));
    }

    /// <summary>我发起的</summary>
    /// <param name="page">页码，从 1 开始</param>
    /// <param name="pageSize">页大小</param>
    /// <param name="q">标题 / 摘要 / 业务对象</param>
    /// <returns>分页列表</returns>
    [HttpGet("Started")]
    public Object Started(Int32 page = 1, Int32 pageSize = 20, String? q = null)
    {
        var user = ManageProvider.User;
        if (user == null) return Json(401, "未授权");
        var list = WorkflowInstance.FindAll(WorkflowInstance._.StarterId == user.ID)
            .OrderByDescending(e => e.Id).ToList();
        var filtered = FilterByKeyword(list, q, inst =>
        [
            inst.Title,
            inst.Summary,
            ResolveTypeName(inst.TypePath),
            inst.TypePath,
        ]);
        return Json(0, null, PageOf(filtered, page, pageSize, ToInstanceView));
    }

    /// <summary>我已办（仅当前用户办理过的任务：Done/Transferred/Rejected 且 AssigneeId=本人）</summary>
    /// <param name="page">页码，从 1 开始</param>
    /// <param name="pageSize">页大小</param>
    /// <param name="q">标题 / 摘要 / 业务对象</param>
    /// <returns>分页列表</returns>
    [HttpGet("Done")]
    public Object Done(Int32 page = 1, Int32 pageSize = 20, String? q = null)
    {
        var user = ManageProvider.User;
        if (user == null) return Json(401, "未授权");
        var tasks = WorkflowTask.FindDoneByUser(user.ID)
            .OrderByDescending(e => e.FinishTime).ToList();
        return Json(0, null, PageTasks(tasks, page, pageSize, q));
    }

    /// <summary>任务列表按标题/摘要/业务对象过滤后分页</summary>
    static Object PageTasks(List<WorkflowTask> tasks, Int32 page, Int32 pageSize, String? q)
    {
        var filtered = FilterByKeyword(tasks, q, task =>
        {
            var instance = WorkflowInstance.FindById(task.InstanceId);
            var typePath = instance?.TypePath;
            return
            [
                instance?.Title,
                instance?.Summary,
                ResolveTypeName(typePath),
                typePath,
            ];
        });
        return PageOf(filtered, page, pageSize, ToTaskView);
    }

    static List<T> FilterByKeyword<T>(List<T> source, String? q, Func<T, String?[]> fields)
    {
        var key = q?.Trim();
        if (key.IsNullOrEmpty()) return source;
        return source.Where(item => fields(item).Any(f => !f.IsNullOrEmpty() && f.Contains(key, StringComparison.OrdinalIgnoreCase))).ToList();
    }

    static Object PageOf<T>(List<T> source, Int32 page, Int32 pageSize, Func<T, Object> view)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 1000) pageSize = 1000;
        var total = source.Count;
        var list = source.Skip((page - 1) * pageSize).Take(pageSize).Select(view).ToList();
        return new { total, page, pageSize, list };
    }
    #endregion

    #region 效率
    /// <summary>效率聚合结果（design §6.5）</summary>
    public class EfficiencyResult
    {
        /// <summary>平均耗时（小时；无样本 null）</summary>
        public Double? AvgHours { get; set; }

        /// <summary>完成率（窗口内通过或驳回 / 窗口内发起；无实例 null）</summary>
        public Double? CompletionRate { get; set; }

        /// <summary>超 48 小时仍未办完的计入任务数</summary>
        public Int32 OverdueOpen { get; set; }

        /// <summary>实例超 2000 条截断</summary>
        public Boolean Truncated { get; set; }

        /// <summary>聚合行</summary>
        public List<EfficiencyRow> Rows { get; set; } = [];

        /// <summary>最慢 10 条（仅 node/month 展开）</summary>
        public List<EfficiencySlowItem> Slow { get; set; } = [];
    }

    /// <summary>效率聚合行</summary>
    public class EfficiencyRow
    {
        /// <summary>行键</summary>
        public String Key { get; set; }

        /// <summary>行标题</summary>
        public String Title { get; set; }

        /// <summary>已结束节点样本数</summary>
        public Int32 Count { get; set; }

        /// <summary>平均耗时（小时）</summary>
        public Double? AvgHours { get; set; }

        /// <summary>中位耗时（小时）</summary>
        public Double? MedianHours { get; set; }

        /// <summary>超 48 小时比例</summary>
        public Double? Over48Rate { get; set; }
    }

    /// <summary>效率最慢条目</summary>
    public class EfficiencySlowItem
    {
        /// <summary>实例编号（字符串透传）</summary>
        public String InstanceId { get; set; }

        /// <summary>节点 Id</summary>
        public String NodeId { get; set; }

        /// <summary>标题</summary>
        public String Title { get; set; }

        /// <summary>办理人显示名</summary>
        public String Assignee { get; set; }

        /// <summary>耗时（小时）</summary>
        public Double Hours { get; set; }

        /// <summary>未办完（等待中）</summary>
        public Boolean Waiting { get; set; }
    }

    /// <summary>节点样本（已结束）</summary>
    class EffSample
    {
        public Int64 InstanceId { get; set; }
        public String NodeId { get; set; }
        public String NodeTitle { get; set; }
        public Int32 AssigneeId { get; set; }
        public Double Hours { get; set; }
        public DateTime InstanceTime { get; set; }
    }

    /// <summary>未结束任务的等待项</summary>
    class EffOpen
    {
        public Int64 InstanceId { get; set; }
        public String NodeId { get; set; }
        public String NodeTitle { get; set; }
        public Int32 AssigneeId { get; set; }
        public Double Hours { get; set; }
    }

    /// <summary>效率聚合（只读）。按流程/部门/用户/节点/年/月聚合节点耗时样本；无「效率」菜单 Detail 权 403</summary>
    /// <param name="groupBy">聚合维度：process/department/user/node/year/month，缺省 process</param>
    /// <param name="days">时间窗口天数：7/30/90，缺省 30；与 year 互斥</param>
    /// <param name="year">公历年（今年与前两年）；与 days 互斥</param>
    /// <param name="definitionId">流程定义编号（groupBy=node 必填；行筛选用）</param>
    /// <param name="departmentId">部门编号（行筛选；点部门行改按用户）</param>
    /// <param name="userId">用户编号（行筛选）</param>
    /// <param name="nodeId">节点 Id（groupBy=node 且展开最慢 10 条时）</param>
    /// <param name="month">yyyy-MM（groupBy=month 且展开最慢 10 条时）</param>
    /// <returns>三个数 + 行 + slow</returns>
    [HttpGet("Efficiency")]
    public Object Efficiency(String? groupBy = null, Int32? days = null, String? year = null,
        String? definitionId = null, String? departmentId = null, String? userId = null,
        String? nodeId = null, String? month = null)
    {
        var user = ManageProvider.User;
        if (user == null) return Json(401, "未授权");
        if (!WorkflowAuth.CanEfficiency(user)) return Json(403, "无效率菜单权限");

        var kind = (groupBy ?? "").Trim().ToLowerInvariant();
        if (kind.Length == 0) kind = "process";
        if (kind is not ("process" or "department" or "user" or "node" or "year" or "month"))
            return Json(400, "groupBy 非法");
        if (days != null && !year.IsNullOrEmpty()) return Json(400, "days 与 year 不能同时使用");

        var now = DateTime.Now;
        var thisYear = now.Year;
        DateTime winFrom;
        DateTime winTo;
        if (kind == "year")
        {
            // 固定今年与前两年共三行（忽略 days）
            winFrom = new DateTime(thisYear - 2, 1, 1);
            winTo = new DateTime(thisYear + 1, 1, 1);
        }
        else if (!year.IsNullOrEmpty())
        {
            if (year.Length != 4 || !Int32.TryParse(year, out var y) || y < thisYear - 2 || y > thisYear)
                return Json(400, "year 仅支持今年与前两年");
            winFrom = new DateTime(y, 1, 1);
            winTo = new DateTime(y + 1, 1, 1);
        }
        else
        {
            var d = days ?? 30;
            if (d is not (7 or 30 or 90)) return Json(400, "days 仅支持 7/30/90");
            winFrom = now.AddDays(-d);
            winTo = DateTime.MaxValue;
        }
        if (kind == "node" && definitionId.IsNullOrEmpty()) return Json(400, "按节点聚合必须指定 definitionId");

        try
        {
            var data = BuildEfficiency(kind, winFrom, winTo, now, definitionId, departmentId, userId, nodeId, month);
            return Json(0, null, data);
        }
        catch (Exception ex)
        {
            return Json(500, "效率聚合失败：" + ex.GetTrue().Message);
        }
    }

    /// <summary>效率聚合计算（design §6）。节点样本：oa.approve 且节点未标 excludeStats、意见非自动跳过/自动通过；未结束节点只计超长等待</summary>
    /// <param name="kind">聚合维度（已校验）</param>
    /// <param name="winFrom">窗口起点（含）</param>
    /// <param name="winTo">窗口终点（不含；MaxValue 表示至今）</param>
    /// <param name="now">当前时间</param>
    /// <param name="definitionId">流程定义筛选</param>
    /// <param name="departmentId">部门筛选（办理人当前 DepartmentID，0=未分配）</param>
    /// <param name="userId">用户筛选</param>
    /// <param name="nodeId">slow 展开节点</param>
    /// <param name="month">slow 展开月 yyyy-MM</param>
    /// <returns>聚合结果</returns>
    public static EfficiencyResult BuildEfficiency(String kind, DateTime winFrom, DateTime winTo, DateTime now,
        String? definitionId, String? departmentId, String? userId, String? nodeId, String? month)
    {
        var result = new EfficiencyResult();

        // 窗口实例（最新优先；超 2000 截断）
        var all = WorkflowInstance.FindAll(WorkflowInstance._.CreateTime >= winFrom);
        if (winTo < DateTime.MaxValue) all = all.Where(e => e.CreateTime < winTo).ToList();
        if (all.Count > 2000)
        {
            result.Truncated = true;
            all = all.OrderByDescending(e => e.CreateTime).Take(2000).ToList();
        }
        if (!definitionId.IsNullOrEmpty())
        {
            var did = definitionId.ToLong();
            all = all.Where(e => e.DefinitionId == did).ToList();
        }
        var instMap = all.ToDictionary(e => e.Id);

        var instIds = instMap.Keys;
        var tasks = instIds.Count > 0 ? WorkflowTask.FindAll(WorkflowTask._.InstanceId.In(instIds)).ToList() : [];
        var comments = instIds.Count > 0 ? WorkflowComment.FindAll(WorkflowComment._.InstanceId.In(instIds)).ToList() : [];

        // 自动意见任务（自动跳过/审批人为空自动通过）不计入
        var autoTaskIds = new HashSet<Int64>();
        foreach (var c in comments)
        {
            if (c.TaskId <= 0 || c.Content.IsNullOrEmpty()) continue;
            var content = c.Content.Trim();
            if (content == "自动跳过" || content == "审批人为空，自动通过") autoTaskIds.Add(c.TaskId);
        }

        // 定义名（行标题与 slow 标题）
        var defNames = new Dictionary<Int64, String>();
        if (instIds.Count > 0)
        {
            var defIds = all.Select(e => e.DefinitionId).Distinct().ToList();
            foreach (var d in WorkflowDefinition.FindAll(WorkflowDefinition._.Id.In(defIds)))
            {
                defNames[d.Id] = d.Name;
            }
        }

        // 图快照 / 用户 / 部门缓存
        var graphCache = new Dictionary<String, WorkflowGraph>();
        var deptCache = new Dictionary<Int32, Int32>();
        var deptNameCache = new Dictionary<Int32, String>();
        var userNameCache = new Dictionary<Int32, String>();

        WorkflowGraph GraphOf(WorkflowInstance e)
        {
            var json = e.GraphSnapshot;
            if (json.IsNullOrEmpty()) return null;
            if (graphCache.TryGetValue(json, out var cached)) return cached;
            var parsed = WorkflowGraph.Parse(json);
            graphCache[json] = parsed;
            return parsed;
        }
        Int32 DeptOf(Int32 uid)
        {
            if (uid <= 0) return 0;
            if (!deptCache.TryGetValue(uid, out var d))
            {
                deptCache[uid] = d = UserX.FindByID(uid)?.DepartmentID ?? 0;
            }
            return d;
        }
        String UserName(Int32 uid)
        {
            if (uid <= 0) return "—";
            if (!userNameCache.TryGetValue(uid, out var n))
            {
                var u = UserX.FindByID(uid);
                userNameCache[uid] = n = u == null ? "#" + uid : (u.DisplayName.IsNullOrEmpty() ? u.Name : u.DisplayName);
            }
            return n;
        }
        String DeptName(Int32 did)
        {
            if (did <= 0) return "未分配部门";
            if (!deptNameCache.TryGetValue(did, out var n))
            {
                deptNameCache[did] = n = Department.FindByID(did)?.Name ?? ("部门 #" + did);
            }
            return n;
        }
        String InstTitle(WorkflowInstance e) =>
            !e.Title.IsNullOrEmpty() ? e.Title : defNames.GetValueOrDefault(e.DefinitionId) ?? e.TypePath;

        // 节点样本与未结束等待
        var samples = new List<EffSample>();
        var opens = new List<EffOpen>();
        var statInsts = new List<WorkflowInstance>();
        var byInst = tasks.GroupBy(t => t.InstanceId).ToDictionary(g => g.Key, g => g.ToList());
        foreach (var inst in all)
        {
            var graph = GraphOf(inst);
            if (graph == null) continue;
            // 流程根 excludeStats：整条流程不进统计
            if (graph.Raw?["excludeStats"]?.GetValue<Boolean>() == true) continue;
            statInsts.Add(inst);

            var list = byInst.GetValueOrDefault(inst.Id, []);
            foreach (var node in graph.Nodes)
            {
                if (node.Type != WorkflowGraph.ApproveType) continue;
                if (node.Raw?["excludeStats"]?.GetValue<Boolean>() == true) continue;

                var nt = list.Where(t => BaseNodeId(t.NodeId) == node.Id && !autoTaskIds.Contains(t.Id)).ToList();
                if (nt.Count == 0) continue;

                var start = nt.Min(t => t.CreateTime);
                var ended = nt.Where(t => t.Status is WorkflowStatuses.Done or WorkflowStatuses.Rejected or WorkflowStatuses.Transferred)
                    .OrderBy(t => t.UpdateTime).ToList();
                var open = nt.Where(t => t.Status is WorkflowStatuses.Pending or WorkflowStatuses.Active).ToList();

                // 未结束任务：等待超 48 小时只计超长等待；节点还有未结束时不进平均/中位/比例
                foreach (var t in open)
                {
                    opens.Add(new EffOpen
                    {
                        InstanceId = inst.Id,
                        NodeId = node.Id,
                        NodeTitle = node.Name,
                        AssigneeId = FirstAssignee(t),
                        Hours = Math.Max(0, (now - t.CreateTime).TotalHours),
                    });
                }
                if (open.Count > 0 || ended.Count == 0) continue;

                // 或签取最早已结束，会签/依次取最晚；起点统一为计入任务最早 CreateTime
                var mode = nt[0].Mode;
                var endTask = mode == "or" ? ended.First() : ended.Last();
                samples.Add(new EffSample
                {
                    InstanceId = inst.Id,
                    NodeId = node.Id,
                    NodeTitle = node.Name,
                    AssigneeId = endTask.AssigneeId,
                    Hours = Math.Max(0, (endTask.UpdateTime - start).TotalHours),
                    InstanceTime = inst.CreateTime,
                });
            }
        }

        // 行筛选（部门/用户标签）；nodeId/month 只用于 slow 展开
        Boolean InScope(Int32 assigneeId)
        {
            if (!userId.IsNullOrEmpty() && assigneeId != userId.ToInt()) return false;
            if (!departmentId.IsNullOrEmpty() && DeptOf(assigneeId) != departmentId.ToInt()) return false;
            return true;
        }
        var scoped = samples.Where(s => InScope(s.AssigneeId)).ToList();
        var scopedOpens = opens.Where(o => InScope(o.AssigneeId)).ToList();
        // 部门/用户标签收窄后，完成率只计仍有计入样本或未结束任务的实例；未筛选时含自动通过等无样本实例
        var rateInsts = statInsts;
        if (!userId.IsNullOrEmpty() || !departmentId.IsNullOrEmpty())
        {
            var hit = new HashSet<Int64>();
            foreach (var s in scoped) hit.Add(s.InstanceId);
            foreach (var o in scopedOpens) hit.Add(o.InstanceId);
            rateInsts = statInsts.Where(e => hit.Contains(e.Id)).ToList();
        }

        // 三个汇总数：筛选之后、分组之前的全体
        result.AvgHours = scoped.Count > 0 ? Math.Round(scoped.Average(s => s.Hours), 1) : null;
        result.CompletionRate = rateInsts.Count > 0
            ? Math.Round((Double)rateInsts.Count(e => e.Status is WorkflowStatuses.Approved or WorkflowStatuses.Rejected) / rateInsts.Count, 3)
            : null;
        result.OverdueOpen = scopedOpens.Count(o => o.Hours > 48);

        // 聚合行
        var rows = new List<EfficiencyRow>();
        switch (kind)
        {
            case "process":
                foreach (var grp in scoped.GroupBy(s => instMap[s.InstanceId].DefinitionId))
                {
                    rows.Add(RowOf(grp.Key.ToString(), defNames.GetValueOrDefault(grp.Key) ?? ("定义 #" + grp.Key), grp.ToList()));
                }
                break;
            case "department":
                foreach (var grp in scoped.GroupBy(s => DeptOf(s.AssigneeId)))
                {
                    rows.Add(RowOf(grp.Key.ToString(), DeptName(grp.Key), grp.ToList()));
                }
                break;
            case "user":
                foreach (var grp in scoped.GroupBy(s => s.AssigneeId))
                {
                    rows.Add(RowOf(grp.Key.ToString(), UserName(grp.Key), grp.ToList()));
                }
                break;
            case "node":
                foreach (var grp in scoped.GroupBy(s => s.NodeId))
                {
                    rows.Add(RowOf(grp.Key, grp.First().NodeTitle, grp.ToList()));
                }
                break;
            case "year":
                for (var y = now.Year; y >= now.Year - 2; y--)
                {
                    rows.Add(RowOf(y.ToString(), y + "年", scoped.Where(s => s.InstanceTime.Year == y).ToList()));
                }
                break;
            case "month":
                foreach (var grp in scoped.GroupBy(s => s.InstanceTime.ToString("yyyy-MM")).OrderByDescending(g => g.Key))
                {
                    rows.Add(RowOf(grp.Key, grp.Key, grp.ToList()));
                }
                break;
        }
        // 默认按平均耗时从高到低；样本为 0 的行排最后
        rows.Sort((a, b) =>
        {
            var ac = a.Count == 0 ? 1 : 0;
            var bc = b.Count == 0 ? 1 : 0;
            if (ac != bc) return ac - bc;
            return (b.AvgHours ?? -1).CompareTo(a.AvgHours ?? -1);
        });
        result.Rows = rows;

        // 最慢 10 条：仅节点、月可展开
        var slow = new List<EfficiencySlowItem>();
        void AddSlow(Int64 instanceId, String slowNodeId, Int32 assigneeId, Double hours, Boolean waiting)
        {
            slow.Add(new EfficiencySlowItem
            {
                InstanceId = instanceId.ToString(),
                NodeId = slowNodeId,
                Title = instMap.TryGetValue(instanceId, out var i2) ? InstTitle(i2) : "",
                Assignee = UserName(assigneeId),
                Hours = Math.Round(hours, 1),
                Waiting = waiting,
            });
        }
        if (kind == "node" && !nodeId.IsNullOrEmpty())
        {
            foreach (var s in scoped.Where(x => x.NodeId == nodeId))
            {
                AddSlow(s.InstanceId, s.NodeId, s.AssigneeId, s.Hours, false);
            }
            foreach (var o in opens.Where(x => x.NodeId == nodeId && InScope(x.AssigneeId)))
            {
                AddSlow(o.InstanceId, o.NodeId, o.AssigneeId, o.Hours, true);
            }
        }
        else if (kind == "month" && !month.IsNullOrEmpty())
        {
            foreach (var s in scoped.Where(x => x.InstanceTime.ToString("yyyy-MM") == month))
            {
                AddSlow(s.InstanceId, s.NodeId, s.AssigneeId, s.Hours, false);
            }
            foreach (var o in opens.Where(x => InScope(x.AssigneeId) && instMap.TryGetValue(x.InstanceId, out var i3) && i3.CreateTime.ToString("yyyy-MM") == month))
            {
                AddSlow(o.InstanceId, o.NodeId, o.AssigneeId, o.Hours, true);
            }
        }
        result.Slow = slow.OrderByDescending(x => x.Hours).Take(10).ToList();

        return result;
    }

    /// <summary>构造聚合行（样本平均/中位/超 48 比例）</summary>
    static EfficiencyRow RowOf(String key, String title, List<EffSample> items)
    {
        var row = new EfficiencyRow { Key = key, Title = title, Count = items.Count };
        if (items.Count > 0)
        {
            row.AvgHours = Math.Round(items.Average(s => s.Hours), 1);
            row.MedianHours = Math.Round(Median(items.Select(s => s.Hours).ToList()), 1);
            row.Over48Rate = (Double)items.Count(s => s.Hours > 48) / items.Count;
        }
        return row;
    }

    /// <summary>中位数</summary>
    static Double Median(List<Double> list)
    {
        if (list.Count == 0) return 0;
        list.Sort();
        var n = list.Count;
        return n % 2 == 1 ? list[n / 2] : (list[n / 2 - 1] + list[n / 2]) / 2;
    }

    /// <summary>去掉加签临时后缀，得到定义图节点 Id</summary>
    static String BaseNodeId(String nodeId)
    {
        if (nodeId.IsNullOrEmpty()) return nodeId;
        var i = nodeId.IndexOf('#');
        return i < 0 ? nodeId : nodeId[..i];
    }

    /// <summary>任务办理人（未指派时取首个候选人）</summary>
    static Int32 FirstAssignee(WorkflowTask task)
    {
        if (task.AssigneeId > 0) return task.AssigneeId;
        var cands = WorkflowHelper.ReadIntArray(JsonNode.Parse(task.CandidateJson.IsNullOrEmpty() ? "[]" : task.CandidateJson) as JsonArray);
        return cands.Count > 0 ? cands[0] : 0;
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

        /// <summary>提交人自选。节点 Id → 用户 Id</summary>
        public Dictionary<String, Int32[]>? Picks { get; set; }
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
        publishedGraphJson = def.PublishedGraphJson,
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
            // 节点类型（oa.approve/oa.handle/...）：前端行按钮矩阵（办理行只有「已办理」）
            nodeType = NodeTypeOf(task, instance),
            task.AssigneeId,
            candidate = WorkflowHelper.ReadIntArray(JsonNode.Parse(task.CandidateJson.IsNullOrEmpty() ? "[]" : task.CandidateJson) as JsonArray),
            task.SequenceIndex,
            task.Visible,
            task.Status,
            // 未配置超时（TimeoutHours=0）时 DueTime 为 DateTime.MinValue。
            // 序列化成 "0001-01-01 00:00:00" 后，浏览器 Date 会把它当成 2001 年，倒计时变成九千多天。
            dueTime = task.DueTime.Year > 1900 ? task.DueTime : (DateTime?)null,
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

    /// <summary>任务所在节点类型（实例快照解析；缺失返回空串）</summary>
    /// <param name="task">任务</param>
    /// <param name="instance">实例</param>
    /// <returns>节点类型如 oa.approve / oa.handle</returns>
    static String NodeTypeOf(WorkflowTask task, WorkflowInstance? instance)
    {
        if (instance == null || instance.GraphSnapshot.IsNullOrEmpty()) return "";
        try
        {
            var graph = WorkflowGraph.Parse(instance.GraphSnapshot);
            return graph?.Find(BaseNodeId(task.NodeId))?.Type ?? "";
        }
        catch
        {
            return "";
        }
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

    /// <summary>上传流程附件。发起时 Key=实例Id；审批任务上传时 Key=实例Id:任务Id，提交意见时改绑到意见</summary>
    /// <param name="file">文件</param>
    /// <param name="instanceId">实例编号</param>
    /// <param name="taskId">任务编号（审批中上传时传）</param>
    /// <returns>附件信息</returns>
    [HttpPost("Attachments")]
    [RequestSizeLimit(50 * 1024 * 1024)]
    public async Task<Object> UploadAttachment(IFormFile? file, String? instanceId, String? taskId = null)
    {
        var user = ManageProvider.User;
        if (user == null) return Json(401, "未授权");
        if (file == null || file.Length <= 0) return Json(400, "未选择文件");
        var iid = instanceId.ToLong();
        if (iid <= 0) return Json(400, "实例无效");
        var instance = WorkflowInstance.FindById(iid);
        if (instance == null) return Json(404, "流程实例不存在");
        if (!CanView(instance, user)) return Json(403, "无权上传该流程附件");

        var tid = taskId.ToLong();
        if (!taskId.IsNullOrEmpty() && tid <= 0) return Json(400, "任务无效");
        if (tid > 0)
        {
            var task = WorkflowTask.FindById(tid);
            if (task == null || task.InstanceId != iid) return Json(404, "任务不存在");
        }

        try
        {
            var att = new Attachment
            {
                Category = "WorkflowComment",
                Key = tid > 0 ? $"{iid}:{tid}" : iid.ToString(),
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

    /// <summary>把审批意见附件改绑到本次意见（Key=实例Id:c意见Id），进度按意见列出</summary>
    /// <param name="taskId">任务编号</param>
    /// <param name="attachmentIds">附件编号数组</param>
    static void BindCommentAttachments(Int64 taskId, Int64[]? attachmentIds)
    {
        if (attachmentIds == null || attachmentIds.Length == 0) return;
        var task = WorkflowTask.FindById(taskId);
        if (task == null) return;
        // 本任务最新一条意见即本次提交写入的意见
        var comment = WorkflowComment.FindAll(WorkflowComment._.TaskId == taskId).OrderByDescending(e => e.Id).FirstOrDefault();
        if (comment == null) return;

        var prefix = task.InstanceId.ToString();
        foreach (var aid in attachmentIds)
        {
            var att = Attachment.FindById(aid);
            if (att == null || att.Category != "WorkflowComment") continue;
            // 仅允许把本实例暂存的附件绑定到本次意见
            var key = att.Key ?? "";
            if (key != prefix && !key.StartsWith(prefix + ":")) continue;
            att.Key = $"{prefix}:c{comment.Id}";
            att.Update();
        }
    }
    #endregion
}
