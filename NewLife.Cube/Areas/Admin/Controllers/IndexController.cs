using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.AI;
using NewLife.Cube.Entity;
using NewLife.Cube.ViewModels;
using NewLife.Cube.Widgets.System;
using NewLife.Log;
using NewLife.Reflection;
using NewLife.Serialization;
using NewLife.Web;
using XCode;
using XCode.Membership;
using XLog = XCode.Membership.Log;
using static XCode.Membership.Log;

namespace NewLife.Cube.Areas.Admin.Controllers;

/// <summary>首页</summary>
[DisplayName("首页")]
[AdminArea]
[Menu(0, false, Icon = "HomeFilled")]
public class IndexController : ControllerBaseX, IPageDataContext
{
    private readonly IManageProvider _provider;
    private readonly IWebHostEnvironment _env;
    private readonly IAIService _ai;

    static IndexController() => MachineInfo.RegisterAsync();

    /// <summary>实例化</summary>
    public IndexController(IManageProvider manageProvider, IWebHostEnvironment env, IAIService ai)
    {
        _provider = manageProvider;
        _env = env;
        _ai = ai;
        PageSetting.EnableNavbar = false;
    }

    /// <summary>首页</summary>
    /// <returns></returns>
    //[EntityAuthorize(PermissionFlags.Detail)]
    [AllowAnonymous]
    [HttpGet("/api/[area]/[controller]")]
    public ActionResult Index()
    {
        var user = ManageProvider.Provider.TryLogin(HttpContext);
        // WebAPI版实体路由带 /api 前缀，回跳地址需还原为前端路由
        if (user == null) return RedirectToAction("Login", "User", new { r = (Request.Path + "").TrimApiPrefix() });

        //ViewBag.User = ManageProvider.User;
        //ViewBag.Config = SysConfig.Current;

        //// 工作台页面
        //var startPage = Request["page"];
        //if (startPage.IsNullOrEmpty()) startPage = Setting.Current.StartPage;

        //ViewBag.Main = startPage;

        //return View();
        return Json(0, "ok");
    }

    /// <summary>服务器信息</summary>
    /// <returns></returns>
    [DisplayName("服务器信息")]
    [EntityAuthorize(PermissionFlags.Detail)]
    [HttpGet]
    public ActionResult Main()
    {
        var result = BuildServerInfo(_env.ContentRootPath, HttpContext);
        //var res = result.ToOkApiResponse();
        return Json(0, null, result);
    }

    /// <summary>收集服务器信息（供页面展示与 AI 页面上下文共用，避免重复逻辑）</summary>
    /// <param name="contentRootPath">应用内容根目录（<see cref="IWebHostEnvironment.ContentRootPath"/>）</param>
    /// <param name="context">当前 HTTP 上下文，用于获取请求与连接信息</param>
    /// <returns>服务器信息对象</returns>
    private static Object BuildServerInfo(String contentRootPath, Microsoft.AspNetCore.Http.HttpContext context)
    {
        var req = context.Request;
        var conn = context.Connection;
        var gc = $"IsServerGC={GCSettings.IsServerGC},LatencyMode={GCSettings.LatencyMode}";
        var mi = MachineInfo.Current ?? new MachineInfo();
        var process = Process.GetCurrentProcess();
        var asm = Assembly.GetExecutingAssembly();
        var att = asm.GetCustomAttribute<TargetFrameworkAttribute>();
        var ver = att?.FrameworkDisplayName ?? att?.FrameworkName;
        var addrLocal = conn.LocalIpAddress;
        var addrRemote = conn.RemoteIpAddress;
        if (addrLocal != null && addrLocal.IsIPv4MappedToIPv6) addrLocal = addrLocal.MapToIPv4();
        if (addrRemote != null && addrRemote.IsIPv4MappedToIPv6) addrRemote = addrRemote.MapToIPv4();
        var userHost = context.GetUserHost();
        var result = new
        {
            system = req.GetRawUrl()?.AbsolutePath,
            path = contentRootPath,
            host = req.Headers["Host"],
            local = addrLocal + ":" + conn.LocalPort,
            remote = addrRemote + ":" + conn.RemotePort,
            application = process.ProcessName,
            applicationTitle = Environment.CommandLine,
            version = ver,
            os = mi.OSName,
            osVersion = mi.OSVersion,
            machineId = mi.UUID,
            machineProduct = mi.Product,
            cpu = mi.Processor + Environment.ProcessorCount + "核心 使用率" + mi.CpuRate.ToString("p0") + mi.Temperature + " ℃",
            openTime = TimeSpan.FromMilliseconds(Environment.TickCount64).ToString(@"dd\.hh\:mm\:ss"),
            serverTime = DateTime.Now,
            memory = "物理：" + (mi.AvailableMemory / 1024 / 1024).ToString("n0") + "M/" + (mi.Memory / 1024 / 1024).ToString("n0") + "M    工作/提交: " + (process.WorkingSet64 / 1024 / 1024).ToString("n0") + "M/@" + (process.PrivateMemorySize64 / 1024 / 1024).ToString("n0") + "M   GC: " + (GC.GetTotalMemory(false) / 1024 / 1024).ToString("n0") + "M",
            processTime = process.TotalProcessorTime.TotalSeconds.ToString("N2") + "秒 启动于" + process.StartTime.ToLocalTime().ToFullString(),
            gc = gc,
            //startTime = ApplicationManager.Load().StartTime.ToLocalTime().ToFullString()
        };
        return result;
    }

