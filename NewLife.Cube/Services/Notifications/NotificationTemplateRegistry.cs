using System;
using System.Collections.Generic;
using NewLife.Serialization;
using XCode.Membership;

namespace NewLife.Cube.Services.Notifications;

/// <summary>通知模板</summary>
public class NotificationTemplate
{
    /// <summary>模板编码</summary>
    public String Code { get; set; }

    /// <summary>语言</summary>
    public String Language { get; set; }

    /// <summary>标题模板</summary>
    public String Title { get; set; }

    /// <summary>站内信内容模板</summary>
    public String Content { get; set; }

    /// <summary>短信内容模板。为空时使用短信配置的模板编号</summary>
    public String SmsContent { get; set; }

    /// <summary>APP 推送内容模板</summary>
    public String PushContent { get; set; }

    /// <summary>默认跳转地址</summary>
    public String Link { get; set; }

    /// <summary>默认渠道，逗号分隔</summary>
    public String Channels { get; set; }
}

/// <summary>通知模板注册表。模板存放在系统参数表，不新增数据表</summary>
public class NotificationTemplateRegistry
{
    /// <summary>参数分类</summary>
    public const String Category = "NotifyTemplate";

    /// <summary>获取租户全部模板</summary>
    /// <param name="tenantId">租户编号</param>
    /// <returns>模板列表</returns>
    public IList<NotificationTemplate> GetAll(Int32 tenantId)
    {
        var list = new List<NotificationTemplate>();
        foreach (var item in Parameter.FindAllByUserIdAndCategory(tenantId, Category))
        {
            if (item.LongValue.IsNullOrEmpty()) continue;

            var template = item.LongValue.ToJsonEntity<NotificationTemplate>();
            if (template == null) continue;

            template.Code ??= item.Name;
            template.Language ??= item.Value;
            list.Add(template);
        }

        return list;
    }

    /// <summary>获取模板</summary>
    /// <param name="tenantId">租户编号</param>
    /// <param name="code">模板编码</param>
    /// <param name="language">语言。为空时取默认模板</param>
    /// <returns>模板</returns>
    public NotificationTemplate Get(Int32 tenantId, String code, String language = null)
    {
        if (code.IsNullOrEmpty()) return null;

        var name = language.IsNullOrEmpty() ? code : $"{code}:{language}";
        var parameter = Parameter.FindByUserIdAndCategoryAndName(tenantId, Category, name)
            ?? Parameter.FindByUserIdAndCategoryAndName(tenantId, Category, code);
        if (parameter == null || parameter.LongValue.IsNullOrEmpty()) return null;

        var template = parameter.LongValue.ToJsonEntity<NotificationTemplate>();
        if (template == null) return null;

        template.Code ??= code;
        template.Language ??= parameter.Value;

        return template;
    }

    /// <summary>保存模板</summary>
    /// <param name="tenantId">租户编号</param>
    /// <param name="template">模板</param>
    public void Save(Int32 tenantId, NotificationTemplate template)
    {
        if (template == null) throw new ArgumentNullException(nameof(template));
        if (template.Code.IsNullOrEmpty()) throw new ArgumentException("Template code is required.", nameof(template));

        var name = template.Language.IsNullOrEmpty() ? template.Code : $"{template.Code}:{template.Language}";
        var parameter = Parameter.GetOrAdd(tenantId, Category, name);
        parameter.Value = template.Language;
        parameter.LongValue = template.ToJson();
        parameter.Save();
    }

    /// <summary>删除模板</summary>
    /// <param name="tenantId">租户编号</param>
    /// <param name="code">模板编码</param>
    /// <param name="language">语言</param>
    /// <returns>删除行数</returns>
    public Int32 Delete(Int32 tenantId, String code, String language = null)
    {
        var name = language.IsNullOrEmpty() ? code : $"{code}:{language}";

        return Parameter.Delete(Parameter._.UserId == tenantId
            & Parameter._.Category == Category
            & Parameter._.Name == name);
    }

    /// <summary>渲染模板。使用 {key} 占位符替换业务数据</summary>
    /// <param name="text">模板文本</param>
    /// <param name="data">业务数据</param>
    /// <returns>渲染结果</returns>
    public String Render(String text, Object data)
    {
        if (text.IsNullOrEmpty() || data == null) return text;

        var dictionary = data as IDictionary<String, Object> ?? data.ToDictionary();
        if (dictionary == null) return text;

        foreach (var item in dictionary)
        {
            if (item.Key.IsNullOrEmpty()) continue;

            text = text.Replace("{" + item.Key + "}", item.Value + "");
        }

        return text;
    }
}
