using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NewLife;
using NewLife.AI.Tools;
using NewLife.Cube.AI;
using NewLife.Serialization;
using NewLife.Web;
using XCode;
using Xunit;

namespace XUnitTest;

/// <summary>AI 工具集重载测试 — 验证 CubeTools 工具方法可被二次开发者子类重写，且经 ToolRegistry 调用走虚分派</summary>
public class CubeToolsOverrideTests
{
    #region 测试实体
    /// <summary>重载测试实体。手写实体，仅用于元数据，不访问数据库</summary>
    [BindTable("AiToolOverrideEntity", "AI工具重载测试实体", ConnName = "Test")]
    private class AiToolOverrideEntity : Entity<AiToolOverrideEntity>
    {
        private Int32 _Id;
        /// <summary>编号</summary>
        [DisplayName("编号")]
        [DataObjectField(true, true, false, 0)]
        public Int32 Id { get => _Id; set { if (OnPropertyChanging("Id", value)) { _Id = value; OnPropertyChanged("Id"); } } }

        private String _Name;
        /// <summary>名称</summary>
        [DisplayName("名称")]
        [DataObjectField(false, false, false, 50)]
        public String Name { get => _Name; set { if (OnPropertyChanging("Name", value)) { _Name = value; OnPropertyChanged("Name"); } } }
    }
    #endregion

    #region 重写工具集
    /// <summary>自定义工具集：重写表单 Schema 与数据收集，模拟二次开发者调整数据逻辑</summary>
    private class MyCubeTools : CubeTools<AiToolOverrideEntity>
    {
        public MyCubeTools(IEntityFactory factory, Pager? pager, Int64 entityId, Func<Pager, IList<AiToolOverrideEntity>> queryData)
            : base(factory, pager, entityId, queryData) { }

        public override String GetFormSchema(String mode = "add", ToolCallContext? context = null)
            => new { mode, custom = true, note = "自定义字段结构" }.ToJson();

        protected override String GetListContext()
            => new { custom = true, summary = "自定义数据摘要" }.ToJson();

        protected override String GetRecordContext()
            => new { custom = true, record = "自定义记录上下文" }.ToJson();

        /// <summary>新增工具：定制记录分析上下文</summary>
        [ToolDescription("get_custom_context", ReadOnly = true)]
        public String GetCustomContext() => new { custom = true, entity = "custom" }.ToJson();
    }
    #endregion

    private static MyCubeTools CreateMyTools() =>
        new(new Entity<AiToolOverrideEntity>.DefaultEntityFactory(), null, 0, p => []);

    [Fact]
    [DisplayName("CubeTools 工具方法可被子类重写")]
    public void Tools_Override_Works()
    {
        var tools = CreateMyTools();

        // 直接调用走虚分派 → 返回子类重写内容
        var schema = tools.GetFormSchema("add");
        Assert.Contains("\"custom\":true", schema);
        Assert.Contains("自定义字段结构", schema);

        // 工具方法 GetDataContext 走基类分派，内部虚调用子类重写的收集方法
        var ctx = tools.GetDataContext();
        Assert.Contains("自定义数据摘要", ctx);
    }

    [Fact]
    [DisplayName("ToolRegistry 注册重写工具，调用走虚分派")]
    public async Task ToolRegistry_Override_Dispatch()
    {
        var tools = CreateMyTools();
        var registry = new ToolRegistry();
        registry.AddTools(tools);

        // 显式实现接口，经 IToolProvider 访问
        IToolProvider provider = registry;

        // 工具定义发现：基类工具 get_data_context + 重写方法（get_form_schema）+ 新增方法（get_custom_context）
        var defs = provider.GetTools(null);
        Assert.Contains(defs, t => t.Function?.Name == "get_data_context");
        Assert.Contains(defs, t => t.Function?.Name == "get_form_schema");
        Assert.Contains(defs, t => t.Function?.Name == "get_custom_context");

        // 调用 get_data_context → 内部虚调用 MyCubeTools 重写的收集方法
        var ctx = await provider.CallToolAsync("get_data_context", "{}", null, CancellationToken.None);
        Assert.False(ctx.IsError);
        var ctxUser = ctx.Contents.First(c => c.Audience.HasFlag(ToolAudience.User)).Data + "";
        Assert.Contains("自定义数据摘要", ctxUser);

        // 调用 get_form_schema → 走 MyCubeTools 重写实现
        var result = await provider.CallToolAsync("get_form_schema", "{\"mode\":\"add\"}", null, CancellationToken.None);
        Assert.False(result.IsError);
        var user = result.Contents.First(c => c.Audience.HasFlag(ToolAudience.User)).Data + "";
        Assert.Contains("自定义字段结构", user);

        // 调用新增工具
        var custom = await provider.CallToolAsync("get_custom_context", null, null, CancellationToken.None);
        var customUser = custom.Contents.First(c => c.Audience.HasFlag(ToolAudience.User)).Data + "";
        Assert.Contains("\"entity\":\"custom\"", customUser);
    }
}
