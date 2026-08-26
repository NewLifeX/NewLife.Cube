using System.ComponentModel;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using NewLife.Cube.Automation;
using NewLife.Cube.Entity;
using NewLife.Cube.Services;
using NewLife.Data;
using NewLife.Reflection;
using XCode;
using XCode.Membership;
using UserX = XCode.Membership.User;
using HttpContext = Microsoft.AspNetCore.Http.HttpContext;

namespace NewLife.Cube.Controllers;

/// <summary>实体自动化流程 API（OSC-260815fa86）</summary>
[DisplayName("实体自动化流程")]
[Route("Cube/Automation")]
public class AutomationController(TokenService tokenService) : ControllerBaseX
{
    /// <inheritdoc />
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var descriptor = context.ActionDescriptor as ControllerActionDescriptor;
        var allowAnonymous = descriptor?.MethodInfo.GetCustomAttributes(typeof(AllowAnonymousAttribute), true).FirstOrDefault();
        if (allowAnonymous == null && !ValidateToken())
        {
            context.Result = Json(401, "未授权");
            return;
        }
        base.OnActionExecuting(context);
    }

    Boolean ValidateToken()
    {
        if (ManageProvider.User != null) return true;
        var token = CubeController.GetToken(HttpContext);
        if (token.IsNullOrEmpty()) return false;
        var ap = tokenService.FindBySecret(token);
        if (ap != null && ap.Enable) return true;
        var set = CubeSetting.Current;
        var (app, ex) = tokenService.TryDecodeToken(token, set.JwtSecret);
        return app != null && app.Enable && ex != null;
    }

    /// <summary>列表</summary>
    [HttpGet]
    public ActionResult Get(String typePath, Boolean? enable, String triggerKind)
    {
        var user = ManageProvider.User as IUser;
        if (user == null) return Json(401, "未授权");
        typePath = AutomationPaths.NormalizeTypePath(typePath);
        if (typePath.IsNullOrEmpty()) return Json(400, "typePath 不能为空");
        if (!AutomationAuth.CanViewRuns(user, typePath) && !AutomationAuth.CanConfigure(user, typePath))
            return Json(403, "无权查看");
        var list = EntityAutomation.FindAllByTypePath(typePath);
        if (enable != null) list = list.Where(e => e.Enable == enable).ToList();
        if (!triggerKind.IsNullOrEmpty()) list = list.Where(e => e.TriggerKind.EqualIgnoreCase(triggerKind)).ToList();
        var lastRuns = AutomationFlowLog.FindLastRunTimes(list.Select(e => e.Id));
        var data = list.Select(e =>
        {
            lastRuns.TryGetValue(e.Id, out var last);
            return new
            {
                e.Id,
                e.Name,
                e.Enable,
                e.Priority,
                e.TriggerKind,
                e.Version,
                hasWebhook = !e.HookToken.IsNullOrEmpty(),
                buttonLabel = ButtonLabel(e),
                lastRunTime = last.Year > 2000 ? last : (DateTime?)null,
            };
        }).ToList();
        return Json(0, null, data);
    }

    static String ButtonLabel(EntityAutomation e)
    {
        if (!e.TriggerKind.EqualIgnoreCase("button")) return null;
        try
        {
            var n = JsonNode.Parse(e.TriggerConfig ?? "{}");
            var s = n?["label"]?.ToString();
            return s.IsNullOrEmpty() ? "运行" : s;
        }
        catch { return "运行"; }
    }

    /// <summary>详情</summary>
    [HttpGet("{id:long}")]
    public ActionResult GetById(Int64 id)
    {
        var user = ManageProvider.User as IUser;
        if (user == null) return Json(401, "未授权");
        var e = EntityAutomation.FindById(id);
        if (e == null) return Json(404, "不存在");
        if (!AutomationAuth.CanConfigure(user, e.TypePath)) return Json(403, "无权配置");
        return Json(0, null, e.ToModel());
    }

    /// <summary>创建</summary>
    [HttpPost]
    public ActionResult Post([FromBody] AutomationSaveModel model) => Save(model, true);

    /// <summary>更新。根路径 PUT 与 /Update（PUT/POST）并存：IIS 禁 PUT 时前端可回退 POST /Update，避免误打到创建接口。</summary>
    [HttpPut]
    [HttpPut("Update")]
    [HttpPost("Update")]
    public ActionResult Put([FromBody] AutomationSaveModel model) => Save(model, false);

    ActionResult Save(AutomationSaveModel model, Boolean create)
    {
        var user = ManageProvider.User as IUser;
        if (user == null) return Json(401, "未授权");
        if (model == null) return Json(400, "body 不能为空");
        var typePath = AutomationPaths.NormalizeTypePath(model.TypePath + "");
        if (typePath.IsNullOrEmpty()) return Json(400, "typePath 不能为空");
        if (!AutomationAuth.CanConfigure(user, typePath)) return Json(403, "无权配置");
        var name = (model.Name + "").Trim();
        if (name.Length is < 1 or > 50) return Json(400, "名称长度须为 1–50");
        var kind = (model.TriggerKind + "").Trim().ToLowerInvariant();
        var kinds = new HashSet<String>(StringComparer.OrdinalIgnoreCase)
        {
            "insert","update","delete","insertOrUpdateIf","fieldChange","dateArrive","schedule","button","webhook"
        };
        if (!kinds.Contains(kind)) return Json(400, "非法 TriggerKind");
        if ((model.Actions?.Count ?? 0) > 20) return Json(400, "动作不能超过 20 个");

        // 触发配置保存校验（design §3.2）：schedule cron 可解析；dateArrive 字段为 DateTime；fieldChange 剔除未知名
        var fact = AutomationExecutor.ResolveEntityType(typePath) is { } et ? EntityFactory.CreateFactory(et) : null;
        var fieldNames = fact?.Fields?.Select(f => f.Name).ToList() ?? [];
        var cfgNode = model.TriggerConfig as JsonObject ?? new JsonObject();
        if (kind == "schedule")
        {
            var cron = cfgNode["cron"]?.ToString();
            if (cron.IsNullOrEmpty()) return Json(400, "schedule 必须提供 cron");
            var cronParser = new NewLife.Threading.Cron();
            if (!cronParser.Parse(cron)) return Json(400, "cron 表达式无效");
        }
        if (kind == "dateArrive")
        {
            var field = cfgNode["field"]?.ToString()?.Trim();
            if (field.IsNullOrEmpty()) return Json(400, "dateArrive 必须指定字段");
            var fi = fact?.Fields?.FirstOrDefault(f => f.Name.EqualIgnoreCase(field));
            if (fi == null) return Json(400, $"dateArrive 字段不存在：{field}");
            if (fi.Type != typeof(DateTime) && fi.Type != typeof(DateTime?))
                return Json(400, $"dateArrive 字段必须是日期时间类型：{field}");
        }

        var graph = AutomationGraph.Compile(new AutomationCompileInput
        {
            TriggerKind = kind,
            Filter = model.Filter,
            Actions = model.Actions,
        });
        var selfId = create ? 0 : model.Id;
        var err = AutomationGraph.ValidateForSave(graph, selfId);
        if (err != null) return Json(400, err);

        EntityAutomation entity;
        if (create)
        {
            entity = new EntityAutomation { TypePath = typePath, Version = 1 };
        }
        else
        {
            entity = EntityAutomation.FindById(model.Id);
            if (entity == null) return Json(404, "不存在");
            if (entity.Version != model.Version) return Json(409, "版本冲突");
            entity.Version++;
        }

        entity.Name = name;
        entity.Enable = model.Enable;
        entity.Priority = model.Priority == 0 ? 100 : model.Priority;
        entity.TriggerKind = kind;
        entity.TriggerConfig = NormalizeTrigger(kind, model.TriggerConfig, fieldNames);
        entity.GraphJson = AutomationGraph.ToJson(graph);
        entity.TypePath = typePath;
        if (kind == "webhook" && model.RegenHook) entity.HookToken = null;
        entity.EnsureHookToken();
        if (create) entity.Insert();
        else entity.Update();
        return Json(0, "ok", entity.ToModel());
    }

    static String NormalizeTrigger(String kind, JsonNode cfg, ICollection<String> fieldNames)
    {
        var o = cfg as JsonObject ?? [];
        if (kind == "button")
        {
            var label = (o["label"]?.ToString() + "").Trim();
            if (label.Length > 12) label = label[..12];
            if (label.IsNullOrEmpty()) label = "运行";
            o["label"] = label;
            var rp = o["requirePermission"]?.ToString();
            if (!rp.EqualIgnoreCase("update")) o["requirePermission"] = "detail";
        }
        if (kind == "dateArrive")
        {
            var off = o["offsetMinutes"]?.GetValue<Int32>() ?? 0;
            if (off < -10080) off = -10080;
            if (off > 10080) off = 10080;
            o["offsetMinutes"] = off;
        }
        if (kind == "fieldChange")
        {
            var arr = o["watchFields"] as JsonArray ?? [];
            var clean = new JsonArray();
            var valid = new HashSet<String>(fieldNames ?? [], StringComparer.OrdinalIgnoreCase);
            foreach (var x in arr)
            {
                var s = (x?.ToString() + "").Trim();
                // 剔除不属于该实体的字段名（design §3.2）；字段列表未就绪时保留原始值
                if (valid.Count > 0 && !valid.Contains(s)) continue;
                if (!s.IsNullOrEmpty() && clean.Count < 32) clean.Add(s);
            }
            o["watchFields"] = clean;
        }
        return o.ToJsonString();
    }

    /// <summary>删除</summary>
    [HttpDelete]
    public ActionResult Delete(Int64 id)
    {
        var user = ManageProvider.User as IUser;
        if (user == null) return Json(401, "未授权");
        var e = EntityAutomation.FindById(id);
        if (e == null) return Json(404, "不存在");
        if (!AutomationAuth.CanConfigure(user, e.TypePath)) return Json(403, "无权配置");
        e.Delete();
        return Json(0, "ok");
    }

    /// <summary>运行历史（读系统审计 Log，Action=Automation）</summary>
    [HttpGet("Runs")]
    public ActionResult Runs(String typePath, Int64 automationId = 0, String recordKey = null, Int32 pageIndex = 1, Int32 pageSize = 20)
    {
        var user = ManageProvider.User as IUser;
        if (user == null) return Json(401, "未授权");
        typePath = AutomationPaths.NormalizeTypePath(typePath);
        if (typePath.IsNullOrEmpty()) return Json(400, "typePath 不能为空");
        if (!AutomationAuth.CanViewRuns(user, typePath)) return Json(403, "无权查看");
        var page = new PageParameter { PageIndex = pageIndex <= 0 ? 1 : pageIndex, PageSize = pageSize <= 0 ? 20 : pageSize };
        var list = AutomationFlowLog.SearchRuns(typePath, automationId, recordKey, page);
        var names = new Dictionary<Int64, String>();
        var data = list.Select(item =>
        {
            var log = item.Log;
            var row = item.Row;
            if (!names.TryGetValue(row.AutomationId, out var name))
            {
                name = row.Name.IsNullOrEmpty() ? EntityAutomation.FindById(row.AutomationId)?.Name : row.Name;
                names[row.AutomationId] = name;
            }
            return (Object)new
            {
                id = log.ID,
                automationId = row.AutomationId,
                name,
                typePath,
                recordKey = row.RecordKey,
                triggerKind = row.TriggerKind,
                status = row.Status,
                error = row.Error,
                detail = row.Detail,
                nodes = row.Nodes,
                success = log.Success || (row.Status + "").EqualIgnoreCase("succeeded"),
                createTime = log.CreateTime,
                updateTime = log.CreateTime,
            };
        }).ToList();
        return Json(0, null, data, new { page = new { page.PageIndex, page.PageSize, page.TotalCount } });
    }

    /// <summary>按钮手动跑</summary>
    [HttpPost("Run")]
    public ActionResult Run([FromBody] AutomationRunRequest body)
    {
        var user = ManageProvider.User as IUser;
        if (user == null) return Json(401, "未授权");
        if (body == null) return Json(400, "body 不能为空");
        var rule = EntityAutomation.FindById(body.AutomationId);
        if (rule == null || !rule.Enable) return Json(404, "规则不存在或未启用");
        var req = "detail";
        try
        {
            var n = JsonNode.Parse(rule.TriggerConfig ?? "{}");
            req = n?["requirePermission"]?.ToString() ?? "detail";
        }
        catch { /* default */ }
        if (!AutomationAuth.CanPressButton(user, rule.TypePath, req)) return Json(403, "无权运行");
        var run = AutomationRun.Enqueue(rule, body.RecordKey, "button");
        AutomationWorker.Post(run.Id);
        return Json(0, "ok", new { runId = run.Id });
    }

    /// <summary>可搜索/可写字段元数据。kind=search|all，默认 all</summary>
    [HttpGet("Meta")]
    public ActionResult Meta(String typePath, String kind = "all")
    {
        var user = ManageProvider.User as IUser;
        if (user == null) return Json(401, "未授权");
        typePath = AutomationPaths.NormalizeTypePath(typePath);
        if (typePath.IsNullOrEmpty()) return Json(400, "typePath 不能为空");
        if (!AutomationAuth.CanConfigure(user, typePath)
            && !AutomationAuth.HasPermission(user, typePath, PermissionFlags.Update)
            && !AutomationAuth.HasPermission(user, typePath, PermissionFlags.Insert)
            && !AutomationAuth.HasPermission(user, typePath, PermissionFlags.Detail))
            return Json(403, "无权配置");
        var type = AutomationExecutor.ResolveEntityType(typePath);
        if (type == null) return Json(0, null, Array.Empty<Object>());
        var fact = EntityFactory.CreateFactory(type);
        var fields = fact.Fields.AsEnumerable();
        if (kind.EqualIgnoreCase("search"))
        {
            // 可搜索：非主键、非二进制，排除敏感字段
            fields = fact.Fields.Where(f =>
                !f.PrimaryKey
                && f.Type != typeof(Byte[])
                && !f.Name.EqualIgnoreCase("Password", "Secret", "Salt"));
        }
        var data = fields.Select(f => new
        {
            name = f.Name,
            displayName = f.DisplayName ?? f.Name,
            typeName = f.Type?.Name,
            primaryKey = f.PrimaryKey,
            readOnly = f.ReadOnly,
        }).ToList();
        return Json(0, null, data);
    }

    /// <summary>当前用户有 update/insert 权限的实体列表</summary>
    [HttpGet("Entities")]
    public ActionResult Entities(String permission = "update")
    {
        var user = ManageProvider.User as IUser;
        if (user == null) return Json(401, "未授权");
        var flag = permission.EqualIgnoreCase("insert", "add")
            ? PermissionFlags.Insert
            : PermissionFlags.Update;
        var list = new List<(String typePath, String displayName, String name)>();
        var seen = new HashSet<String>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in EntityPageRegistry.GetAll())
        {
            var url = kv.Value?.Url;
            if (url.IsNullOrEmpty()) continue;
            var typePath = AutomationPaths.NormalizeTypePath(url);
            if (typePath.IsNullOrEmpty() || !seen.Add(typePath)) continue;
            if (!AutomationAuth.HasPermission(user, typePath, flag)) continue;
            var display = kv.Key.GetDisplayName().IsNullOrEmpty() ? kv.Key.Name : kv.Key.GetDisplayName();
            list.Add((typePath, display, kv.Key.Name));
        }
        var data = list.OrderBy(e => e.displayName).Select(e => new
        {
            e.typePath,
            e.displayName,
            e.name,
        }).ToList();
        return Json(0, null, data);
    }

    /// <summary>通知接收人搜索。kind=user|role|department</summary>
    [HttpGet("Recipients")]
    public ActionResult Recipients(String kind, String key = null)
    {
        var user = ManageProvider.User as IUser;
        if (user == null) return Json(401, "未授权");
        kind = (kind + "").Trim().ToLowerInvariant();
        if (kind is "user" or "users")
        {
            var exp = UserX._.Enable == true;
            if (!key.IsNullOrEmpty())
                exp &= UserX._.Name.Contains(key) | UserX._.DisplayName.Contains(key) | UserX._.Code.Contains(key) | UserX._.Mobile.Contains(key);
            var list = UserX.FindAll(exp, null, null, 0, 50);
            if (list.Count == 0 && key.IsNullOrEmpty())
                list = UserX.FindAll(null, null, null, 0, 50);
            return Json(0, null, list.Select(e => new
            {
                id = e.ID,
                name = e.Name,
                displayName = e.DisplayName.IsNullOrEmpty() ? e.Name : e.DisplayName,
            }).ToList());
        }
        if (kind is "role" or "roles")
        {
            var exp = Role._.Enable == true;
            if (!key.IsNullOrEmpty())
                exp &= Role._.Name.Contains(key);
            var list = Role.FindAll(exp, null, null, 0, 50);
            if (list.Count == 0 && key.IsNullOrEmpty())
                list = Role.FindAll(null, null, null, 0, 50);
            return Json(0, null, list.Select(e => new
            {
                id = e.ID,
                name = e.Name,
                displayName = e.Name,
            }).ToList());
        }
        if (kind is "department" or "departments" or "dept")
        {
            var exp = Department._.Enable == true;
            if (!key.IsNullOrEmpty())
                exp &= Department._.Name.Contains(key) | Department._.Code.Contains(key) | Department._.FullName.Contains(key);
            var list = Department.FindAll(exp, null, null, 0, 50);
            if (list.Count == 0 && key.IsNullOrEmpty())
                list = Department.FindAll(null, null, null, 0, 50);
            return Json(0, null, list.Select(e => new
            {
                id = e.ID,
                name = e.Name,
                displayName = e.FullName.IsNullOrEmpty() ? e.Name : e.FullName,
            }).ToList());
        }
        return Json(400, "kind 须为 user/role/department");
    }

    /// <summary>当前用户站内信列表</summary>
    [HttpGet("Inbox")]
    public ActionResult Inbox(Int32 pageIndex = 1, Int32 pageSize = 20, Boolean? unread = null)
    {
        var user = ManageProvider.User as IUser;
        if (user == null) return Json(401, "未授权");
        if (pageIndex < 1) pageIndex = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 50) pageSize = 50;
        var page = new PageParameter { PageIndex = pageIndex, PageSize = pageSize, Sort = "Id", Desc = true, RetrieveTotalCount = true };
        // Search 第 5 参是 read；接口入参 unread=true 表示未读，需取反
        Boolean? read = unread == null ? null : !unread.Value;
        var list = NotificationRecord.Search(0, "InApp", user.ID, null, read, true, DateTime.MinValue, DateTime.MinValue, null, page);
        var data = list.Select(e => new
        {
            e.Id,
            e.Title,
            e.Content,
            e.Read,
            e.ReadTime,
            e.CreateTime,
            e.Action,
            e.Channel,
        }).ToList();
        return Json(0, null, data, new { page = new { page.PageIndex, page.PageSize, page.TotalCount } });
    }

    /// <summary>未读站内信数量</summary>
    [HttpGet("Inbox/UnreadCount")]
    public ActionResult InboxUnreadCount()
    {
        var user = ManageProvider.User as IUser;
        if (user == null) return Json(401, "未授权");
        var page = new PageParameter { PageIndex = 1, PageSize = 1, RetrieveTotalCount = true };
        NotificationRecord.Search(0, "InApp", user.ID, null, false, true, DateTime.MinValue, DateTime.MinValue, null, page);
        return Json(0, null, new { count = page.TotalCount });
    }

    /// <summary>标记站内信已读（id=0 且 all=true 则全部已读）</summary>
    [HttpPost("Inbox/Read")]
    public ActionResult MarkInboxRead([FromBody] InboxReadModel model)
    {
        var user = ManageProvider.User as IUser;
        if (user == null) return Json(401, "未授权");
        model ??= new InboxReadModel();
        var now = DateTime.Now;
        if (model.All)
        {
            var page = new PageParameter { PageIndex = 1, PageSize = 200, Sort = "Id", Desc = true };
            var list = NotificationRecord.Search(0, "InApp", user.ID, null, false, true, DateTime.MinValue, DateTime.MinValue, null, page);
            foreach (var e in list)
            {
                if (e.Read) continue;
                e.Read = true;
                e.ReadTime = now;
                e.Update();
            }
            return Json(0, "ok");
        }
        if (model.Id <= 0) return Json(400, "id 无效");
        var rec = NotificationRecord.FindById(model.Id);
        if (rec == null || rec.UserId != user.ID) return Json(404, "不存在");
        if (!rec.Read)
        {
            rec.Read = true;
            rec.ReadTime = now;
            rec.Update();
        }
        return Json(0, "ok");
    }

    /// <summary>入站 Webhook</summary>
    [AllowAnonymous]
    [HttpPost("Hook/{token}")]
    public async Task<ActionResult> Hook(String token)
    {
        if (token.IsNullOrEmpty()) return Json(404, "不存在");
        var rule = EntityAutomation.FindByHookToken(token);
        if (rule == null || !rule.Enable || !rule.TriggerKind.EqualIgnoreCase("webhook"))
            return Json(404, "不存在");
        // 先校验再限流，避免无效 token 撑大限流字典
        if (!AutomationHookRate.TryAcquire(token)) return Json(429, "过于频繁");
        Request.EnableBuffering();
        using var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
        var raw = await reader.ReadToEndAsync();
        Request.Body.Position = 0;
        JsonObject payload = [];
        try { payload = JsonNode.Parse(String.IsNullOrEmpty(raw) ? "{}" : raw) as JsonObject ?? []; }
        catch { return Json(400, "非法 JSON"); }
        Boolean requireSig = false;
        try { requireSig = JsonNode.Parse(rule.TriggerConfig ?? "{}")?["requireSignature"]?.GetValue<Boolean>() ?? false; }
        catch { /* */ }
        if (requireSig)
        {
            var sig = Request.Headers["X-Cube-Signature"].ToString();
            if (!sig.EqualIgnoreCase(AutomationHookRate.HmacHex(rule.HookToken, raw ?? ""))) return Json(401, "签名错误");
        }
        var run = AutomationRun.Enqueue(rule, payload["recordKey"]?.ToString(), "webhook");
        if (AutomationRuntime.Immediate) AutomationExecutor.Execute(run, payload);
        else AutomationWorker.Post(run.Id);
        return Json(0, "ok", new { runId = run.Id });
    }
}

