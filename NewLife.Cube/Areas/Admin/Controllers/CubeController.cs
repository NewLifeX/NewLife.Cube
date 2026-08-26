using System.Collections;
using System.ComponentModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.AI;
using NewLife.Cube.ViewModels;
using NewLife.Reflection;
using NewLife.Serialization;
using XCode.Membership;

namespace NewLife.Cube.Areas.Admin.Controllers;

/// <summary>系统设置控制器</summary>
[DisplayName("魔方设置")]
[AdminArea]
[Menu(30, true, Icon = "Tools")]
public class CubeController : ConfigController<CubeSetting>, IPageDataContext
{
    /// <summary>WebAPI/SPA 不再使用的 MVC 皮肤项（主题/皮肤/表单组/新UI）</summary>
    private static readonly HashSet<String> SpaHiddenFields = new(StringComparer.OrdinalIgnoreCase)
    {
        nameof(CubeSetting.Theme),
        nameof(CubeSetting.Skin),
        nameof(CubeSetting.FormGroupClass),
        nameof(CubeSetting.EnableNewUI),
    };

    /// <summary>
    /// 官方常用 ECharts 主题（见 https://echarts.apache.org/v4/en/download-theme.html ）。
    /// default 表示不套主题脚本；其余需前端自行 registerTheme。
    /// </summary>
    private static readonly String[] EChartsOfficialThemes =
    [
        "default",
        "dark",
        "vintage",
        "macarons",
        "infographic",
        "shine",
        "roma",
    ];

    /// <summary>
    /// 字段元数据：注入 DefaultRole / ECharts / 登录图上传控件；隐藏 SPA 无用项。
    /// </summary>
    public override IList<DataField> GetFields(ViewKinds kind)
    {
        EnsureCubeSettingFieldSources();
        var list = GetMembers(typeof(CubeSetting))
            .Where(e => !SpaHiddenFields.Contains(e.Name))
            .ToList();
        foreach (var df in list)
        {
            // DefaultRole / ECharts 随上下文变化，禁止沿用上次物化结果
            if (df.Name.EqualIgnoreCase(nameof(CubeSetting.DefaultRole))
                || df.Name.EqualIgnoreCase(nameof(CubeSetting.EChartsTheme)))
                df.DataSourceMap = null;
            df.PrepareForApi();
        }
        return list;
    }

    /// <summary>挂接魔方设置字段数据源与控件类型</summary>
    private static void EnsureCubeSettingFieldSources()
    {
        var list = GetMembers(typeof(CubeSetting));

        var df = list.FirstOrDefault(e => e.Name == nameof(CubeSetting.DefaultRole));
        if (df != null)
        {
            df.ItemType = "singleSelect";
            df.DataSource = _ => BuildDefaultRoleOptions();
        }

        df = list.FirstOrDefault(e => e.Name == nameof(CubeSetting.EChartsTheme));
        if (df != null)
        {
            df.ItemType = "singleSelect";
            df.DataSource = _ => EChartsOfficialThemes.ToDictionary(e => e, e => e);
        }

        foreach (var name in new[] { nameof(CubeSetting.LoginLogo), nameof(CubeSetting.LoginBackground) })
        {
            df = list.FirstOrDefault(e => e.Name == name);
            if (df != null) df.ItemType = "image";
        }

        foreach (var name in new[] { nameof(CubeSetting.AIPrimaryColor), nameof(CubeSetting.AISecondaryColor) })
        {
            df = list.FirstOrDefault(e => e.Name == name);
            if (df != null) df.ItemType = "color";
        }

        // 验证码场景：位掩码多选（1=登录 / 2=注册 / 4=发码），空选=0 不启用
        df = list.FirstOrDefault(e => e.Name == nameof(CubeSetting.CaptchaScene));
        if (df != null)
        {
            df.ItemType = "multipleSelect";
            df.DataSource = _ => new Dictionary<String, String>
            {
                ["1"] = "登录",
                ["2"] = "注册",
                ["4"] = "发验证码（防短信轰炸）",
            };
        }
    }

