using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewLife.AI.Models;
using NewLife.Cube.ViewModels;
using NewLife.AI.Tools;
using NewLife.Collections;
using NewLife.Common;
using NewLife.Configuration;
using NewLife.Cube.AI;
using NewLife.Reflection;
using NewLife.Serialization;

namespace NewLife.Cube;

/// <summary>设置控制器</summary>
public class ConfigController<TConfig> : ObjectController<TConfig>, IFormAiContext where TConfig : Config<TConfig>, new()
{
    /// <summary>要展现和修改的对象</summary>
    protected override TConfig Value
    {
        get
        {
            return Config<TConfig>.Current;
        }
        set
        {
            if (value != null)
            {
                var cfg = Config<TConfig>.Current;
                //value.ConfigFile = cfg.ConfigFile;
                //value.Save();
                cfg.Copy(value);
                cfg.Save();
            }
            //Config<TConfig>.Current = value;
        }
    }

    #region AI 表单
    // IFormAiContext：配置表单页（如魔方设置）经全局 AiController 注册 get_form_schema / fill_form 工具，支持 AI 填表。
    // 配置页虽非实体控制器，但其表单字段可由 GetMembers 枚举，填表能力与实体表单对齐（前端 applyFormValues 按 name 回填）。

    /// <summary>获取配置字段，按当前请求语言本地化</summary>
    /// <param name="kind">视图类型</param>
    /// <returns>本地化后的字段集合</returns>
    [AllowAnonymous]
    [HttpGet]
    public override IList<DataField> GetFields(ViewKinds kind) => DataField.Localize(base.GetFields(kind));

    /// <summary>获取当前配置表单的字段结构（含当前值与敏感过滤），供 AI 生成填表值</summary>
    /// <param name="mode">表单模式：add 新增 / edit 编辑，默认 edit（配置页恒为编辑既有配置）</param>
    /// <returns>表单字段结构 JSON：<c>{entity, description, mode, fields:[{name,displayName,type,description,value,fillable,...}]}</c></returns>
    [HttpGet]
    public virtual String GetFormSchema(String mode = "edit")
    {
        var set = Config<TConfig>.Current;
        var fields = new List<AiFormField>();
        foreach (var fi in DataField.Localize(GetMembers(typeof(TConfig))))
        {
            // 敏感字段不向 AI 暴露（ApiKey/Secret/Token/连接串等）
            if (AiFormHelper.IsSensitiveField(fi.Name)) continue;

            // 配置字段本地化后 DisplayName/Description 已就位，CanWrite 判断只读
            var pi = typeof(TConfig).GetProperty(fi.Name);
            var field = new AiFormField
            {
                Name = fi.Name,
                DisplayName = fi.DisplayName,
                Type = fi.Type?.Name ?? "unknown",
                ItemType = fi.ItemType,
                Description = fi.Description,
                Required = fi.Required,
                Length = fi.Length,
            };
            if (fi.Type != null && fi.Type.IsEnum)
                field.EnumValues = Enum.GetNames(fi.Type).ToList();

            // 可填：属性可写且非只读（敏感已在上面排除）
            field.Fillable = !fi.ReadOnly && (pi?.CanWrite ?? false);

            // 编辑模式并入当前值，供 AI 基于现状补全/修正（空值归一 null 引导 AI 生成）
            var v = set.GetValue(fi.Name);
            field.Value = v is String s && s.IsNullOrEmpty() ? null : v;

            fields.Add(field);
        }

        var name = L.T(typeof(TConfig).GetDisplayName() ?? typeof(TConfig).Name);
        return new { entity = name, description = L.T("System settings, grouped by category"), mode, fields }.ToJson();
    }