/// <summary>保存模型</summary>
public class AutomationSaveModel
{
    /// <summary>编号</summary>
    public Int64 Id { get; set; }
    /// <summary>实体</summary>
    public String TypePath { get; set; }
    /// <summary>名称</summary>
    public String Name { get; set; }
    /// <summary>启用</summary>
    public Boolean Enable { get; set; } = true;
    /// <summary>优先级</summary>
    public Int32 Priority { get; set; } = 100;
    /// <summary>触发</summary>
    public String TriggerKind { get; set; }
    /// <summary>触发配置</summary>
    public JsonNode TriggerConfig { get; set; }
    /// <summary>版本</summary>
    public Int32 Version { get; set; }
    /// <summary>条件</summary>
    public ViewFilterDto Filter { get; set; }
    /// <summary>动作</summary>
    public List<ActionDraft> Actions { get; set; }
    /// <summary>重新生成 Webhook 令牌</summary>
    public Boolean RegenHook { get; set; }
}

/// <summary>手动运行</summary>
public class AutomationRunRequest
{
    /// <summary>规则</summary>
    public Int64 AutomationId { get; set; }
    /// <summary>记录主键</summary>
    public String RecordKey { get; set; }
}

/// <summary>站内信已读</summary>
public class InboxReadModel
{
    /// <summary>记录 Id；与 All 二选一</summary>
    public Int64 Id { get; set; }
    /// <summary>全部已读</summary>
    public Boolean All { get; set; }
}
