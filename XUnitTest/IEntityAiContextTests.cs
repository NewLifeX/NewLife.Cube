using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using NewLife;
using NewLife.AI.Models;
using NewLife.Cube;
using NewLife.Cube.AI;
using NewLife.Web;
using XCode;
using Xunit;

namespace XUnitTest;

/// <summary>IEntityAiContext 能力接口单元测试 — 验证实体控制器将 SearchData/CreateCubeTools/BuildChatSystemPrompt 等重载点经接口暴露给全局 AiController，子类重载仍生效</summary>
public class IEntityAiContextTests
{
    #region 测试实体
    /// <summary>接口测试实体。手写实体，仅用于元数据，不访问数据库</summary>
    [BindTable("AiContextTestEntity", "AI能力接口测试实体", ConnName = "Test")]
    private class AiContextTestEntity : Entity<AiContextTestEntity>
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

        /// <summary>索引器重写：按字段名读写私有字段</summary>
        public override Object? this[String name]
        {
            get => name switch
            {
                "Id" => _Id,
                "Name" => _Name,
                _ => base[name],
            };
            set
            {
                switch (name)
                {
                    case "Id": _Id = value.ToInt(); break;
                    case "Name": _Name = value + ""; break;
                    default: base[name] = value; break;
                }
            }
        }
    }
    #endregion

    #region 测试控制器
    /// <summary>自定义工具集，验证 CreateCubeTools 重载返回类型可识别</summary>
    private class MyTools : CubeTools<AiContextTestEntity>
    {
        public MyTools(IEntityFactory factory, Pager? pager, Int64 entityId, Func<Pager, IList<AiContextTestEntity>> queryData)
            : base(factory, pager, entityId, queryData) { }
    }

    /// <summary>重载 AI 重载点的实体控制器，验证接口委托到子类重载</summary>
    private class AiContextTestController : ReadOnlyEntityController<AiContextTestEntity>
    {
        /// <summary>重载标记</summary>
        public const String Marker = "override-marker";

        protected override IEnumerable<AiContextTestEntity> SearchData(Pager p) => new[] { new AiContextTestEntity { Name = Marker } };

        protected override MyTools CreateCubeTools(Pager? pager, Int64 entityId)
            => new(Factory, pager, entityId, p => SearchData(p).ToList());

        protected override String BuildChatSystemPrompt(AiChatRequest req, Pager? pager) => Marker;
    }
    #endregion

    [Fact(DisplayName = "实体控制器实现 IEntityAiContext 且接口委托到子类重载")]
    public void Interface_Delegates_To_Overrides()
    {
        // 构造函数内 OrderByKey 优化会查询 Meta.Count，测试环境无该表，临时关闭避免 DB 依赖
        var old = PageSetting.Global.OrderByKey;
        try
        {
            PageSetting.Global.OrderByKey = false;

            var ctrl = new AiContextTestController();

            var ctx = Assert.IsAssignableFrom<IEntityAiContext>(ctrl);

            // Factory：实体工厂
            Assert.NotNull(ctx.Factory);
            Assert.Equal(typeof(AiContextTestEntity), ctx.Factory.EntityType);

            // SearchData：子类重载生效
            var data = ctx.SearchData(new Pager()).ToList();
            Assert.Single(data);
            Assert.Equal(AiContextTestController.Marker, ((AiContextTestEntity)data[0]).Name);

            // CreateCubeTools：子类重载生效（返回自定义工具集）
            var tools = ctx.CreateCubeTools(null, 0);
            Assert.IsType<MyTools>(tools);

            // BuildChatSystemPrompt：子类重载生效
            Assert.Equal(AiContextTestController.Marker, ctx.BuildChatSystemPrompt(new AiChatRequest(), null));
        }
        finally
        {
            PageSetting.Global.OrderByKey = old;
        }
    }
}
