using System;
using Microsoft.AspNetCore.Mvc.Filters;
using NewLife;
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

    /// <summary>获取当前配置表单的字段结构（含当前值与敏感过滤），供 AI 生成填表值</summary>
    /// <param name="mode">表单模式：add 新增 / edit 编辑，默认 edit（配置页恒为编辑既有配置）</param>
    /// <returns>表单字段结构 JSON：<c>{entity, description, mode, fields:[{name,displayName,type,description,value,fillable,...}]}</c></returns>
    public virtual String GetFormSchema(String mode = "edit")
    {
        var set = Config<TConfig>.Current;
        var fields = new List<AiFormField>();
        foreach (var fi in GetMembers(typeof(TConfig)))
        {
            // 敏感字段不向 AI 暴露（ApiKey/Secret/Token/连接串等）
            if (AiFormHelper.IsSensitiveField(fi.Name)) continue;

            // 反射读取属性元数据：GetMembers 把 [Description] 合并进了 DisplayName，需单独读取说明；CanWrite 判断只读
            var pi = typeof(TConfig).GetProperty(fi.Name);
            var field = new AiFormField
            {
                Name = fi.Name,
                DisplayName = fi.DisplayName,
                Type = fi.Type?.Name ?? "unknown",
                ItemType = fi.ItemType,
                Description = pi?.GetDescription() ?? fi.Description,
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

        var name = typeof(TConfig).GetDisplayName() ?? typeof(TConfig).Name;
        return new { entity = name, description = "系统配置，可按分类查看各项设置项", mode, fields }.ToJson();
    }

    /// <summary>生成配置字段值并回填前端表单。不写数据库，由用户确认后提交</summary>
    /// <param name="values">字段值 JSON 字符串，键为字段名，值为要填入的值，如 {"AIModel":"qwen3.7-plus"}</param>
    /// <param name="mode">表单模式：add 新增 / edit 编辑，默认 edit</param>
    /// <returns>工具结果，含回填值（供前端 applyFormValues 回填）与给 LLM 的说明</returns>
    public virtual IToolResult FillForm(String values, String mode = "edit")
    {
        // 参数解析：兼容 JSON 对象与扁平键值数组两种格式（部分 LLM 会生成扁平数组），解析失败返回友好错误而非抛异常
        var dic = AiFormHelper.ParseFieldValues(values);
        if (dic == null || dic.Count == 0)
        {
            var err = new { kind = "fill_form", count = 0, message = "未收到有效的字段值字典" }.ToJson();
            return new ToolResult(ToolContent.ForUser(err), ToolContent.ForLlm("[fill_form 参数错误] 未收到有效的字段值字典。请先调用 get_form_schema 获取字段结构，再以 {\"字段名\":值} 形式传入 values 参数。"))
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
        var llm = $"已生成 {rs.Count} 个配置字段值（跳过 {skipped.Count} 个，失败 {errors.Count} 个），已预填到表单，请提示用户检查后提交。";
        return new ToolResult(ToolContent.ForUser(user), ToolContent.ForLlm(llm));
    }

    /// <summary>构建 AI 对话系统提示词（配置表单上下文），引导 LLM 使用 get_form_schema / fill_form</summary>
    /// <param name="req">对话请求（含页面类型/模式等上下文）</param>
    /// <returns>系统提示词</returns>
    public virtual String BuildFormSystemPrompt(AiChatRequest req)
    {
        var sysName = SysConfig.Current?.DisplayName;
        if (sysName.IsNullOrEmpty()) sysName = "魔方后台管理系统";

        var sb = Pool.StringBuilder.Get();
        sb.AppendLine($"你是{sysName}的 AI 助手，正在协助管理员操作当前页面。");
        sb.AppendLine();
        sb.AppendLine("当前页面为系统配置表单页（如魔方设置），可协助查看与填写配置项。");
        sb.AppendLine();
        sb.AppendLine("可用工具：get_form_schema / fill_form / get_page_context / get_system_info / run_js（详细说明见函数定义，按需调用）");
        sb.AppendLine();
        sb.AppendLine("规则：");
        sb.AppendLine("1. 使用简体中文回答，语言简洁专业");
        sb.AppendLine("2. 用户要求新建/填写/补全配置时，先调用 get_form_schema 了解配置项，再调用 fill_form 生成值（对值为空的可填字段生成合理值；对已有值的字段保持原值或按用户要求调整；不要编造或修改敏感配置如 ApiKey/Secret/Token），最后提示用户检查后提交");
        sb.AppendLine("3. 用户询问当前页面内容/配置现状时，调用 get_page_context 获取页面上下文，再给出分析与结论");
        sb.AppendLine("4. 用户询问系统状态/诊断时，调用 get_system_info");
        sb.AppendLine("5. 用户要求读取或操作当前页面元素时，可调用 run_js 执行 JavaScript；修改页面内容或提交表单等写操作前，先向用户说明将执行的操作");
        sb.AppendLine("6. 不要编造数据；信息不足时主动询问用户澄清");

        return sb.Return(true);
    }
    #endregion

    /// <summary>已重载</summary>
    /// <param name="filterContext"></param>
    public override void OnActionExecuting(ActionExecutingContext filterContext)
    {
        //var fi = XmlConfig<TConfig>._.ConfigFile;
        //if (fi.IsNullOrEmpty() || !fi.AsFile().Exists) throw new Exception("无法找到配置文件 {0}".F(fi));

        var bs = this.Bootstrap();
        bs.MaxColumn = 1;
        bs.LabelWidth = 3;

        base.OnActionExecuting(filterContext);
    }
}