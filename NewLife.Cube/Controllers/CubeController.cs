using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using NewLife.Data;
using NewLife.Log;
using NewLife.Reflection;
using XCode;
using XCode.Membership;
using static XCode.Membership.User;
using AreaX = XCode.Membership.Area;
using System.IO;

#if __CORE__
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
#else
using System.Web;
using System.Web.Mvc;
#endif

namespace NewLife.Cube.Controllers
{
    /// <summary>魔方前端数据接口</summary>
    [DisplayName("数据接口")]
    public class CubeController : ControllerBaseX
    {
#if __CORE__
        private readonly IList<EndpointDataSource> _sources;

        /// <summary>构造函数</summary>
        /// <param name="sources"></param>
        public CubeController(IEnumerable<EndpointDataSource> sources)
        {
            _sources = sources.ToList();
        }
#endif

        #region 拦截
#if __CORE__
        /// <summary>执行后</summary>
        /// <param name="context"></param>
        public override void OnActionExecuted(ActionExecutedContext context)
        {
            if (context.Exception != null && !context.ExceptionHandled)
            {
                var ex = context.Exception.GetTrue();
                context.Result = Json(0, null, ex);
                context.ExceptionHandled = true;

                if (XTrace.Debug) XTrace.WriteException(ex);

                return;
            }

            base.OnActionExecuted(context);
        }
#else
        /// <summary>异常</summary>
        /// <param name="filterContext"></param>
        protected override void OnException(ExceptionContext filterContext)
        {
            if (filterContext.Exception != null && !filterContext.ExceptionHandled)
            {
                var ex = filterContext.Exception.GetTrue();
                filterContext.Result = Json(0, null, ex);
                filterContext.ExceptionHandled = true;

                if (XTrace.Debug) XTrace.WriteException(ex);

                return;
            }

            base.OnException(filterContext);
        }
#endif
        #endregion

        #region 服务器信息
        private static readonly String _OS = Environment.OSVersion + "";
        private static readonly String _MachineName = Environment.MachineName;
        private static readonly String _UserName = Environment.UserName;
        private static readonly String _LocalIP = NetHelper.MyIP() + "";

        /// <summary>服务器信息，用户健康检测</summary>
        /// <param name="state">状态信息</param>
        /// <returns></returns>
        public ActionResult Info(String state)
        {
            var asmx = AssemblyX.Entry;
            var asmx2 = AssemblyX.Create(Assembly.GetExecutingAssembly());

            var ip = HttpContext.GetUserHost();

            var rs = new
            {
                Server = asmx?.Name,
                asmx?.Version,
                asmx?.Compile,
                OS = _OS,
                MachineName = _MachineName,
                UserName = _UserName,
                ApiVersion = asmx2?.Version,

                UserHost = ip + "",
                Local = _LocalIP,
                Remote = ip + "",
                State = state,
                Time = DateTime.Now,
            };

            // 转字典
            var dic = rs.ToDictionary();

#if __CORE__
            // 完整显示地址和端口
            var conn = HttpContext.Connection;
            if (conn != null)
            {
                var addr = conn.LocalIpAddress;
                if (addr != null && addr.IsIPv4MappedToIPv6) addr = addr.MapToIPv4();
                dic["Local"] = $"{addr}:{conn.LocalPort}";

                addr = conn.RemoteIpAddress;
                if (addr != null && addr.IsIPv4MappedToIPv6) addr = addr.MapToIPv4();
                dic["Remote"] = $"{addr}:{conn.RemotePort}";
            }
#endif

            // 登录后，系统角色可以看看到进程
            var user = ManageProvider.User;
            if (user != null && user.Roles.Any(r => r.IsSystem)) dic["Process"] = GetProcess();

            return Json(0, null, dic);
        }