    /// <summary>收集当前页面数据上下文（服务器信息），供 AI 分析当前页面。实现 <see cref="IPageDataContext"/>，get_page_context 优先调用服务端实现</summary>
    /// <returns>服务器信息 JSON</returns>
    [HttpGet]
    public Task<String> GetPageDataContextAsync()
        => Task.FromResult(BuildServerInfo(_env.ContentRootPath, HttpContext).ToJson());

    /// <summary>监控数据。工作台性能曲线轮询接口，返回CPU/内存快照</summary>
    /// <returns>CPU/内存快照 JSON</returns>
    [DisplayName("监控数据")]
    [EntityAuthorize(PermissionFlags.Detail)]
    [HttpGet]
    public ActionResult MonitorData()
    {
        var mi = MachineInfo.Current ?? new MachineInfo();

        var cpu = Math.Round(mi.CpuRate * 100, 1);
        var memPct = mi.Memory > 0 ? Math.Round((mi.Memory - mi.AvailableMemory) * 100.0 / mi.Memory, 1) : 0;

        // 图表卡轮询契约：xs 为 X 轴新点数组，series 为各系列新数据数组，前端定时追加
        return Json(0, null, new
        {
            xs = new[] { DateTime.Now.ToString("HH:mm:ss") },
            series = new[]
            {
                new[] { cpu },
                new[] { memPct },
            },
            memUsed = (mi.Memory - mi.AvailableMemory) / 1024 / 1024,
            memTotal = mi.Memory / 1024 / 1024,
        });
    }

