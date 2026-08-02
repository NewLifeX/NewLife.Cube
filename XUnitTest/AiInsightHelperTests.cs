using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Threading.Tasks;
using NewLife;
using NewLife.Cube.AI;
using NewLife.Data;
using NewLife.Web;
using XCode;
using XCode.Cache;
using XCode.Configuration;
using XCode.DataAccessLayer;
using Xunit;

namespace XUnitTest;

/// <summary>AI 洞察数据收集单元测试 — Collect 使用控制器传入的数据</summary>
/// <remarks>验证 AiInsightHelper.Collect 新契约：数据由控制器经 SearchData 查询后传入，
/// Collect 仅负责字段元数据、查询上下文提取、统计摘要与样本选取，不再自行查询数据库。</remarks>
public class AiInsightHelperTests
{
    #region 测试实体
    /// <summary>AI 测试实体。手写实体，仅用于元数据与内存数据，不访问数据库</summary>
    [BindTable("AiTestEntity", "AI测试实体", ConnName = "Test")]
    private class AiTestEntity : Entity<AiTestEntity>
    {
        private Int32 _Id;
        /// <summary>编号</summary>
        [DisplayName("编号")]
        [Description("编号")]
        [DataObjectField(true, false, false, 0)]
        public Int32 Id { get => _Id; set { if (OnPropertyChanging("Id", value)) { _Id = value; OnPropertyChanged("Id"); } } }

        private String _Name;
        /// <summary>名称</summary>
        [DisplayName("名称")]
        [DataObjectField(false, false, false, 50)]
        public String Name { get => _Name; set { if (OnPropertyChanging("Name", value)) { _Name = value; OnPropertyChanged("Name"); } } }

        private Double _Amount;
        /// <summary>金额</summary>
        [DisplayName("金额")]
        [DataObjectField(false, false, false, 0)]
        public Double Amount { get => _Amount; set { if (OnPropertyChanging("Amount", value)) { _Amount = value; OnPropertyChanged("Amount"); } } }

        private String _Password;
        /// <summary>密码（敏感字段，应被过滤）</summary>
        [DisplayName("密码")]
        [DataObjectField(false, false, false, 50)]
        public String Password { get => _Password; set { if (OnPropertyChanging("Password", value)) { _Password = value; OnPropertyChanged("Password"); } } }

        private DateTime _CreateTime;
        /// <summary>创建时间</summary>
        [DisplayName("创建时间")]
        [DataObjectField(false, false, false, 0)]
        public DateTime CreateTime { get => _CreateTime; set { if (OnPropertyChanging("CreateTime", value)) { _CreateTime = value; OnPropertyChanged("CreateTime"); } } }

        /// <summary>索引器重写：按字段名读写私有字段</summary>
        public override Object? this[String name]
        {
            get => name switch
            {
                "Id" => _Id,
                "Name" => _Name,
                "Amount" => _Amount,
                "Password" => _Password,
                "CreateTime" => _CreateTime,
                _ => base[name],
            };
            set
            {
                switch (name)
                {
                    case "Id": _Id = value.ToInt(); break;
                    case "Name": _Name = value + ""; break;
                    case "Amount": _Amount = value.ToDouble(); break;
                    case "Password": _Password = value + ""; break;
                    case "CreateTime": _CreateTime = value.ToDateTime(); break;
                    default: base[name] = value; break;
                }
            }
        }
    }
    #endregion

    #region 测试替身
    /// <summary>实体工厂测试替身。继承真实 DefaultEntityFactory，仅重写 Session 避免访问数据库</summary>
    private class FakeFactory : Entity<AiTestEntity>.DefaultEntityFactory
    {
        private readonly IEntitySession _session;

        public FakeFactory(Int32 totalCount) => _session = new FakeSession(totalCount);

        public override IEntitySession Session => _session;
    }

    /// <summary>实体会话测试替身。仅实现 Count，其余成员不支持（Collect 只用到 Count）</summary>
    private class FakeSession : IEntitySession
    {
        private readonly Int32 _count;

        public FakeSession(Int32 count) => _count = count;