    /// <summary>
    /// 默认角色候选项：键值均为角色 Name（与 FindByName / GetOrAdd 一致）。
    /// EnableTenant 且当前租户 Id&gt;0 时，仅保留租户 RoleIds 内且已启用的角色。
    /// </summary>
    private static IDictionary BuildDefaultRoleOptions()
    {
        var roles = Role.FindAllWithCache().Where(e => e.Enable).OrderByDescending(e => e.Sort).ToList();
        if (CubeSetting.Current.EnableTenant)
        {
            var tenantId = TenantContext.CurrentId;
            if (tenantId > 0)
            {
                var tenant = Tenant.FindById(tenantId);
                var ids = tenant?.RoleIds.SplitAsInt(",") ?? [];
                roles = roles.Where(e => ids.Contains(e.ID)).ToList();
            }
        }

        var dic = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
        foreach (var r in roles)
        {
            if (r.Name.IsNullOrEmpty()) continue;
            dic[r.Name] = r.Name;
        }

        var cur = CubeSetting.Current.DefaultRole;
        if (!cur.IsNullOrEmpty() && !dic.ContainsKey(cur))
            dic[cur] = cur;

        return dic;
    }

    /// <summary>配置页图片/文件上传（登录 Logo、背景等），保存到 UploadPath/Cube/</summary>
    [HttpPost]
    [EntityAuthorize(PermissionFlags.Update)]
    public async Task<ActionResult> UploadFile(IFormFile file, String id = null, String title = null)
    {
        if (file == null || file.Length <= 0) return Json(-1, "请选择文件");

        var set = CubeSetting.Current;
        var root = set.UploadPath.GetFullPath();
        var ext = Path.GetExtension(file.FileName);
        if (ext.IsNullOrEmpty()) ext = ".bin";
        var rel = $"Cube/{DateTime.Now:yyyyMMdd}/{DateTime.Now:HHmmssfff}_{Guid.NewGuid():N}{ext}";
        var dest = root.CombinePath(rel);
        dest.EnsureDirectory(true);

        await using (var fs = new FileStream(dest, FileMode.Create, FileAccess.Write))
        {
            await file.CopyToAsync(fs);
        }

        // 站点相对 URL，登录页可直接用作 img/background
        var url = "/" + set.UploadPath.Trim('/', '\\').Replace('\\', '/') + "/" + rel.Replace('\\', '/');
        return Json(0, null, new { url, filePath = url, path = url });
    }

    /// <summary>获取登录设置（附加开关与 StartPage；oauth 始终可见列表，不绑 EnableOAuthServer）</summary>
    [AllowAnonymous]
    [HttpGet]
    public ActionResult GetLoginConfig()
    {
        var model = new LoginConfigModel();
        var set = CubeSetting.Current;
        return Json(0, null, new
        {
            model.Code,
            model.Name,
            model.Copyright,
            model.Registration,
            model.Logo,
            model.LoginTip,
            model.LoginLogo,
            model.LoginBackground,
            model.Login,
            model.Register,
            model.OAuth,
            model.Security,
            EnableTenant = set.EnableTenant,
            EnableOAuthServer = set.EnableOAuthServer,
            StartPage = set.StartPage,
            SsoUserCenter = set.SsoUserCenter,
            RedirectUserToSso = set.RedirectUserToSso,
        });
    }

    /// <summary>收集当前页面数据上下文（魔方设置配置摘要）</summary>
    [HttpGet]
    public Task<String> GetPageDataContextAsync()
    {
        var set = CubeSetting.Current;
        var fields = new List<Object>();
        foreach (var fi in GetMembers(typeof(CubeSetting)))
        {
            if (SpaHiddenFields.Contains(fi.Name)) continue;
            if (AiFormHelper.IsSensitiveField(fi.Name)) continue;

            var value = set.GetValue(fi.Name);
            fields.Add(new
            {
                name = fi.Name,
                displayName = fi.DisplayName,
                description = fi.Description,
                category = fi.Category,
                value = value?.ToString() ?? "",
            });
        }

        var data = new { page = "魔方设置", description = "魔方后台系统配置，可按分类查看各项设置项", fields }.ToJson();
        return Task.FromResult(data);
    }
}