    /// <summary>工作台数据聚合（React 皮肤首页使用）。返回 KPI、快捷入口、个人信息、系统信息</summary>
    /// <returns>工作台聚合数据 JSON</returns>
    [DisplayName("工作台数据")]
    [EntityAuthorize]
    [HttpGet]
    public ActionResult Workbench()
    {
        var user = ManageProvider.User;
        var now = DateTime.Now;
        var start = now.AddHours(-24);

        var mi = MachineInfo.Current ?? new MachineInfo();
        var process = Process.GetCurrentProcess();
        var memTotal = mi.Memory / 1024 / 1024;
        var memUsed = memTotal - mi.AvailableMemory / 1024 / 1024;
        var memRate = memTotal <= 0 ? 0 : (Double)memUsed * 100 / memTotal;

        var snow = XLog.Meta.Factory.Snow;

        // KPI 指标
        var kpis = new List<Object>
        {
            new { name = "users", label = "用户总数", value = XCode.Membership.User.Meta.Count.ToString("n0"), trend = "注册用户", color = "blue", url = "/Admin/User" },
            new { name = "login", label = "今日登录", value = XLog.FindCount(_.ID.Between(DateTime.Today, now, snow) & _.Action.Contains("登录")).ToString("n0"), trend = "今日登录成功", color = "green", url = "/Admin/Log" },
            new { name = "online", label = "在线用户", value = UserOnline.FindCount().ToString("n0"), trend = "当前在线", color = "cyan", url = "/Admin/UserOnline" },
            new { name = "log", label = "24h日志", value = XLog.FindCount(_.ID.Between(start, now, snow)).ToString("n0"), trend = "最近24小时", color = "grey", url = "/Admin/Log" },
            new { name = "error", label = "24h异常", value = XLog.FindCount(_.ID.Between(start, now, snow) & _.Success == false).ToString(), trend = "最近24小时异常", color = "red", url = "/Admin/Log?success=false" },
            new { name = "cpu", label = "CPU使用率", value = Math.Round(mi.CpuRate * 100, 1).ToString("0.0") + "%", trend = "内存 " + Math.Round(memRate, 1).ToString("0.0") + "%", color = "orange", url = "/Admin/Index/Main" },
        };

        // 快捷入口：最近访问优先，菜单补足
        var links = new List<Object>();
        var urls = new HashSet<String>(StringComparer.OrdinalIgnoreCase);
        if (user != null)
        {
            foreach (var item in QuickLinkWidget.GetRecent(user.ID))
            {
                var menu = ManageProvider.Menu?.FindByUrl(item.Url);
                if (menu == null || !menu.Visible || menu.Url.IsNullOrEmpty() || menu.Url.StartsWith("~/")) continue;
                if (!urls.Add(menu.Url)) continue;

                links.Add(new { Name = menu.DisplayName ?? menu.Name, Url = menu.Url, Icon = menu.Icon });
            }
        }
        if (links.Count < 8)
        {
            foreach (var menu in XCode.Membership.Menu.FindAllWithCache().Where(e => e.Visible && !e.Url.IsNullOrEmpty() && !e.Url.StartsWith("~/") && !e.Url.EqualIgnoreCase("/Admin/Index/Dashboard")).OrderByDescending(e => e.UpdateTime).ThenByDescending(e => e.ID))
            {
                if (links.Count >= 8) break;
                if (!urls.Add(menu.Url)) continue;

                links.Add(new { Name = menu.DisplayName ?? menu.Name, Url = menu.Url, Icon = menu.Icon });
            }
        }

        // 个人信息
        var u = user as User;
        Object profile = u == null ? null : new
        {
            u.Name,
            u.DisplayName,
            RoleNames = u.RoleNames,
            u.Online,
            u.Logins,
            LastLogin = u.LastLogin.Year > 2000 ? u.LastLogin.ToFullString() : "",
            u.LastLoginIP,
            RegisterTime = u.RegisterTime.Year > 2000 ? u.RegisterTime.ToFullString() : "",
        };

        // 系统信息
        var sysInfo = new Dictionary<String, Object>
        {
            ["操作系统"] = $"{mi.OSName} {mi.OSVersion}",
            ["机器"] = $"{Environment.MachineName} / {Environment.UserName}",
            ["处理器"] = $"{mi.Processor}，{Environment.ProcessorCount} 核心",
            ["运行时"] = Assembly.GetExecutingAssembly().GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkDisplayName,
            ["应用"] = $"{process.ProcessName}，程序集 {AssemblyX.GetAssemblies(null).Count()} 个",
            ["启动时间"] = DateTime.Now.AddMilliseconds(-Environment.TickCount64).ToFullString(),
        };

        return Json(0, null, new
        {
            user = new
            {
                name = user?.Name,
                displayName = (user as IUser)?.DisplayName,
                roles = (user as IUser)?.Roles?.Select(e => e.Name).ToList(),
            },
            kpis,
            quickLinks = links,
            profile,
            sysInfo,
        });
    }