        public String ConnName => "Test";
        public String TableName => "AiTestEntity";
        public IDataTable DataTable => throw new NotSupportedException();
        public String Key => "Test###AiTestEntity";
        public String FormatedTableName => throw new NotSupportedException();
        public DAL Dal => throw new NotSupportedException();
        public IDictionary<String, Object> Items { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public Int32 Count => _count;
        public Int64 LongCount => _count;
        public IEntityCache Cache => throw new NotSupportedException();
        public ISingleEntityCache SingleCache => throw new NotSupportedException();

        public event Action<Type> OnDataChange { add { } remove { } }

        public Boolean WaitForInitData(Int32 ms = 3000) => true;

        [Obsolete]
        public void ClearCache(String reason) { }

        public void ClearCache(String reason, Boolean force) { }

        public void InitData() { }

        public DbTable Query(SelectBuilder builder, Int64 startRowIndex, Int64 maximumRows) => throw new NotSupportedException();
        public Int32 QueryCount(SelectBuilder builder) => throw new NotSupportedException();
        public Int32 Execute(String sql, CommandType type = CommandType.Text, params IDataParameter[] ps) => throw new NotSupportedException();
        public Int64 InsertAndGetIdentity(String sql, CommandType type = CommandType.Text, params IDataParameter[] ps) => throw new NotSupportedException();
        public Task<DbTable> QueryAsync(SelectBuilder builder, Int64 startRowIndex, Int64 maximumRows) => throw new NotSupportedException();
        public Task<Int64> QueryCountAsync(SelectBuilder builder) => throw new NotSupportedException();
        public Task<Int32> ExecuteAsync(String sql, CommandType type = CommandType.Text, params IDataParameter[]? ps) => throw new NotSupportedException();
        public Task<Int64> InsertAndGetIdentityAsync(String sql, CommandType type = CommandType.Text, params IDataParameter[]? ps) => throw new NotSupportedException();
        public Int32 Truncate() => throw new NotSupportedException();
        public Int32 BeginTrans() => throw new NotSupportedException();
        public Int32 Commit() => throw new NotSupportedException();
        public Int32 Rollback() => throw new NotSupportedException();
        public EntityTransaction CreateTrans() => throw new NotSupportedException();
        public Int32 Insert(IEntity entity) => throw new NotSupportedException();
        public Int32 Update(IEntity entity) => throw new NotSupportedException();
        public Int32 Delete(IEntity entity) => throw new NotSupportedException();
        public Task<Int32> InsertAsync(IEntity entity) => throw new NotSupportedException();
        public Task<Int32> UpdateAsync(IEntity entity) => throw new NotSupportedException();
        public Task<Int32> DeleteAsync(IEntity entity) => throw new NotSupportedException();
    }
    #endregion

    #region 测试方法
    [Fact]
    [DisplayName("Collect - 使用传入数据计算统计与样本")]
    public void Collect_UsesPassedData_ComputesStatsAndSamples()
    {
        var now = new DateTime(2026, 1, 1);
        var list = new List<AiTestEntity>
        {
            new() { Id = 1, Name = "甲", Amount = 10, Password = "x", CreateTime = now },
            new() { Id = 2, Name = "乙", Amount = 20, Password = "y", CreateTime = now.AddDays(1) },
            new() { Id = 3, Name = "丙", Amount = 30, Password = "z", CreateTime = now.AddDays(2) },
        };

        var pager = new Pager();
        pager["Name"] = "测试";
        pager.Sort = "Amount";
        pager.Desc = true;

        var ctx = AiInsightHelper.Collect<AiTestEntity>(new FakeFactory(1000), pager, list, 100);

        // 使用传入数据，不再自行查询
        Assert.Equal(3, ctx.ShownCount);
        // 总记录数来自工厂会话
        Assert.Equal(1000, ctx.TotalCount);

        // 统计摘要：数值字段 min/max/avg
        Assert.True(ctx.Stats.NumericStats.ContainsKey("Amount"));
        var ns = ctx.Stats.NumericStats["Amount"];
        Assert.Equal(10.0, ns.Min);
        Assert.Equal(30.0, ns.Max);
        Assert.Equal(20.0, ns.Avg);

        // 查询上下文：过滤条件与排序
        Assert.Equal("测试", ctx.Filters["Name"]);
        Assert.Equal("Amount", ctx.SortField);
        Assert.True(ctx.SortDesc);

        // 样本非空且全部来自传入数据
        Assert.True(ctx.Samples.Count > 0);
        Assert.True(ctx.Samples.Count <= 50);
    }

    [Fact]
    [DisplayName("Collect - 空数据时样本与统计为空")]
    public void Collect_EmptyData_NoSamplesNoStats()
    {
        var pager = new Pager();

        var ctx = AiInsightHelper.Collect<AiTestEntity>(new FakeFactory(0), pager, new List<AiTestEntity>(), 100);

        Assert.Equal(0, ctx.ShownCount);
        Assert.Empty(ctx.Samples);
        Assert.Empty(ctx.Stats.NumericStats);
        Assert.Empty(ctx.Stats.Distribution);
    }

    [Fact]
    [DisplayName("Collect - 敏感字段 Password 被过滤")]
    public void Collect_FiltersSensitiveFields()
    {
        var list = new List<AiTestEntity>
        {
            new() { Id = 1, Name = "甲", Amount = 10, Password = "secret", CreateTime = DateTime.Now },
        };
        var pager = new Pager();

        var ctx = AiInsightHelper.Collect<AiTestEntity>(new FakeFactory(1), pager, list, 100);

        // 敏感字段（Password）不应出现在 AI 字段列表中
        Assert.DoesNotContain(ctx.Fields, f => f.Name == "Password");
        // 安全字段保留
        Assert.Contains(ctx.Fields, f => f.Name == "Name");
        Assert.Contains(ctx.Fields, f => f.Name == "Amount");
        Assert.Contains(ctx.Fields, f => f.Name == "CreateTime");
    }
    #endregion
}
