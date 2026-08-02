using System;
using System.Collections.Generic;
using System.ComponentModel;
using NewLife;
using NewLife.Cube.AI;
using NewLife.Web;
using XCode;
using Xunit;

namespace XUnitTest;

/// <summary>AI 洞察数据收集单元测试 — Collect 收集整页数据并过滤敏感字段</summary>
/// <remarks>验证 AiInsightHelper.Collect 契约：数据由控制器经 SearchData 查询后传入，
/// Collect 负责字段元数据（安全过滤）、查询上下文（Pager）与整页数据收集，不再自行查询数据库。</remarks>
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
        [BindColumn("Password", "密码", "", ItemType = "password")]
        [DisplayName("密码")]
        [DataObjectField(false, false, false, 50)]
        public String Password { get => _Password; set { if (OnPropertyChanging("Password", value)) { _Password = value; OnPropertyChanged("Password"); } } }

        private String _ExtData;
        /// <summary>扩展数据（名称安全但 ItemType=secret，验证 ItemType 过滤）</summary>
        [BindColumn("ExtData", "扩展数据", "", ItemType = "secret")]
        [DisplayName("扩展数据")]
        [DataObjectField(false, false, false, 50)]
        public String ExtData { get => _ExtData; set { if (OnPropertyChanging("ExtData", value)) { _ExtData = value; OnPropertyChanged("ExtData"); } } }

        private Int32 _RoleId;
        /// <summary>角色（映射字段，应被 RoleName 替代）</summary>
        [DisplayName("角色")]
        [DataObjectField(false, false, false, 0)]
        public Int32 RoleId { get => _RoleId; set { if (OnPropertyChanging("RoleId", value)) { _RoleId = value; OnPropertyChanged("RoleId"); } } }

        /// <summary>角色名称（Map 扩展属性，映射 RoleId，验证 xxxId→xxxName 替换）</summary>
        [Map("RoleId")]
        [DisplayName("角色名称")]
        public String RoleName => "角色" + _RoleId;

        private String _Remark;
        /// <summary>备注（大文本字段，应被截断）</summary>
        [DisplayName("备注")]
        [DataObjectField(false, false, false, 2000)]
        public String Remark { get => _Remark; set { if (OnPropertyChanging("Remark", value)) { _Remark = value; OnPropertyChanged("Remark"); } } }

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
                "ExtData" => _ExtData,
                "RoleId" => _RoleId,
                "RoleName" => RoleName,
                "Remark" => _Remark,
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
                    case "ExtData": _ExtData = value + ""; break;
                    case "RoleId": _RoleId = value.ToInt(); break;
                    case "Remark": _Remark = value + ""; break;
                    case "CreateTime": _CreateTime = value.ToDateTime(); break;
                    default: base[name] = value; break;
                }
            }
        }
    }
    #endregion

    #region 测试方法
    [Fact]
    [DisplayName("Collect - 整页数据进入上下文，敏感字段被过滤")]
    public void Collect_KeepsWholePage_FiltersSensitiveFields()
    {
        var now = new DateTime(2026, 1, 1);
        var list = new List<AiTestEntity>
        {
            new() { Id = 1, Name = "甲", Amount = 10, Password = "x", ExtData = "secret-1", CreateTime = now },
            new() { Id = 2, Name = "乙", Amount = 20, Password = "y", ExtData = "secret-2", CreateTime = now.AddDays(1) },
            new() { Id = 3, Name = "丙", Amount = 30, Password = "z", ExtData = "secret-3", CreateTime = now.AddDays(2) },
        };

        var pager = new Pager();
        pager["Name"] = "测试";
        pager.Sort = "Amount";
        pager.Desc = true;

        var ctx = AiInsightHelper.Collect<AiTestEntity>(new Entity<AiTestEntity>.DefaultEntityFactory(), pager, list);

        // 整页数据保留
        Assert.Equal(3, ctx.Data.Count);

        // 每行仅含安全字段，密码/密钥被过滤
        foreach (var row in ctx.Data)
        {
            Assert.Contains("Name", row.Keys);
            Assert.Contains("Amount", row.Keys);
            Assert.DoesNotContain("Password", row.Keys);
            Assert.DoesNotContain("ExtData", row.Keys);
        }

        // 查询上下文由 Pager 携带
        Assert.Same(pager, ctx.Pager);
        Assert.Equal("测试", ctx.Pager.Params["Name"]);
        Assert.Equal("Amount", ctx.Pager.Sort);
        Assert.True(ctx.Pager.Desc);
    }

    [Fact]
    [DisplayName("Collect - 空数据时数据为空但字段列表保留")]
    public void Collect_EmptyData_DataEmpty()
    {
        var pager = new Pager();

        var ctx = AiInsightHelper.Collect<AiTestEntity>(new Entity<AiTestEntity>.DefaultEntityFactory(), pager, new List<AiTestEntity>());

        Assert.Empty(ctx.Data);
        Assert.Contains(ctx.Fields, f => f.Name == "Name");
    }

    [Fact]
    [DisplayName("Collect - 敏感字段按名称与 ItemType 双重过滤")]
    public void Collect_FiltersSensitiveFields()
    {
        var list = new List<AiTestEntity>
        {
            new() { Id = 1, Name = "甲", Amount = 10, Password = "secret", ExtData = "secret", CreateTime = DateTime.Now },
        };
        var pager = new Pager();

        var ctx = AiInsightHelper.Collect<AiTestEntity>(new Entity<AiTestEntity>.DefaultEntityFactory(), pager, list);

        // 字段名黑名单：Password 被过滤
        Assert.DoesNotContain(ctx.Fields, f => f.Name == "Password");
        // ItemType 黑名单：ExtData 名称安全但 ItemType=secret 被过滤
        Assert.DoesNotContain(ctx.Fields, f => f.Name == "ExtData");
        // 安全字段保留
        Assert.Contains(ctx.Fields, f => f.Name == "Name");
        Assert.Contains(ctx.Fields, f => f.Name == "Amount");
        Assert.Contains(ctx.Fields, f => f.Name == "CreateTime");
    }

    [Fact]
    [DisplayName("Collect - 映射字段 RoleId 被 RoleName 替代")]
    public void Collect_ReplacesMapFieldWithName()
    {
        var list = new List<AiTestEntity>
        {
            new() { Id = 1, Name = "甲", Amount = 10, RoleId = 5 },
        };
        var pager = new Pager();

        var ctx = AiInsightHelper.Collect<AiTestEntity>(new Entity<AiTestEntity>.DefaultEntityFactory(), pager, list);

        // 字段列表：RoleId 被 RoleName 替代
        Assert.DoesNotContain(ctx.Fields, f => f.Name == "RoleId");
        Assert.Contains(ctx.Fields, f => f.Name == "RoleName");

        // 数据：RoleId 被 RoleName 替代，值为可读名称
        var row = Assert.Single(ctx.Data);
        Assert.DoesNotContain("RoleId", row.Keys);
        Assert.Equal("角色5", row["RoleName"]);
    }

    [Fact]
    [DisplayName("Collect - 大文本字段截断")]
    public void Collect_TruncatesLongText()
    {
        var longText = new String('测', 2000);
        var list = new List<AiTestEntity>
        {
            new() { Id = 1, Name = "甲", Remark = longText },
        };
        var pager = new Pager();

        var ctx = AiInsightHelper.Collect<AiTestEntity>(new Entity<AiTestEntity>.DefaultEntityFactory(), pager, list);

        var row = Assert.Single(ctx.Data);
        var remark = (String)row["Remark"]!;
        // 截断到 MaxTextLength + 截断标记
        var mark = "...[已截断]";
        Assert.True(remark.Length <= AiDataHelper.MaxTextLength + mark.Length);
        Assert.EndsWith(mark, remark);
    }
    #endregion
}
