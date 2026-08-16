using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Text;
using System.Text.Json.Nodes;
using NewLife.Cube.AI;
using NewLife.Cube.Entity;
using NewLife.Cube.Services;
using NewLife.Log;
using XCode;
using XCode.Membership;

namespace NewLife.Cube.Automation;

/// <summary>动作实现</summary>
public static class AutomationActions
{
    static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(15) };

    /// <summary>执行一个动作节点</summary>
    public static Boolean Run(AutomationContext ctx, String type, JsonObject data, out String detail)
    {
        detail = type;
        try
        {
            return type.ToLowerInvariant() switch
            {
                "notify" => Notify(ctx, data, out detail),
                "updaterecord" => UpdateRecord(ctx, data, out detail),
                "createrecord" => CreateRecord(ctx, data, out detail),
                "findrecords" => FindRecords(ctx, data, out detail),
                "httprequest" => HttpRequest(ctx, data, out detail),
                "runautomation" => RunAutomation(ctx, data, out detail),
                "addcomment" => AddComment(ctx, data, out detail),
                "aitext" => AiText(ctx, data, out detail),
                _ => Fail($"未知动作 {type}", out detail),
            };
        }
        catch (Exception ex)
        {
            detail = ex.Message;
            XTrace.WriteException(ex);
            return false;
        }
    }

    static Boolean Fail(String msg, out String detail) { detail = msg; return false; }

    static IEntity TargetEntity(AutomationContext ctx, JsonObject data)
    {
        var t = data["target"]?.ToString();
        if (t.EqualIgnoreCase("found")) return ctx.FoundCurrent ?? ctx.Found?.FirstOrDefault();
        if (t.EqualIgnoreCase("created")) return ctx.Created;
        return ctx.Current;
    }

    static Boolean Notify(AutomationContext ctx, JsonObject data, out String detail)
    {
        var channel = data["channel"]?.ToString();
        if (channel.IsNullOrEmpty()) channel = "InApp";
        var title = AutomationExecutor.ApplyTemplate(data["title"]?.ToString() ?? "", ctx).Cut(200);
        var body = AutomationExecutor.ApplyTemplate(data["body"]?.ToString() ?? "", ctx).Cut(2000);
        var to = data["to"] as JsonObject;
        var userIds = ResolveRecipientUserIds(to, ctx, data);
        if (userIds.Count == 0)
        {
            detail = "notify skip: no recipients";
            return true;
        }
        var n = 0;
        var okCount = 0;
        var failCount = 0;
        foreach (var userId in userIds)
        {
            if (n++ >= 200) break;
            String err = null;
            Boolean ok;
            switch (channel.ToLowerInvariant())
            {
                case "mail": ok = SendMail(ctx, userId, title, body, out err); break;
                case "sms": ok = SendSms(ctx, userId, body, out err); break;
                case "dingtalk":
                case "wecom": ok = SendBot(ctx, data, userId, channel, title, body, out err); break;
                default: ok = true; break; // InApp 写记录即送达
            }
            if (ok) okCount++;
            else failCount++;

            var rec = new NotificationRecord
            {
                TenantId = ctx.Rule.TenantId,
                Action = "Notify",
                Channel = channel,
                UserId = userId,
                Title = title,
                Content = body,
                Success = ok,
            };
            if (!ok) rec.Result = err.Cut(200);
            rec.Insert();
        }
        detail = "notify " + channel + " ok=" + okCount + " fail=" + failCount;
        // 渠道失败不中止后续节点（design §5.3）；全部失败时 ok=false 写入轨迹
        return failCount == 0;
    }

    /// <summary>Mail 渠道：MailConfig(Notify) + SmtpClient 实际发送；无配置/无邮箱/异常 → false</summary>
    static Boolean SendMail(AutomationContext ctx, Int32 userId, String title, String body, out String err)
    {
        err = null;
        var svc = AutomationRuntime.Services?.GetService<MailService>();
        var config = svc?.GetConfig(ctx.Rule.TenantId, "Notify");
        if (config == null) { err = "无邮件配置"; return false; }
        var user = User.FindByID(userId);
        var mail = user?.Mail;
        if (mail.IsNullOrEmpty()) { err = "用户未填写邮箱"; return false; }
        try
        {
            using var msg = new MailMessage
            {
                From = new MailAddress(config.FromMail, config.FromName),
                Subject = title,
                Body = body,
                IsBodyHtml = false,
            };
            msg.To.Add(mail);
            using var smtp = new SmtpClient(config.Server, config.Port > 0 ? config.Port : 25)
            {
                EnableSsl = config.EnableSsl,
                Credentials = config.UserName.IsNullOrEmpty() ? null : new NetworkCredential(config.UserName, config.Password),
            };
            smtp.Send(msg);
            return true;
        }
        catch (Exception ex)
        {
            err = ex.Message.Cut(200);
            return false;
        }
    }

    /// <summary>Sms 渠道：SmsConfig(Notify) 仅支持验证码模板短信，暂无自由文本通道 → 明确失败不假装成功</summary>
    static Boolean SendSms(AutomationContext ctx, Int32 userId, String body, out String err)
    {
        err = null;
        var svc = AutomationRuntime.Services?.GetService<SmsService>();
        var config = svc?.GetConfig(ctx.Rule.TenantId, "Notify");
        if (config == null) { err = "无短信配置"; return false; }
        err = "短信通知通道未接入（SmsConfig 仅支持验证码模板）";
        return false;
    }

    /// <summary>钉钉/企微渠道：节点 data 提供 webhookUrl 时 POST text；未配置则降级（design §5.3：只记记录且 ok=false）</summary>
    static Boolean SendBot(AutomationContext ctx, JsonObject data, Int32 userId, String channel, String title, String body, out String err)
    {
        err = null;
        var url = AutomationExecutor.ApplyTemplate(data["webhookUrl"]?.ToString() ?? "", ctx);
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            err = $"未配置 {channel} Webhook";
            return false;
        }
        try
        {
            var text = title.IsNullOrEmpty() ? body : title + "\n" + body;
            var payload = new JsonObject
            {
                ["msgtype"] = "text",
                ["text"] = new JsonObject { ["content"] = text },
            };
            using var req = new HttpRequestMessage(HttpMethod.Post, uri)
            {
                Content = new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json"),
            };
            using var resp = Http.Send(req);
            if (resp.IsSuccessStatusCode) return true;
            err = "HTTP " + (Int32)resp.StatusCode;
            return false;
        }
        catch (Exception ex)
        {
            err = ex.Message.Cut(200);
            return false;
        }
    }

    /// <summary>
    /// 解析接收人：kind=users|roles|departments 三选一；兼容旧多字段与 mode=userId|field。
    /// </summary>
    static HashSet<Int32> ResolveRecipientUserIds(JsonObject to, AutomationContext ctx, JsonObject data)
    {
        var ids = new HashSet<Int32>();
        if (to == null) return ids;

        var kind = (to["kind"]?.ToString() ?? "").Trim().ToLowerInvariant();
        if (kind.IsNullOrEmpty())
        {
            // 推断：仅一个非空数组时视为该 kind
            var hasU = ReadIntArray(to["users"]).Any(x => x > 0);
            var hasR = ReadIntArray(to["roles"]).Any(x => x > 0);
            var hasD = ReadIntArray(to["departments"]).Any(x => x > 0);
            var n = (hasU ? 1 : 0) + (hasR ? 1 : 0) + (hasD ? 1 : 0);
            if (n == 1)
                kind = hasU ? "users" : hasR ? "roles" : "departments";
        }

        void AddUsers()
        {
            foreach (var id in ReadIntArray(to["users"]))
                if (id > 0) ids.Add(id);
        }
        void AddRoles()
        {
            var roleIds = ReadIntArray(to["roles"]).Where(x => x > 0).Distinct().ToArray();
            if (roleIds.Length == 0) return;
            var exp = User._.RoleID.In(roleIds);
            foreach (var rid in roleIds)
                exp |= User._.RoleIds.Contains("," + rid + ",");
            exp &= User._.Enable == true;
            foreach (var u in User.FindAll(exp, null, null, 0, 500))
                if (u.ID > 0) ids.Add(u.ID);
        }
        void AddDepts()
        {
            var deptIds = ReadIntArray(to["departments"]).Where(x => x > 0).Distinct().ToArray();
            if (deptIds.Length == 0) return;
            var exp = User._.DepartmentID.In(deptIds) & User._.Enable == true;
            foreach (var u in User.FindAll(exp, null, null, 0, 500))
                if (u.ID > 0) ids.Add(u.ID);
        }

        if (kind is "user" or "users") AddUsers();
        else if (kind is "role" or "roles") AddRoles();
        else if (kind is "department" or "departments" or "dept") AddDepts();
        else
        {
            // 无 kind：兼容旧数据，三者并集
            AddUsers();
            AddRoles();
            AddDepts();
        }

        // 兼容旧版 to.mode
        var mode = to["mode"]?.ToString();
        if (mode.EqualIgnoreCase("userId"))
        {
            var userId = to["userId"]?.GetValue<Int32>() ?? 0;
            if (userId > 0) ids.Add(userId);
        }
        else if (mode.EqualIgnoreCase("field"))
        {
            var field = to["field"]?.ToString();
            var ent = TargetEntity(ctx, data) ?? ctx.Current;
            if (ent != null && !field.IsNullOrEmpty())
            {
                var userId = ent[field].ToInt();
                if (userId > 0) ids.Add(userId);
            }
        }

        return ids;
    }

    static IEnumerable<Int32> ReadIntArray(JsonNode node)
    {
        var list = new List<Int32>();
        if (node is JsonArray arr)
        {
            foreach (var item in arr)
            {
                if (item == null) continue;
                try { list.Add(item.GetValue<Int32>()); }
                catch
                {
                    if (Int32.TryParse(item.ToString(), out var id)) list.Add(id);
                }
            }
            return list;
        }
        if (node != null)
        {
            var s = node.ToString();
            if (!s.IsNullOrEmpty())
            {
                foreach (var part in s.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (Int32.TryParse(part.Trim().Trim('"'), out var id)) list.Add(id);
                }
            }
        }
        return list;
    }

    static Boolean UpdateRecord(AutomationContext ctx, JsonObject data, out String detail)
    {
        var entity = TargetEntity(ctx, data);
        if (entity == null) { detail = "无目标记录"; return false; }
        ApplyFields(entity, data["fields"] as JsonArray, ctx);
        entity.Update();
        detail = "updated";
        return true;
    }

    static Boolean CreateRecord(AutomationContext ctx, JsonObject data, out String detail)
    {
        var typePath = data["typePath"]?.ToString() ?? ctx.Rule.TypePath;
        var type = AutomationExecutor.ResolveEntityType(typePath);
        if (type == null) { detail = "未知 typePath"; return false; }
        var fact = EntityFactory.CreateFactory(type);
        var entity = fact.Create() as IEntity;
        ApplyFields(entity, data["fields"] as JsonArray, ctx);
        entity.Insert();
        ctx.Created = entity;
        detail = "created";
        return true;
    }

    static Boolean FindRecords(AutomationContext ctx, JsonObject data, out String detail)
    {
        var typePath = data["typePath"]?.ToString() ?? ctx.Rule.TypePath;
        var type = AutomationExecutor.ResolveEntityType(typePath);
        if (type == null) { detail = "未知 typePath"; return false; }
        var fact = EntityFactory.CreateFactory(type);
        var limit = data["limit"]?.GetValue<Int32>() ?? 20;
        if (limit < 1) limit = 1;
        if (limit > 100) limit = 100;
        var filter = (data["filter"] as JsonObject).DeserializeFilter();
        var list = fact.FindAll(null, null, null, 0, Math.Min(500, limit * 5));
        ctx.Found = list.Where(e => AutomationFilter.Match(e, filter)).Take(limit).ToList();
        detail = "found " + ctx.Found.Count;
        return true;
    }

    static Boolean HttpRequest(AutomationContext ctx, JsonObject data, out String detail)
    {
        var url = AutomationExecutor.ApplyTemplate(data["url"]?.ToString() ?? "", ctx);
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            detail = "非法 URL";
            return false;
        }
        var method = (data["method"]?.ToString() ?? "POST").ToUpperInvariant();
        using var req = new HttpRequestMessage(new HttpMethod(method), uri);
        var headers = data["headers"] as JsonObject;
        if (headers != null)
        {
            var n = 0;
            foreach (var kv in headers)
            {
                if (n++ >= 16) break;
                if (kv.Key.EqualIgnoreCase("Authorization")) continue; // 不进 trace；仍发送
                req.Headers.TryAddWithoutValidation(kv.Key, AutomationExecutor.ApplyTemplate(kv.Value?.ToString(), ctx));
            }
            if (headers["Authorization"] != null)
                req.Headers.TryAddWithoutValidation("Authorization", AutomationExecutor.ApplyTemplate(headers["Authorization"]?.ToString(), ctx));
        }
        var body = data["body"]?.ToString();
        if (!body.IsNullOrEmpty() && method is not "GET")
            req.Content = new StringContent(AutomationExecutor.ApplyTemplate(body, ctx), Encoding.UTF8, "application/json");
        var resp = Http.Send(req);
        var text = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        if (text != null && text.Length > 64 * 1024) text = text[..(64 * 1024)];
        detail = ((Int32)resp.StatusCode) + " " + (text ?? "").Cut(200);
        return resp.IsSuccessStatusCode;
    }

    static Boolean RunAutomation(AutomationContext ctx, JsonObject data, out String detail)
    {
        var id = data["automationId"]?.GetValue<Int64>() ?? 0;
        if (id <= 0 || id == ctx.Rule.Id) { detail = "非法 automationId"; return false; }
        var rule = EntityAutomation.FindById(id);
        if (rule == null) { detail = "目标规则不存在"; return false; }
        var depth = ctx.Run.Depth + 1;
        if (depth >= AutomationRuntime.MaxDepth)
        {
            var failed = AutomationRun.Enqueue(rule, ctx.Run.RecordKey, "runAutomation", depth, DateTime.MinValue, "failed");
            failed.Error = "超过最大深度";
            failed.Update();
            detail = "超过最大深度";
            return false;
        }
        var run = AutomationRun.Enqueue(rule, ctx.Run.RecordKey, "runAutomation", depth);
        AutomationWorker.Post(run.Id);
        detail = "enqueued " + run.Id;
        return true;
    }

    static Boolean AddComment(AutomationContext ctx, JsonObject data, out String detail)
    {
        var content = AutomationExecutor.ApplyTemplate(data["content"]?.ToString() ?? "", ctx);
        if (content.IsNullOrEmpty()) { detail = "空评论"; return false; }
        var entity = TargetEntity(ctx, data) ?? ctx.Current;
        var keyStr = AutomationPaths.RecordKey(entity);
        var key = keyStr.ToLong();
        if (key <= 0) key = ctx.Run.RecordKey.ToLong();
        if (key <= 0) { detail = "无目标记录"; return false; }
        var typePath = ctx.Rule.TypePath;
        var t = data["typePath"]?.ToString();
        if (!t.IsNullOrEmpty()) typePath = t;
        var user = ManageProvider.User;
        EntityComment.AddComment(user != null && user.ID > 0 ? user.ID : 1, user?.Name ?? "automation", typePath, key, content);
        detail = "commented";
        return true;
    }

    static Boolean AiText(AutomationContext ctx, JsonObject data, out String detail)
    {
        var ai = AutomationRuntime.Services?.GetService<IAIService>();
        if (ai == null || !CubeSetting.Current.AISwitch) { detail = "AI 未启用"; return false; }
        var prompt = AutomationExecutor.ApplyTemplate(data["prompt"]?.ToString() ?? "", ctx);
        var text = ai.ChatAsync(prompt, null).GetAwaiter().GetResult();
        var field = data["outputField"]?.ToString();
        if (!field.IsNullOrEmpty() && ctx.Current != null)
        {
            ctx.Current[field] = text;
            ctx.Current.Update();
        }
        detail = (text + "").Cut(200);
        return true;
    }

    static void ApplyFields(IEntity entity, JsonArray fields, AutomationContext ctx)
    {
        if (entity == null || fields == null) return;
        var n = 0;
        foreach (var f in fields.OfType<JsonObject>())
        {
            if (n++ >= 32) break;
            var name = f["name"]?.ToString();
            if (name.IsNullOrEmpty()) continue;
            var val = AutomationExecutor.ApplyTemplate(f["value"]?.ToString() ?? "", ctx);
            entity[name] = val;
        }
    }
}
