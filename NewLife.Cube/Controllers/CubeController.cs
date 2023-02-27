using System.Buffers;
using System.ComponentModel;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using NewLife.Cube.Entity;
using NewLife.Data;
using NewLife.Reflection;
using XCode;
using XCode.Membership;
using static XCode.Membership.User;
using AreaX = XCode.Membership.Area;

namespace NewLife.Cube.Controllers;

/// <summary>魔方前端数据接口</summary>
[DisplayName("数据接口")]
[Route("[controller]/[action]")]
public class CubeController : ControllerBaseX
{
    private readonly IList<EndpointDataSource> _sources;

    /// <summary>构造函数</summary>
    /// <param name="sources"></param>
    public CubeController(IEnumerable<EndpointDataSource> sources)
    {
        _sources = sources.ToList();
    }

    #region 拦截
    ///// <summary>执行后</summary>
    ///// <param name="context"></param>
    //public override void OnActionExecuted(ActionExecutedContext context)
    //{
    //    if (context.Exception != null && !context.ExceptionHandled)
    //    {
    //        var ex = context.Exception.GetTrue();
    //        context.Result = Json(0, null, ex);
    //        context.ExceptionHandled = true;

    //        if (XTrace.Debug) XTrace.WriteException(ex);

    //        return;
    //    }

    //    base.OnActionExecuted(context);
    //}
    #endregion

    #region 服务器信息
    private static readonly String _OS = Environment.OSVersion + "";

    ///// <summary>服务器信息</summary>
    ///// <returns></returns>
    //[Route("[controller]")]
    //public ActionResult Get() => Info(null);

    /// <summary>服务器信息，用户健康检测</summary>
    /// <param name="state">状态信息</param>
    /// <returns></returns>
    [HttpGet]
    public ActionResult Info(String state)
    {
        var asmx = AssemblyX.Entry;
        var conn = HttpContext.Connection;
        var remote = conn.RemoteIpAddress;
        if (remote.IsIPv4MappedToIPv6) remote = remote.MapToIPv4();
        var ip = HttpContext.GetUserHost();

        var rs = new
        {
            asmx?.Name,
            asmx?.Title,
            asmx?.FileVersion,
            asmx?.Compile,
            OS = _OS,

            UserHost = ip?.ToString(),
            Remote = remote?.ToString(),
            Port = conn.LocalPort,
            Time = DateTime.Now,
            State = state,
        };

        return Json(0, null, rs);
    }
    #endregion

    #region 接口信息
    /// <summary>获取所有接口信息</summary>
    /// <returns></returns>
    [HttpGet]
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

        return Json(0, null, eps);
    }
    #endregion

    #region 用户
    /// <summary>用户搜索</summary>
    /// <param name="roleId"></param>
    /// <param name="departmentId"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    [HttpGet]
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
    [HttpGet]
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
    [HttpGet]
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
    [HttpGet]
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
    [HttpGet]
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
    [HttpGet]
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
    [HttpGet]
    public virtual ActionResult Avatar(Int32 id)
    {
        if (id <= 0) throw new ArgumentNullException(nameof(id));

        var user = ManageProvider.Provider?.FindByID(id) as IUser;
        if (user == null) throw new Exception("用户不存在 " + id);

        var set = Setting.Current;
        var av = "";
        if (!user.Avatar.IsNullOrEmpty() && !user.Avatar.StartsWith("/"))
        {
            av = set.AvatarPath.CombinePath(user.Avatar).GetBasePath();
            if (!System.IO.File.Exists(av)) av = null;
        }

        // 用于兼容旧代码
        if (av.IsNullOrEmpty() && !set.AvatarPath.IsNullOrEmpty())
        {
            av = set.AvatarPath.CombinePath(user.ID + ".png").GetBasePath();
            if (!System.IO.File.Exists(av)) av = null;
        }

        if (!System.IO.File.Exists(av)) throw new Exception("用户头像不存在 " + id);

        var vs = System.IO.File.ReadAllBytes(av);
        return File(vs, "image/png");
    }
    #endregion

    #region 字典参数        
    ///// <summary>
    ///// 保存字典参数到后台
    ///// </summary>
    ///// <param name="para">The para.</param>
    ///// <returns></returns>
    ///// <exception cref="System.ArgumentNullException">para</exception>
    //[HttpPost]
    //public ActionResult SaveParameter(Int32 userid, Parameter para)
    //{
    //    if(para == null) throw new ArgumentNullException(nameof(para));
    //    para.SaveAsync();

    //    return Ok();
    //}

    /// <summary>
    /// 根据用户、类别及具体的名字保存字典参数到后台
    /// </summary>
    /// <param name="userid">The user identifier.</param>
    /// <param name="category">The category.</param>
    /// <param name="name">The name.</param>
    /// <param name="value">The value.</param>
    /// <returns></returns>
    [HttpPost]
    public ActionResult SaveLayout(Int32 userid, String category, String name, String value)
    {
        if (!category.EqualIgnoreCase("LayoutSetting"))
            return Json(203, "非授权操作，不允许保存系统布局以外的信息");

        var para = Parameter.GetOrAdd(userid, category, name);
        para.SetItem("Value", value);
        para.Save();

        return Ok();
    }
    #endregion

    #region 附件
    /// <summary>
    /// 访问图片附件
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult> Image(String id)
    {
        if (id.IsNullOrEmpty()) return NotFound("非法附件编号");

        // 去掉仅用于装饰的后缀名
        var p = id.IndexOf('.');
        if (p > 0) id = id[..p];

        var att = Attachment.FindById(id.ToLong());
        if (att == null) return NotFound("找不到附件信息");

        // 如果附件不存在，则抓取
        var filePath = att.GetFilePath();
        if (filePath.IsNullOrEmpty() || !System.IO.File.Exists(filePath))
        {
            var url = att.Source;
            if (url.IsNullOrEmpty()) return NotFound("找不到附件文件");

            var rs = await att.Fetch(url);
            if (!rs) return NotFound("附件远程抓取失败");

            filePath = att.GetFilePath();
        }
        if (filePath.IsNullOrEmpty() || !System.IO.File.Exists(filePath)) return NotFound("附件文件不存在");

        if (!att.ContentType.IsNullOrEmpty())
            return PhysicalFile(filePath, att.ContentType, att.FileName);
        else
            return PhysicalFile(filePath, "image/png", att.FileName);
    }

    /// <summary>
    /// 访问附件
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult> File(String id)
    {
        if (id.IsNullOrEmpty()) return NotFound("非法附件编号");

        // 去掉仅用于装饰的后缀名
        var p = id.IndexOf('.');
        if (p > 0) id = id[..p];

        var att = Attachment.FindById(id.ToLong());
        if (att == null) return NotFound("找不到附件信息");

        // 如果附件不存在，则抓取
        var filePath = att.GetFilePath();
        if (filePath.IsNullOrEmpty() || !System.IO.File.Exists(filePath))
        {
            var url = att.Source;
            if (url.IsNullOrEmpty()) return NotFound("找不到附件文件");

            var rs = await att.Fetch(url);
            if (!rs) return NotFound("附件远程抓取失败");

            filePath = att.GetFilePath();
        }
        if (filePath.IsNullOrEmpty() || !System.IO.File.Exists(filePath)) return NotFound("附件文件不存在");

        if (!att.ContentType.IsNullOrEmpty() && !att.ContentType.EqualIgnoreCase("application/octet-stream"))
            return PhysicalFile(filePath, att.ContentType, att.FileName);
        else
            return PhysicalFile(filePath, "application/octet-stream", att.FileName, true);
    }
    #endregion
}