    #region AI 诊断
    /// <summary>AI 系统诊断。根据服务器运行指标生成健康诊断报告（SSE 流式输出）</summary>
    /// <returns></returns>
    [DisplayName("AI 系统诊断")]
    [EntityAuthorize(PermissionFlags.Detail)]
    [HttpGet]
    public async Task<ActionResult> AiDiagnose()
    {
        var set = CubeSetting.Current;
        if (!set.AISwitch) return Json(500, null, "AI 未启用，请在系统设置中开启");

        var mi = MachineInfo.Current ?? new MachineInfo();
        var process = Process.GetCurrentProcess();

        // 查询过去 24h 错误数
        var now = DateTime.Now;
        var start = now.AddHours(-24);
        var errorCount = XCode.Membership.Log.FindCount(
            XCode.Membership.Log._.CreateTime >= start & XCode.Membership.Log._.CreateTime <= now,
            null, null, 0, 0);

        var sysInfo = new
        {
            cpu = $"{mi.CpuRate:P0}",
            temperature = mi.Temperature,
            memoryAvailable = $"{mi.AvailableMemory / 1024 / 1024:N0}M",
            memoryTotal = $"{mi.Memory / 1024 / 1024:N0}M",
            workingSet = $"{process.WorkingSet64 / 1024 / 1024:N0}M",
            openTime = TimeSpan.FromMilliseconds(Environment.TickCount64).ToString(@"dd\.hh\:mm\:ss"),
            errorCount24h = errorCount,
            os = mi.OSName,
            machineName = Environment.MachineName,
        }.ToJson();

        // SSE 方式输出
        Response.Headers["Content-Type"] = "text/event-stream; charset=utf-8";
        Response.Headers["Cache-Control"] = "no-cache";
        Response.Headers["X-Accel-Buffering"] = "no";

        // 发送元数据事件
        var metaJson = new { type = "meta", model = set.AIModel, thinking = false }.ToJson();
        await Response.WriteAsync($"data: {metaJson}\n\n", HttpContext.RequestAborted);
        await Response.Body.FlushAsync(HttpContext.RequestAborted);

        await foreach (var chunk in _ai.DiagnoseSystemStreamAsync(sysInfo, HttpContext.RequestAborted))
        {
            if (chunk.IsNullOrEmpty()) continue;
            var eventJson = new { type = "text", content = chunk }.ToJson();
            await Response.WriteAsync($"data: {eventJson}\n\n", HttpContext.RequestAborted);
            await Response.Body.FlushAsync(HttpContext.RequestAborted);
        }

        // 发送结束事件
        await Response.WriteAsync($"data: {{\"type\":\"done\"}}\n\n", HttpContext.RequestAborted);
        await Response.Body.FlushAsync(HttpContext.RequestAborted);

        return new EmptyResult();
    }
    #endregion

    /// <summary>服务器变量列表</summary>
    /// <returns></returns>
    [DisplayName("服务器变量列表")]
    [EntityAuthorize(PermissionFlags.Detail)]
    [HttpGet]
    public ActionResult ServerVarList()
    {
        var req = HttpContext.Request;
        var list = new List<dynamic>();
        foreach (var kv in req.Headers)
        {
            var v = kv.Value.ToString();
            var key = kv.Key;
            list.Add(new { name = key, value = v });
        }
        var rqlist = new List<dynamic>();
        foreach (var pi in req.GetType().GetProperties())
        {
            var type = pi.PropertyType;
            if (pi.GetIndexParameters().Length > 0 || (type != typeof(String)
                                                  && type != typeof(Uri)
                                                  && type != typeof(PathString)
                                                  && type != typeof(HostString)
                                                  && !typeof(Boolean).IsAssignableFrom(type)
                                                  && !typeof(String).IsAssignableFrom(type)))
            {
                continue;
            }
            rqlist.Add(new { name = pi.Name, value = req.GetValue(pi) });
        }
        return Json(0, null, new { server = list, requestName = req.GetType().FullName, request = rqlist });
    }
    /// <summary>进程模块列表</summary>
    /// <param name="model">All全部,OnlyUser用户</param>
    /// <returns></returns>
    [DisplayName("进程模块列表")]
    [EntityAuthorize(PermissionFlags.Detail)]
    [HttpGet]
    public ActionResult ProcessList(String model)
    {
        var isAll = String.Equals("All", model, StringComparison.OrdinalIgnoreCase);
        var process = Process.GetCurrentProcess();
        var result = new List<dynamic>();
        foreach (ProcessModule item in process.Modules)
        {
            try
            {
                if (isAll || item.FileVersionInfo.CompanyName != "Microsoft Corporation")
                {
                    result.Add(new
                    {
                        name = item.ModuleName,
                        companyName = item.FileVersionInfo.CompanyName,
                        productName = item.FileVersionInfo.ProductName,
                        description = item.FileVersionInfo.FileDescription,
                        version = item.FileVersionInfo.FileVersion,
                        size = item.ModuleMemorySize,
                        fileName = item.FileName
                    });
                }
            }
            catch { }
        }
        return Json(0, null, result);
    }

    /// <summary>程序集列表</summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [DisplayName("程序集列表")]
    [EntityAuthorize(PermissionFlags.Detail)]
    [HttpGet]
    public ActionResult AssemblyList(String model)
    {
        var isAll = String.Equals("All", model, StringComparison.OrdinalIgnoreCase);
        var result = new List<dynamic>();
        AssemblyX[] asms = null;
        if (isAll)
            asms = AssemblyX.GetAssemblies(null).OrderBy(e => e.Name).OrderByDescending(e => e.Compile).ToArray();
        else
            asms = AssemblyX.GetMyAssemblies().OrderBy(e => e.Name).OrderByDescending(e => e.Compile).ToArray();
        foreach (var assembly in asms)
        {
            result.Add(new
            {
                name = assembly.Name,
                title = assembly.Title,
                fileVersion = assembly.FileVersion,
                version = assembly.Version,
                compileTime = assembly.Compile.ToFullString(),
                location = assembly.Asm.Location
            });
        }
        return Json(0, null, result);
    }