        private Object GetProcess()
        {
            var proc = Process.GetCurrentProcess();

            return new
            {
                Environment.ProcessorCount,
                ProcessId = proc.Id,
                Threads = proc.Threads.Count,
                Handles = proc.HandleCount,
                WorkingSet = proc.WorkingSet64,
                PrivateMemory = proc.PrivateMemorySize64,
                GCMemory = GC.GetTotalMemory(false),
                GC0 = GC.GetGeneration(0),
                GC1 = GC.GetGeneration(1),
                GC2 = GC.GetGeneration(2),
            };
        }
        #endregion

        #region 接口信息
#if __CORE__
        /// <summary>获取所有接口信息</summary>
        /// <returns></returns>
        public ActionResult Apis()
        {
            var set = new List<EndpointDataSource>();
            var eps = new List<String>();
            foreach (var item in _sources)
            {
                if (!set.Contains(item))
                {
                    set.Add(item);

                    //eps.AddRange(item.Endpoints);
                    foreach (var elm in item.Endpoints)
                    {
                        var area = elm.Metadata.GetMetadata<AreaAttribute>();
                        var disp = elm.Metadata.GetMetadata<DisplayNameAttribute>();
                        var desc = elm.Metadata.GetMetadata<ControllerActionDescriptor>();
                        var post = elm.Metadata.GetMetadata<HttpPostAttribute>();
                        if (desc == null) continue;

                        //var name = area == null ?
                        //    $"{desc.ControllerName}/{desc.ActionName}" :
                        //    $"{area?.RouteValue}/{desc.ControllerName}/{desc.ActionName}";

                        var sb = new StringBuilder();
                        sb.Append(post != null ? "POST " : "GET ");
                        sb.Append(desc.ControllerName);
                        sb.Append("/");
                        sb.Append(desc.ActionName);
                        sb.Append("(");
                        sb.Append(desc.MethodInfo.GetParameters().Join(",", pi => $"{pi.ParameterType.Name} {pi.Name}"));
                        sb.Append(")");

                        var name = sb.ToString();

                        if (!eps.Contains(name)) eps.Add(name);
                    }
                }
            }

            return Json(eps);
        }
#endif
        #endregion

        #region 用户
        /// <summary>用户搜索</summary>
        /// <param name="roleId"></param>
        /// <param name="departmentId"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public ActionResult UserSearch(Int32 roleId = 0, Int32 departmentId = 0, String key = null)
        {
            var exp = new WhereExpression();
            if (roleId > 0) exp &= _.RoleID == roleId;
            if (departmentId > 0) exp &= _.DepartmentID == departmentId;
            exp &= _.Enable == true;
            if (!key.IsNullOrEmpty()) exp &= _.Code.StartsWith(key) | _.Name.StartsWith(key) | _.DisplayName.StartsWith(key) | _.Mobile.StartsWith(key);

            var page = new PageParameter { PageSize = 20 };

            // 默认排序
            if (page.Sort.IsNullOrEmpty()) page.Sort = _.Name;

            var list = XCode.Membership.User.FindAll(exp, page);

            return Json(0, null, list.Select(e => new
            {
                e.ID,
                e.Code,
                e.Name,
                e.DisplayName,
                //e.DepartmentID,
                DepartmentName = e.Department?.ToString(),
            }).ToArray());
        }
        #endregion

        #region 部门
        /// <summary>网点搜索</summary>
        /// <param name="parentid"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public ActionResult DepartmentSearch(Int32 parentid = -1, String key = null)
        {
            var exp = new WhereExpression();
            if (parentid >= 0) exp &= Department._.ParentID == parentid;
            exp &= Department._.Enable == true & Department._.Visible == true;
            if (!key.IsNullOrEmpty()) exp &= Department._.Code.StartsWith(key) | Department._.Name.StartsWith(key) | Department._.FullName.StartsWith(key);

            var page = new PageParameter { PageSize = 20 };

            // 默认排序
            if (page.Sort.IsNullOrEmpty()) page.Sort = Department._.Name;

            var list = Department.FindAll(exp, page);

            return Json(0, null, list.Select(e => new
            {
                e.ID,
                e.Code,
                e.Name,
                e.FullName,
                //e.ManagerID,
                Manager = FindByID(e.ManagerID)?.ToString(),
            }).ToArray());
        }
        #endregion

