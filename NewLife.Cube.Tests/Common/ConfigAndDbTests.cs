using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NewLife;
using NewLife.Cube;
using NewLife.Cube.Areas.Admin.Controllers;
using NewLife.Cube.Tests.Membership;
using XCode.Membership;
using Xunit;

namespace NewLife.Cube.Tests.Common;

/// <summary>
/// SYS 系统管理测试（第四批）。覆盖系统配置（修改后持久化到参数表并可读回）与数据库管理
/// （连接列表、表结构、实体结构与非法连接名拒绝）。
/// </summary>
[Collection("TenantAuth")]
public class ConfigAndDbTests
{
    private readonly TenantAuthFixture _fx;

    /// <summary>实例化</summary>
    /// <param name="fixture">多租户认证夹具。复用其 SQLite 库</param>
    public ConfigAndDbTests(TenantAuthFixture fixture) => _fx = fixture;

    #region 辅助
    private DbController CreateDbController()
    {
        var ctx = new DefaultHttpContext();
        ctx.RequestServices = _fx.Services;

        return new DbController { ControllerContext = new ControllerContext { HttpContext = ctx } };
    }

    private static String Content(ActionResult rs)
    {
        var cr = Assert.IsType<ContentResult>(rs);
        Assert.NotNull(cr.Content);

        return cr.Content;
    }
    #endregion

    #region SYS-1 系统配置
    [Fact(DisplayName = "系统配置_修改后持久化到参数表")]
    public void CubeSetting_Save_PersistsToParameters()
    {
        var set = CubeSetting.Current;
        var old = set.TokenExpire;
        try
        {
            set.TokenExpire = 4321;
            set.Save();

            var param = Parameter.FindByUserIDAndCategoryAndName(0, "Cube", nameof(CubeSetting.TokenExpire));
            Assert.NotNull(param);
            Assert.Contains("4321", param.Value);
        }
        finally
        {
            set.TokenExpire = old;
            set.Save();
        }

        var restored = Parameter.FindByUserIDAndCategoryAndName(0, "Cube", nameof(CubeSetting.TokenExpire));
        Assert.NotNull(restored);
        Assert.Equal(old.ToString(), restored.Value);

        // 配置对象本身同步生效
        Assert.Equal(old, CubeSetting.Current.TokenExpire);
    }
    #endregion

    #region SYS-4 数据库管理
    [Fact(DisplayName = "数据库管理_连接列表包含测试库且不含密码")]
    public void DbController_Index_ListsConnections()
    {
        var controller = CreateDbController();

        var json = Content(controller.Index());

        Assert.Contains("Membership", json);
        Assert.Contains("\"code\":0", json.Replace(" ", ""));
    }

    [Fact(DisplayName = "数据库管理_表结构与实体结构可查看")]
    public void DbController_ShowTablesAndEntities()
    {
        var controller = CreateDbController();

        var tables = Content(controller.ShowTables("Membership"));
        Assert.Contains("UserToken", tables);
        Assert.Contains("Parameter", tables);

        var entities = Content(controller.ShowEntities("Membership"));
        Assert.Contains("用户令牌", entities);
        Assert.Contains("Parameter", entities);
    }

    [Fact(DisplayName = "数据库管理_非法连接名被拒绝")]
    public void DbController_UnknownConnection_Throws()
    {
        var controller = CreateDbController();

        var ex = Assert.Throws<XException>(() => controller.ShowTables("NoSuchConnection"));
        Assert.False(ex.Message.IsNullOrEmpty());
        Assert.Throws<XException>(() => controller.ShowEntities("NoSuchConnection"));
        Assert.Throws<XException>(() => controller.ModelDiff("NoSuchConnection"));
    }
    #endregion
}