    /// <summary>重启</summary>
    /// <returns></returns>
    [DisplayName("重启")]
    [EntityAuthorize((PermissionFlags)16)]
    [HttpPost]
    public ActionResult Restart()
    {
        //try
        {
            //AppDomain.Unload(AppDomain.CurrentDomain);
            //HttpContext.User = null;
            //HttpRuntime.UnloadAppDomain();
            //HostingEnvironment.InitiateShutdown();
            //ApplicationManager.GetApplicationManager().ShutdownAll();
            // 通过修改web.config时间来重启站点，稳定可靠
            var wc = "web.config".GetFullPath();
            System.IO.File.SetLastWriteTime(wc, DateTime.Now);
        }
        //catch { }

        return RedirectToAction(nameof(Main));
    }

    /// <summary>
    /// 释放内存，参考之前的Runtime方法
    /// </summary>
    /// <returns></returns>
    [DisplayName("释放内存")]
    [EntityAuthorize((PermissionFlags)16)]
    [HttpGet]
    public ActionResult MemoryFree()
    {
        try
        {
            GC.Collect();

            // 释放当前进程所占用的内存
            var p = Process.GetCurrentProcess();
            SetProcessWorkingSetSize(p.Handle, -1, -1);
        }
        catch (Exception ex)
        {
            XTrace.WriteException(ex);
        }

        return Json(0, "释放内存成功");
    }

    [DllImport("kernel32.dll")]
    static extern Boolean SetProcessWorkingSetSize(IntPtr proc, Int32 min, Int32 max);

    /// <summary>
    /// 获取菜单树
    /// </summary>
    /// <returns></returns>
    [EntityAuthorize]
    [HttpGet]
    public ActionResult GetMenuTree(String module) => Json(0, null, GetMenu(module));

    private IList<MenuTree> GetMenu(String module)
    {
        var fact = ManageProvider.Menu;
        var menus = fact.Root.Childs;

        // 根据模块过滤菜单
        if (module.EqualIgnoreCase("base"))
        {
            // 直接取base下级，以及所有仅有二级的菜单
            var ms = menus.FirstOrDefault(e => e.Name.EqualIgnoreCase("base"))?.Childs ?? [];
            foreach (var item in menus)
            {
                if (!item.Name.EqualIgnoreCase("base") && item.Childs.All(e => e.Childs.Count == 0))
                {
                    ms.Add(item);
                }
            }
            menus = ms;
        }
        else if (!module.IsNullOrEmpty())
        {
            menus = menus.FirstOrDefault(e => e.Name.EqualIgnoreCase(module))?.Childs ?? [];
        }
        // module 为空时不做过滤，直接返回全部根级菜单

        // 如果顶级只有一层，并且至少有三级目录，则提升一级
        if (menus.Count == 1 && menus[0].Childs.All(m => m.Childs.Count > 0)) { menus = menus[0].Childs; }

        var menuTree = MenuTree.GetMenuTree(pMenuTree =>
        {
            // 左侧菜单展示所有可见菜单，不按角色权限过滤
            // 权限控制在 Controller/Action 层通过 EntityAuthorizeAttribute 实现
            var parent = fact.FindByID(pMenuTree.ID);
            var subMenus = parent?.Childs?.Where(m => m.Visible).ToList() as IList<IMenu> ?? [];
            return subMenus;
        }, list =>
        {
            if (list == null || list.Count == 0) return null;
            var menuList = (from menu in list
                            select new MenuTree
                            {
                                ID = menu.ID,
                                Name = menu.Name,
                                DisplayName = menu.DisplayName ?? menu.Name,
                                FullName = menu.FullName,
                                Url = menu.Url,
                                Icon = menu.Icon,
                                Visible = menu.Visible,
                                NewWindow = menu.NewWindow,
                                ParentID = menu.ParentID,
                                Permissions = menu.Permissions
                            }).ToList();
            return menuList.Count > 0 ? menuList : null;
        }, menus);

        return menuTree;
    }
}