    /// <summary>生成配置字段值并回填前端表单。不写数据库，由用户确认后提交</summary>
    /// <param name="values">字段值 JSON 字符串，键为字段名，值为要填入的值，如 {"AIModel":"qwen3.7-plus"}</param>
    /// <param name="mode">表单模式：add 新增 / edit 编辑，默认 edit</param>
    /// <returns>工具结果，含回填值（供前端 applyFormValues 回填）与给 LLM 的说明</returns>
    [HttpPost]
    public virtual IToolResult FillForm(String values, String mode = "edit")
    {
        // 参数解析：兼容 JSON 对象与扁平键值数组两种格式（部分 LLM 会生成扁平数组），解析失败返回友好错误而非抛异常
        var dic = AiFormHelper.ParseFieldValues(values);
        if (dic == null || dic.Count == 0)
        {
            var err = new { kind = "fill_form", count = 0, message = L.T("No valid field value dictionary was received") }.ToJson();
            return new ToolResult(ToolContent.ForUser(err), ToolContent.ForLlm(L.T("[fill_form invalid argument] No valid field value dictionary was received. Call get_form_schema first to get the field structure, then pass the values parameter as {\"field\":value}.")))
            {
                IsError = true
            };
        }

        var fields = GetMembers(typeof(TConfig)).ToList();
        var rs = new Dictionary<String, Object?>();
        var skipped = new List<String>();
        var errors = new List<String>();
        foreach (var kv in dic)
        {
            var field = fields.FirstOrDefault(f => f.Name.EqualIgnoreCase(kv.Key));
            if (field == null)
            {
                skipped.Add(kv.Key);
                continue;
            }
            // 敏感/只读字段不允许 AI 填写
            if (AiFormHelper.IsSensitiveField(field.Name) || field.ReadOnly)
            {
                skipped.Add(kv.Key);
                continue;
            }
            var val = AiFormHelper.CoerceValue(kv.Value, field);
            if (val == null)
            {
                errors.Add(kv.Key);
                continue;
            }
            // 空字符串视为 AI 未生成值，不预填
            if (val is String s && s.IsNullOrEmpty())
            {
                skipped.Add(kv.Key);
                continue;
            }
            rs[field.Name] = val;
        }

        var user = new { kind = "fill_form", values = rs, count = rs.Count, skipped, errors }.ToJson();
        var llm = L.T("Generated {0} configuration field values (skipped {1}, failed {2}) and filled them into the form. Ask the user to review before submitting.", rs.Count, skipped.Count, errors.Count);
        return new ToolResult(ToolContent.ForUser(user), ToolContent.ForLlm(llm));
    }

    /// <summary>构建 AI 对话系统提示词（配置表单上下文），引导 LLM 使用 get_form_schema / fill_form</summary>
    /// <param name="req">对话请求（含页面类型/模式等上下文）</param>
    /// <returns>系统提示词</returns>
    [HttpPost]
    public virtual String BuildFormSystemPrompt(AiChatRequest req)
    {
        var sysName = SysConfig.Current?.DisplayName;
        if (sysName.IsNullOrEmpty()) sysName = L.T("Cube admin console");

        var sb = Pool.StringBuilder.Get();
        sb.AppendLine(L.T("You are the AI assistant of {0}, helping an administrator work on the current page.", sysName));
        sb.AppendLine();
        sb.AppendLine(L.T("The current page is a system configuration form (such as Cube settings); you can help review and fill in configuration items."));
        sb.AppendLine();
        sb.AppendLine(L.T("Available tools: get_form_schema / fill_form / get_page_context / get_system_info / run_js (see the function definitions; call them when needed)"));
        sb.AppendLine();
        sb.AppendLine(L.T("Rules:"));
        sb.AppendLine(L.T("1. Answer in the same language as the user's question, concisely and professionally"));
        sb.AppendLine(L.T("2. When the user asks to create, fill in or complete configuration, call get_form_schema first to inspect the items, then call fill_form to generate values (invent reasonable values for empty writable fields, keep existing values unless the user asks otherwise, and never fabricate or modify sensitive settings such as ApiKey/Secret/Token); finally ask the user to review before submitting"));
        sb.AppendLine(L.T("3. When the user asks about the content or current state of the page, call get_page_context, then give your analysis and conclusion"));
        sb.AppendLine(L.T("4. When the user asks about system status or diagnostics, call get_system_info"));
        sb.AppendLine(L.T("5. When the user asks to read or operate page elements, you may call run_js; explain any write operation (changing page content, submitting forms) to the user before running it"));
        sb.AppendLine(L.T("6. Never invent data; ask the user for clarification when information is insufficient"));

        return sb.Return(true);
    }
    #endregion

    ///// <summary>已重载</summary>
    ///// <param name="filterContext"></param>
    //public override void OnActionExecuting(ActionExecutingContext filterContext)
    //{
    //    //var fi = XmlConfig<TConfig>._.ConfigFile;
    //    //if (fi.IsNullOrEmpty() || !fi.AsFile().Exists) throw new Exception("无法找到配置文件 {0}".F(fi));

    //    var bs = this.Bootstrap();
    //    bs.MaxColumn = 1;
    //    bs.LabelWidth = 3;

    //    base.OnActionExecuting(filterContext);
    //}
}