        #region 地区
        /// <summary>地区信息</summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [AllowAnonymous]
        public ActionResult Area(Int32 id = 0)
        {
            var r = id <= 0 ? AreaX.Root : AreaX.FindByID(id);
            if (r == null) return Json(500, null, "找不到地区");

            return Json(0, null, new
            {
                r.ID,
                r.Name,
                r.FullName,
                r.ParentID,
                r.Level,
                r.Path,
                IdPath = r.AllParents.Where(e => e.ID > 0).Select(e => e.ID).Join("/"),
                r.ParentPath
            });
        }

        /// <summary>获取地区子级</summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [AllowAnonymous]
        public ActionResult AreaChilds(Int32 id = 0)
        {
            var r = id <= 0 ? AreaX.Root : AreaX.FindByID(id);
            if (r == null) return Json(500, null, "找不到地区");

            if (r.ID == 0)
                return Json(0, null, r.Childs.Where(e => e.Enable).Select(e => new { e.ID, e.Name, e.FullName, BigArea = e.GetBig() }).ToArray());
            else
                return Json(0, null, r.Childs.Where(e => e.Enable).Select(e => new { e.ID, e.Name, e.FullName }).ToArray());
        }

        /// <summary>获取地区父级</summary>
        /// <param name="id">查询地区编号</param>
        /// <param name="isContainSelf">是否包含查询的地区</param>
        /// <returns></returns>
        [AllowAnonymous]
        public ActionResult AreaParents(Int32 id = 0, Boolean isContainSelf = false)
        {
            var r = id <= 0 ? AreaX.Root : AreaX.FindByID(id);
            if (r == null) return Json(500, null, "找不到地区");

            var list = new List<Object>();
            foreach (var e in r.AllParents)
            {
                if (e.ID == 0) continue;
                if (r.ID == 0)
                    list.Add(new { e.ID, e.Name, e.FullName, e.ParentID, e.Level, BigArea = e.GetBig() });
                else
                    list.Add(new { e.ID, e.Name, e.FullName, e.ParentID, e.Level });
            }

            if (isContainSelf) list.Add(r);

            return Json(0, null, list);
        }

        /// <summary>获取地区所有父级</summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [AllowAnonymous]
        public ActionResult AreaAllParents(Int32 id = 0)
        {
            var r = id <= 0 ? AreaX.Root : AreaX.FindByID(id);
            if (r == null) return Json(500, null, "找不到地区");

            var rs = new List<AreaX>();
            foreach (var item in r.AllParents)
            {
                rs.AddRange(item.Parent.Childs.Where(e => e.Enable));
            }
            rs.AddRange(r.Parent.Childs);

            var list = new List<Object>();
            foreach (var e in rs)
            {
                if (e.ParentID == 0)
                    list.Add(new { e.ID, e.Name, e.ParentID, e.Level, BigArea = e.GetBig() });
                else
                    list.Add(new { e.ID, e.Name, e.ParentID, e.Level });
            }

            return Json(0, null, list);
        }
        #endregion

        #region 头像
        /// <summary>获取用户头像</summary>
        /// <param name="id">用户编号</param>
        /// <returns></returns>
        [AllowAnonymous]
        public virtual ActionResult Avatar(Int32 id)
        {
            if (id <= 0) throw new ArgumentNullException(nameof(id));

            var user = ManageProvider.Provider?.FindByID(id) as IUser;
            if (user == null) throw new Exception("用户不存在 " + id);

            var set = Setting.Current;
            var av = set.UploadPath.CombinePath(user.Avatar).GetBasePath();
            if (!System.IO.File.Exists(av)) throw new Exception("用户头像不存在 " + id);

#if __CORE__
            var vs = System.IO.File.ReadAllBytes(av);
            return File(vs, "image/png");
#else
            return File(av, "image/png");
#endif
        }
        #endregion
    }
}