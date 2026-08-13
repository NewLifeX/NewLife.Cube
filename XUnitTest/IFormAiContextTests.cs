using System;
using System.ComponentModel;
using System.Linq;
using NewLife;
using NewLife.Configuration;
using NewLife.Cube;
using NewLife.Cube.AI;
using Xunit;

namespace XUnitTest;

/// <summary>配置表单 AI 能力测试 — 验证 ConfigController 实现 IFormAiContext，为魔方设置等配置表单页提供 get_form_schema / fill_form 填表能力</summary>
/// <remarks>
/// 设计预期：魔方设置等配置表单页虽非实体控制器，但也是表单，应支持"帮我填表"。
/// ConfigController&lt;TConfig&gt; 实现 IFormAiContext 后，全局 /Ai/AiChat 端点注册表单工具，
/// AI 可枚举配置字段结构并按说明生成填表值（前端按 name 回填）。
/// </remarks>
public class IFormAiContextTests
{
    #region 测试配置
    /// <summary>表单能力测试配置。纯内存默认值，避免数据库依赖</summary>
    [DisplayName("测试配置")]
    public class TestFormConfig : Config<TestFormConfig>
    {
        /// <summary>调试</summary>
        [Description("调试")]
        public Boolean Debug { get; set; } = true;

        /// <summary>AI 模型</summary>
        [Description("AI 模型")]
        public String AIModel { get; set; } = "test-model";

        /// <summary>AI ApiKey。敏感字段，不应暴露给 AI</summary>
        [Description("AI ApiKey")]
        public String AIApiKey { get; set; } = "sk-test-secret";

        /// <summary>接口地址</summary>
        [Description("接口地址")]
        public String Endpoint { get; set; } = "";

        /// <summary>只读标记。模拟只读字段（无 setter）</summary>
        [Description("只读字段")]
        public Int32 ReadOnlyFlag => 1;
    }

    /// <summary>测试配置控制器</summary>
    public class TestFormController : ConfigController<TestFormConfig> { }
    #endregion

    [Fact(DisplayName = "ConfigController 实现 IFormAiContext（配置表单页支持填表）")]
    public void ConfigController_Implements_IFormAiContext()
    {
        Assert.True(typeof(IFormAiContext).IsAssignableFrom(typeof(ConfigController<TestFormConfig>)),
            "ConfigController 应实现 IFormAiContext，使魔方设置等配置表单页支持 AI 填表");
    }

    [Fact(DisplayName = "GetFormSchema 返回字段结构且过滤敏感字段")]
    public void GetFormSchema_FiltersSensitiveFields()
    {
        var ctrl = new TestFormController();

        var json = ctrl.GetFormSchema("edit");

        // 返回实体名与字段列表
        Assert.Contains("\"entity\":\"测试配置\"", json);
        Assert.Contains("\"mode\":\"edit\"", json);

        // 正常字段在 Schema 中（AiFormField 序列化为 PascalCase 属性名）
        Assert.Contains("\"Name\":\"AIModel\"", json);
        Assert.Contains("\"Name\":\"Debug\"", json);

        // 敏感字段（AIApiKey）不暴露
        Assert.DoesNotContain("AIApiKey", json);
        Assert.DoesNotContain("sk-test-secret", json);

        // 当前值并入（编辑模式供 AI 基于现状补全）
        Assert.Contains("test-model", json);

        // 只读属性（无 setter）不可填
        Assert.Contains("\"Name\":\"ReadOnlyFlag\"", json);
        Assert.Contains("\"Fillable\":false", json);
    }

    [Fact(DisplayName = "FillForm 转换类型并跳过敏感/只读字段")]
    public void FillForm_ConvertsAndSkipsSensitive()
    {
        var ctrl = new TestFormController();

        // AI 生成值：正常字段 + 敏感字段 + 未知字段
        var values = "{\"AIModel\":\"qwen3.7-plus\",\"Debug\":false,\"AIApiKey\":\"hacked\",\"Unknown\":\"x\"}";
        var result = ctrl.FillForm(values, "edit");

        // 工具结果含多受众内容块，合并后断言（用户可见的 fill_form JSON + 给 LLM 的说明）
        var all = String.Join("\n", result.Contents.Select(e => e.Data?.ToString()));

        // 用户可见内容：含回填值与统计
        Assert.Contains("fill_form", all);
        Assert.Contains("\"count\":2", all); // AIModel + Debug
        Assert.Contains("qwen3.7-plus", all);

        // 敏感字段被跳过、未知字段被跳过（出现在 skipped 列表）
        Assert.Contains("AIApiKey", all);
        Assert.Contains("Unknown", all);
        Assert.DoesNotContain("hacked", all); // 敏感值不回填

        // LLM 说明
        Assert.Contains("2 个", all);
    }
}
