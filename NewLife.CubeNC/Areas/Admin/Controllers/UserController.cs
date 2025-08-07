﻿using System.ComponentModel;
using System.Text;
using System.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using NewLife.Caching;
using NewLife.Common;
using NewLife.Cube.Areas.Admin.Models;
using NewLife.Cube.Entity;
using NewLife.Cube.Services;
using NewLife.Cube.ViewModels;
using NewLife.Data;
using NewLife.Log;
using NewLife.Reflection;
using NewLife.Security;
using NewLife.Web;
using XCode;
using XCode.Membership;
using static XCode.Membership.User;

namespace NewLife.Cube.Areas.Admin.Controllers;

/// <summary>用户控制器</summary>
[DataPermission(null, "ID={#userId}")]
[DisplayName("用户")]
[Description("系统基于角色授权，每个角色对不同的功能模块具备添删改查以及自定义权限等多种权限设定。")]
[AdminArea]
[Menu(100, true, Icon = "fa-user", HelpUrl = "https://newlifex.com/cube/cube_security", Mode = MenuModes.Admin | MenuModes.Tenant)]
public class UserController : EntityController<User, UserModel>
{
    /// <summary>用于防爆破登录。即使内存缓存，也有一定用处，最糟糕就是每分钟重试次数等于集群节点数的倍数</summary>
    private readonly ICache _cache;
    private readonly PasswordService _passwordService;
    private readonly UserService _userService;
    private readonly ITracer _tracer;

    private Boolean _isMobile { get; set; } = false;

    static UserController()
    {
        ListFields.RemoveField("Avatar", "RoleIds", "Online", "Age", "Birthday", "LastLoginIP", "RegisterIP", "RegisterTime");
        ListFields.RemoveField("Phone", "Code", "Question", "Answer");
        ListFields.RemoveField("MailVerified", "MobileVerified");
        ListFields.RemoveField("Ex1", "Ex2", "Ex3", "Ex4", "Ex5", "Ex6");
        ListFields.RemoveUpdateField();
        ListFields.RemoveField("Remark");

        {
            var df = ListFields.AddListField("AvatarImage", "Name");
            df.Header = "";
            //df.Text = "<img src=\"{Avatar}\" style=\"width:64px;height:64px;\" />";
            //df.Url = "/Admin/User/Edit?id={ID}";
            df.DataVisible = entity => !(entity as User).Avatar.IsNullOrEmpty();
            // 使用ILinkExtend，高度定制头像超链接
            df.AddService(new MyAvatar());
            df.Title = "{Remark}";
        }
        {
            var df = ListFields.GetField("Name") as ListField;
            df.Url = "/Admin/User/Edit?id={ID}";
            df.Target = "_blank";
        }
        {
            var df = ListFields.GetField("DisplayName") as ListField;
            df.Url = "/Admin/User/Edit?id={ID}";
            df.Target = "_blank";
            df.Title = "{Remark}";
        }

        {
            var df = AddFormFields.AddDataField("RoleId", "RoleName");
            df.DataSource = entity => Role.FindAllWithCache().Where(x => x.Enable).OrderByDescending(e => e.Sort).ToDictionary(e => e.ID, e => e.Name);
            AddFormFields.RemoveField("RoleName");
        }
        {
            var df = EditFormFields.AddDataField("RoleId", "RoleName");
            df.DataSource = entity => Role.FindAllWithCache().Where(x => x.Enable).OrderByDescending(e => e.Sort).ToDictionary(e => e.ID, e => e.Name);
            EditFormFields.RemoveField("RoleName");
        }

        {
            var df = AddFormFields.AddDataField("RoleIds", "RoleNames");
            df.DataSource = entity => Role.FindAllWithCache().Where(x => x.Enable).OrderByDescending(e => e.Sort).ToDictionary(e => e.ID, e => e.Name);
            AddFormFields.RemoveField("RoleNames");
        }
        {
            var df = EditFormFields.AddDataField("RoleIds", "RoleNames");
            df.DataSource = entity => Role.FindAllWithCache().Where(x => x.Enable).OrderByDescending(e => e.Sort).ToDictionary(e => e.ID, e => e.Name);
            EditFormFields.RemoveField("RoleNames");
        }
        {
            AddFormFields.GroupVisible = (entity, group) => (entity as User).ID == 0 && group != "扩展";
        }
        {
            var ff = AddFormFields.GetField("AreaId") as FormField;
            // 使用area4视图
            ff.ItemView = "_Area4";
        }
        {
            var ff = EditFormFields.GetField("AreaId") as FormField;
            //ff.ItemView = "_Area3";
            // 使用area4组件
            ff.ItemType = "area4";
        }
    }

    class MyAvatar : ILinkExtend
    {
        public String Resolve(DataField field, IModel data)
        {
            var user = data as User;
            return $"<a href=\"/Admin/User/Edit?id={user.ID}\" target=\"_blank\"><img src=\"{user.GetAvatarUrl()}\" style=\"width:64px;height:64px;\" /></a>";
        }
    }

    /// <summary>已重载。</summary>
    /// <param name="filterContext"></param>
    public override void OnActionExecuting(ActionExecutingContext filterContext)
    {
        base.OnActionExecuting(filterContext);

        var uAgent = Request.Headers["User-Agent"] + "";
        _isMobile = uAgent.Contains("Android") || uAgent.Contains("iPhone") || uAgent.Contains("iPad");

        if (filterContext.ActionDescriptor is ControllerActionDescriptor act &&
            act.ActionName.EqualIgnoreCase(nameof(Detail), nameof(Edit), nameof(Info), nameof(ChangePassword), nameof(Binds), nameof(TenantSetting)))
        {
            PageSetting.NavView = "_User_Nav";
            PageSetting.EnableNavbar = false;
        }
    }

    /// <summary>
    /// 实例化用户控制器
    /// </summary>
    /// <param name="passwordService"></param>
    /// <param name="cacheProvider"></param>
    /// <param name="userService"></param>
    /// <param name="tracer"></param>
    public UserController(PasswordService passwordService, ICacheProvider cacheProvider, UserService userService, ITracer tracer)
    {
        _passwordService = passwordService;
        _cache = cacheProvider.Cache;
        _userService = userService;
        _tracer = tracer;
    }

    /// <summary>搜索数据集</summary>
    /// <param name="p"></param>
    /// <returns></returns>
    protected override IEnumerable<User> Search(Pager p)
    {
        var id = p["id"].ToInt(-1);
        if (id > 0)
        {
            var list = new List<User>();
            var entity = FindByID(id);
            entity.Password = null;
            if (entity != null) list.Add(entity);
            return list;
        }

        var roleIds = p["roleIds"].SplitAsInt();
        var departmentIds = p["departmentId"].SplitAsInt();
        var areaIds = p["areaId"].SplitAsInt("/");
        var enable = p["enable"]?.ToBoolean();
        var start = p["dtStart"].ToDateTime();
        var end = p["dtEnd"].ToDateTime();
        var key = p["q"];

        if (areaIds.Length > 0)
        {
            var rs = areaIds.ToList();
            var r = Area.FindByID(areaIds[areaIds.Length - 1]);
            if (r != null)
            {
                // 城市，要下一级
                if (r.Level == 2)
                {
                    rs.AddRange(r.Childs.Select(e => e.ID));
                }
                // 省份，要下面两级
                else if (r.Level == 1)
                {
                    rs.AddRange(r.Childs.Select(e => e.ID));
                    foreach (var item in r.Childs)
                    {
                        rs.AddRange(item.Childs.Select(e => e.ID));
                    }
                }
            }
            areaIds = rs.ToArray();
        }

        IList<User> list2 = [];

        // 只读取租户相关的用户
        var tencentId = ManagerProviderHelper.GetTenantId(HttpContext);
        if (tencentId > 0)
        {
            list2 = XCode.Membership.User.SearchWithTenant(tencentId, roleIds, departmentIds, areaIds, enable, start, end, key, p);
        }
        else
        {
            list2 = XCode.Membership.User.Search(roleIds, departmentIds, areaIds, enable, start, end, key, p);
        }

        foreach (var user in list2)
        {
            user.Password = null;
        }

        return list2;
    }

    /// <summary>验证实体对象</summary>
    /// <param name="entity"></param>
    /// <param name="type"></param>
    /// <param name="post"></param>
    /// <returns></returns>
    protected override Boolean Valid(User entity, DataObjectMethodType type, Boolean post)
    {
        if (!post)
        {
            // 清空密码，不向浏览器输出
            //entity.Password = null;
            entity["Password"] = null;
        }

        if (post)
        {
            // 非系统管理员，禁止修改任何人的角色
            var user = ManageProvider.User;
            if (TenantContext.CurrentId == 0)//非租户验证
            {
                if (!user.Roles.Any(e => e.IsSystem) && entity is IEntity entity2)
                {
                    if (entity2.Dirtys["RoleID"]) throw new Exception("禁止修改角色！");
                    if (entity2.Dirtys["RoleIds"]) throw new Exception("禁止修改角色！");
                }
            }
        }

        if (post && type == DataObjectMethodType.Update)
        {
            var ds = (entity as IEntity).Dirtys;
            if (ds["Password"])
            {
                if (entity.Password.IsNullOrEmpty())
                    ds["Password"] = false;
                else
                    entity.Password = ManageProvider.Provider.PasswordProvider.Hash(entity.Password);
            }

            if (!entity.RoleIds.IsNullOrEmpty()) entity.RoleIds = entity.RoleIds == "-1" ? null : entity.RoleIds.Replace(",-1,", ",");
        }

        return base.Valid(entity, type, post);
    }

    #region 登录注销
    /// <summary>登录</summary>
    /// <returns></returns>
    [AllowAnonymous]
    public ActionResult Login()
    {
        var returnUrl = GetRequest("r");
        if (returnUrl.IsNullOrEmpty()) returnUrl = GetRequest("ReturnUrl");

        // 如果已登录，直接跳转
        if (ManageProvider.User != null)
        {
            if (Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            else
                return RedirectToAction("Index", "Index", new { page = returnUrl });
        }

        // 是否已完成第三方登录
        var logId = Session["Cube_OAuthId"].ToLong();

        // 如果禁用本地登录，且只有一个第三方登录，直接跳转，构成单点登录
        var ms = OAuthConfig.GetValids(GrantTypes.AuthorizationCode);
        var set = CubeSetting.Current;
        if (ms != null && !set.AllowLogin)
        {
            if (logId > 0) throw new Exception("已完成第三方登录，但无法绑定本地用户且没有开启自动注册，建议开启OAuth应用的自动注册");
            if (ms.Count == 0)
            {
                //throw new Exception("禁用了本地密码登录，且没有配置第三方登录");
                set.AllowLogin = true;
            }

            // 只有一个，跳转
            if (ms.Count == 1)
            {
                var url = $"~/Sso/Login?name={ms[0].Name}";
                if (!returnUrl.IsNullOrEmpty()) url += "&r=" + HttpUtility.UrlEncode(returnUrl);

                return Redirect(url);
            }
        }

        // 部分提供支持应用内免登录，直接跳转
        if (ms != null && ms.Count > 0 && logId == 0 && GetRequest("autologin") != "0")
        {
            var agent = Request.Headers.UserAgent + "";
            if (!agent.IsNullOrEmpty())
            {
                foreach (var item in ms)
                {
                    var client = OAuthClient.Create(item.Name);
                    if (client != null && client.Support(agent))
                    {
                        var url = $"~/Sso/Login?name={item.Name}";
                        if (!returnUrl.IsNullOrEmpty()) url += "&r=" + HttpUtility.UrlEncode(returnUrl);

                        return Redirect(url);
                    }
                }
            }
        }

        //ViewBag.IsShowTip = XCode.Membership.User.Meta.Count == 1;
        //ViewBag.ReturnUrl = returnUrl;

        var model = GetViewModel(returnUrl);
        model.OAuthItems = ms.Where(e => e.Visible).ToList();

        var key = DateTime.Now.Ticks.ToString();
        var dicKey = _cache.GetOrAdd(key, k => NCreateKeyPair(), 300);
        ViewData["pKey"] = new KeyValuePair<String, String>(key, dicKey.Item1);

        return _isMobile ? View("MLogin", model) : View(model);
    }

    private LoginViewModel GetViewModel(String returnUrl)
    {
        var set = CubeSetting.Current;
        var sys = SysConfig.Current;
        var model = new LoginViewModel
        {
            DisplayName = sys.DisplayName,

            AllowLogin = set.AllowLogin,
            AllowRegister = set.AllowRegister,
            //AutoRegister = set.AutoRegister,

            LoginTip = set.LoginTip,
            ResourceUrl = set.ResourceUrl,
            ReturnUrl = returnUrl,

            //OAuthItems = ms,
        };

        // 默认登录提示，没有新用户之前
        if (model.LoginTip.IsNullOrEmpty() && XCode.Membership.User.Meta.Count <= 1)
            model.LoginTip = "首个注册登录用户成为管理员，默认用户admin/admin，推荐第三方登录";

        if (model.ResourceUrl.IsNullOrEmpty()) model.ResourceUrl = "/Content";
        model.ResourceUrl = model.ResourceUrl.TrimEnd('/');

        // 是否使用Sso登录
        var appId = GetRequest("ssoAppId").ToInt();
        var app = App.FindById(appId);
        if (app != null)
        {
            model.DisplayName = app + "";
            model.Logo = app.Logo;
        }

        return model;
    }

    /// <summary>密码登录</summary>
    /// <returns></returns>
    [HttpPost()]
    [AllowAnonymous]
    public ActionResult Login(LoginModel loginModel)
    {
        var username = loginModel.Username;
        var password = loginModel.Password;
        var remember = loginModel.Remember;

        var pdic = loginModel.Pkey.IsNullOrEmpty() ? new Tuple<String, String>(null, null) : _cache.Get<Tuple<String, String>>(loginModel.Pkey);

        var ip = UserHost;
        var dkey = DateTime.Now.Ticks.ToString();
        var dicKey = _cache.GetOrAdd(dkey, k => NCreateKeyPair(), 300);
        ViewData["pKey"] = new KeyValuePair<String, String>(dkey, dicKey.Item1);

        // 连续错误校验
        var key = $"Login:{username}";
        var errors = _cache.Get<Int32>(key);
        var ipKey = $"Login:{ip}";
        var ipErrors = _cache.Get<Int32>(ipKey);

        //var peKey = $"Login:{loginModel.Pkey}";
        //var pErrors = _cache.Get<Int32>(peKey);

        using var span = _tracer?.NewSpan(nameof(Login), new { username, ip, errors });

        var set = CubeSetting.Current;
        var returnUrl = GetRequest("r");
        if (returnUrl.IsNullOrEmpty()) returnUrl = GetRequest("ReturnUrl");
        try
        {
            if (username.IsNullOrEmpty()) throw new ArgumentNullException(nameof(username), "用户名不能为空！");
            if (password.IsNullOrEmpty()) throw new ArgumentNullException(nameof(password), "密码不能为空！");
            //if (loginModel.Pkey.IsNullOrEmpty()) throw new ArgumentNullException("pkey", "加密令牌不能为空");

            if (errors >= set.MaxLoginError && set.MaxLoginError > 0)
                throw new InvalidOperationException($"[{username}]登录错误过多，请在{set.LoginForbiddenTime}秒后再试！");
            if (ipErrors >= set.MaxLoginError && set.MaxLoginError > 0)
                throw new InvalidOperationException($"IP地址[{UserHost}]登录错误过多，请在{set.LoginForbiddenTime}秒后再试！");

            var rsaKey = pdic.Item2;
            password = rsaKey.IsNullOrEmpty() ? password : Decrypt(rsaKey, password);

            var provider = ManageProvider.Provider;
            if (ModelState.IsValid && provider.Login(username, password, remember) != null)
            {
                // 登录成功，清空错误数
                if (errors > 0) _cache.Remove(key);
                if (ipErrors > 0) _cache.Remove(ipKey);
                // 移除秘钥私钥信息，避免重放
                if (!loginModel.Pkey.IsNullOrEmpty()) _cache.Remove(loginModel.Pkey);

                if (IsJsonRequest)
                {
                    var token = HttpContext.Items["jwtToken"];
                    return Json(0, "ok", new { /*provider.Current.ID,*/ Token = token });
                }

                //FormsAuthentication.SetAuthCookie(username, remember ?? false);

                // 记录在线统计
                var stat = UserStat.GetOrAdd(DateTime.Today);
                if (stat != null)
                {
                    stat.Logins++;
                    stat.SaveAsync(5_000);
                }

                // 设置租户
                HttpContext.ChooseTenant(provider.Current.ID);

                if (Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);

                // 不要嵌入自己
                if (returnUrl.EndsWithIgnoreCase("/Admin", "/Admin/User/Login")) returnUrl = null;

                // 登录后自动绑定
                var logId = Session["Cube_OAuthId"].ToLong();
                if (logId > 0)
                {
                    Session["Cube_OAuthId"] = null;
                    var log = NewLife.Cube.Controllers.SsoController.Provider.BindAfterLogin(logId);
                    if (log != null && log.Success && !log.RedirectUri.IsNullOrEmpty()) return Redirect(log.RedirectUri);
                }

                return RedirectToAction("Index", "Index", new { page = returnUrl });
            }

            // 如果我们进行到这一步时某个地方出错，则重新显示表单
            ModelState.AddModelError("username", "提供的用户名或密码不正确。");
        }
        catch (Exception ex)
        {
            // 登录失败比较重要，记录一下
            var action = ex is InvalidOperationException ? "风控" : "登录";
            LogProvider.Provider.WriteLog(typeof(User), action, false, ex.Message, 0, username, UserHost);
            XTrace.WriteLine("[{0}]登录失败！{1}", username, ex.Message);
            XTrace.WriteException(ex);

            span?.SetError(ex, null);

            // 累加错误数，首次出错时设置过期时间
            _cache.Increment(key, 1);
            _cache.Increment(ipKey, 1);
            var time = 300;
            if (set.LoginForbiddenTime > 0) time = set.LoginForbiddenTime;
            if (errors <= 0) _cache.SetExpire(key, TimeSpan.FromSeconds(time));
            if (ipErrors <= 0) _cache.SetExpire(ipKey, TimeSpan.FromSeconds(time));

            if (IsJsonRequest)
            {
                return Json(500, ex.Message);
            }

            ModelState.AddModelError("", ex.Message);
        }

        ////云飞扬2019-02-15修改，密码错误后会走到这，需要给ViewBag.IsShowTip重赋值，否则抛异常
        //ViewBag.IsShowTip = XCode.Membership.User.Meta.Count == 1;

        var model = GetViewModel(returnUrl);
        model.OAuthItems = OAuthConfig.GetVisibles();

        return _isMobile ? View("MLogin", model) : View(model);
    }

    /// <summary>获取登录密钥</summary>
    /// <returns>返回pKey和publicKey</returns>
    [AllowAnonymous]
    [HttpGet]
    public ActionResult GetLoginKey(String token)
    {
        if (ManageProvider.User != null)
        {
            return Json(new
            {
                code = 500,
                message = "已登录，无需获取密钥"
            });
        }
        var validToken = "5tU3Xr6PkF6AHfdCu7Sr";

        if (token != validToken)
        {
            return Json(new
            {
                code = 500,
                message = "非法请求，token错误"
            });
        }
        try
        {
            var key = DateTime.Now.Ticks.ToString();
            var dicKey = _cache.GetOrAdd(key, k => NCreateKeyPair(), 300);

            return Json(new
            {
                code = 0,
                message = "ok",
                data = new
                {
                    pKey = key,
                    publicKey = dicKey.Item1
                }
            });
        }
        catch (Exception ex)
        {
            return Json(new
            {
                code = 500,
                message = ex.Message
            });
        }
    }

    /// <summary>注销</summary>
    /// <returns></returns>
    [AllowAnonymous]
    public ActionResult Logout()
    {
        var returnUrl = GetRequest("r");
        if (returnUrl.IsNullOrEmpty()) returnUrl = GetRequest("ReturnUrl");

        var set = CubeSetting.Current;
        if (set.LogoutAll)
        {
            // 如果是单点登录，则走单点登录注销
            var name = Session["Cube_Sso"] as String;
            if (!name.IsNullOrEmpty())
            {
                UserService.ClearOnline(ManageProvider.User as User);

                return Redirect($"~/Sso/Logout?name={name}&r={HttpUtility.UrlEncode(returnUrl)}");
            }
            //if (!name.IsNullOrEmpty()) return RedirectToAction("Logout", "Sso", new
            //{
            //    area = "",
            //    name,
            //    r = returnUrl
            //});
        }

        ManageProvider.Provider.Logout();

        if (IsJsonRequest) return Ok();

        if (!returnUrl.IsNullOrEmpty()) return Redirect(returnUrl);

        return RedirectToAction(nameof(Login));
    }
    #endregion

    /// <summary>获取用户资料</summary>
    /// <param name="id"></param>
    /// <returns></returns>
    //[AllowAnonymous]
    [EntityAuthorize]
    public ActionResult Info(Int32 id)
    {
        //if (id == null || id.Value <= 0) throw new Exception("无效用户编号！");

        var user = ManageProvider.User as XCode.Membership.User;
        if (user == null) return RedirectToAction("Login");

        if (id > 0 && id != user.ID) throw new Exception("禁止查看非当前登录用户资料");

        user = XCode.Membership.User.FindByKeyForEdit(user.ID);
        if (user == null) throw new Exception("无效用户编号！");

        //user.Password = null;
        user["Password"] = null;

        if (IsJsonRequest)
        {
            var userInfo = new UserInfo();
            userInfo.Copy(user);
            userInfo.SetPermission(user.Roles);
            userInfo.SetRoleNames(user.Roles);

            return Json(0, "ok", userInfo);
        }

        // 用于显示的列
        if (ViewBag.Fields == null) ViewBag.Fields = OnGetFields(ViewKinds.EditForm, null);
        ViewBag.Factory = Factory;

        // 必须指定视图名，因为其它action会调用
        //return View("Info", user);
        return _isMobile ? View("MInfo", user) : View("Info", user);
    }

    /// <summary>更新用户资料</summary>
    /// <param name="user"></param>
    /// <returns></returns>
    [HttpPost]
    //[AllowAnonymous]
    [EntityAuthorize]
    public async Task<ActionResult> Info(User user)
    {
        var cur = ManageProvider.User;
        if (cur == null) return RedirectToAction("Login");

        if (user.ID != cur.ID) throw new Exception("禁止修改非当前登录用户资料");

        var entity = user as IEntity;
        if (entity.Dirtys["Name"]) throw new Exception("禁止修改用户名！");
        if (entity.Dirtys["RoleID"]) throw new Exception("禁止修改角色！");
        if (entity.Dirtys["RoleIds"]) throw new Exception("禁止修改角色！");
        if (entity.Dirtys["Enable"]) throw new Exception("禁止修改禁用！");

        var file = HttpContext.Request.Form.Files["avatar"];
        if (file != null)
        {
            var ext = Path.GetExtension(file.FileName);
            if (!ext.EqualIgnoreCase(".png", ".jpg", ".gif", ".bmp", ".tiff", ".svg"))
                throw new Exception("仅支持上传图片文件！");

            //var set = CubeSetting.Current;
            //var fileName = user.ID + Path.GetExtension(file.FileName);
            var att = await SaveFile(user, file, null, null);
            if (att != null) user.Avatar = ViewHelper.GetAttachmentUrl(att);
        }

        user.Update();

        return Info(user.ID);
    }

    ///// <summary>保存文件</summary>
    ///// <param name="entity">实体对象</param>
    ///// <param name="file">文件</param>
    ///// <param name="uploadPath">上传目录，默认使用UploadPath配置</param>
    ///// <param name="fileName">文件名，如若指定则忽略前面的目录</param>
    ///// <returns></returns>
    //protected override Task<Attachment> SaveFile(User entity, IFormFile file, String uploadPath, String fileName)
    //{
    //    // 修改保存目录和文件名
    //    var set = CubeSetting.Current;
    //    if (file.Name.EqualIgnoreCase("avatar")) fileName = entity.ID + Path.GetExtension(file.FileName);

    //    return base.SaveFile(entity, file, set.AvatarPath, fileName);
    //}

    /// <summary>修改密码</summary>
    /// <returns></returns>
    //[AllowAnonymous]
    [EntityAuthorize]
    public ActionResult ChangePassword()
    {
        var user = ManageProvider.User as XCode.Membership.User;
        if (user == null) return RedirectToAction("Login");

        var name = Session["Cube_Sso"] as String;
        var model = new ChangePasswordModel
        {
            Name = user.Name,
            SsoName = name,
        };

        return _isMobile ? View("MChangePassword", model) : View("ChangePassword", model);
    }

    /// <summary>修改密码</summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPost]
    //[AllowAnonymous]
    [EntityAuthorize]
    public ActionResult ChangePassword(ChangePasswordModel model)
    {
        if (model.NewPassword.IsNullOrWhiteSpace()) throw new ArgumentException($"新密码不能为 Null 或空白", nameof(model.NewPassword));
        if (model.NewPassword2.IsNullOrWhiteSpace()) throw new ArgumentException($"确认密码不能为 Null 或空白", nameof(model.NewPassword2));
        if (model.NewPassword != model.NewPassword2) throw new ArgumentException($"两次输入密码不一致", nameof(model.NewPassword));

        if (!_passwordService.Valid(model.NewPassword)) throw new ArgumentException($"密码太弱，要求8位起且包含数字大小写字母和符号", nameof(model.NewPassword));

        // SSO 登录不需要知道原密码就可以修改，原则上更相信外方，同时也避免了直接第三方登录没有设置密码的尴尬
        var ssoName = Session["Cube_Sso"] as String;
        var requireOldPass = ssoName.IsNullOrEmpty();
        if (requireOldPass)
        {
            if (model.OldPassword.IsNullOrWhiteSpace()) throw new ArgumentException($"原密码不能为 Null 或空白", nameof(model.OldPassword));
            if (model.NewPassword == model.OldPassword) throw new ArgumentException($"修改密码不能与原密码一致", nameof(model.NewPassword));
        }

        var current = ManageProvider.User;
        if (current == null) return RedirectToAction("Login");

        var user = ManageProvider.Provider.ChangePassword(current.Name, model.NewPassword, requireOldPass ? model.OldPassword : null);

        //(user as User).Update();

        ViewBag.StatusMessage = "修改成功！";

        if (IsJsonRequest) return Ok(ViewBag.StatusMessage);

        return ChangePassword();
    }

    /// <summary>用户绑定</summary>
    /// <returns></returns>
    //[AllowAnonymous]
    [EntityAuthorize]
    public ActionResult Binds()
    {
        var user = ManageProvider.User as XCode.Membership.User;
        if (user == null) return RedirectToAction("Login");

        user = XCode.Membership.User.FindByKeyForEdit(user.ID);
        if (user == null) throw new Exception("无效用户编号！");

        // 第三方绑定
        var ucs = UserConnect.FindAllByUserID(user.ID);
        var ms = OAuthConfig.GetValids(GrantTypes.AuthorizationCode);

        var model = new BindsModel
        {
            Name = user.Name,
            Connects = ucs,
            OAuthItems = ms,
        };

        if (IsJsonRequest) return Ok(data: model);

        return View(model);
    }

    /// <summary>注册</summary>
    /// <returns></returns>
    [HttpPost]
    [AllowAnonymous]
    public ActionResult Register(RegisterModel registerModel)
    {
        var email = registerModel.Email;
        var username = registerModel.Username;
        var password = registerModel.Password;
        var password2 = registerModel.Password2;

        var set = CubeSetting.Current;
        if (!set.AllowRegister) throw new Exception("禁止注册！");

        try
        {
            //if (String.IsNullOrEmpty(email)) throw new ArgumentNullException("email", "邮箱地址不能为空！");
            if (String.IsNullOrEmpty(username)) throw new ArgumentNullException("username", "用户名不能为空！");
            if (String.IsNullOrEmpty(password)) throw new ArgumentNullException("password", "密码不能为空！");
            if (String.IsNullOrEmpty(password2)) throw new ArgumentNullException("password2", "重复密码不能为空！");
            if (password != password2) throw new ArgumentOutOfRangeException("password2", "两次密码必须一致！");

            if (!_passwordService.Valid(password)) throw new ArgumentException($"密码太弱，要求8位起且包含数字大小写字母和符号", nameof(password));

            // 不得使用OAuth前缀
            foreach (var item in OAuthConfig.GetValids())
            {
                if (username.StartsWithIgnoreCase($"{item.Name}_"))
                    throw new ArgumentException(nameof(username), $"禁止使用[{item.Name}_]前缀！");
            }

            // 去重判断
            var user = FindByName(username);
            if (user != null) throw new ArgumentException(nameof(username), $"用户[{username}]已存在！");

            user = FindByMail(email);
            if (user != null) throw new ArgumentException(nameof(email), $"邮箱[{email}]已存在！");

            var r = Role.GetOrAdd(set.DefaultRole);
            //user = new User()
            //{
            //    Name = username,
            //    Password = password,
            //    Mail = email,
            //    RoleID = r.ID,
            //    Enable = true
            //};
            //user.Register();
            var user2 = ManageProvider.Provider.Register(username, password, r.ID, true);

            // 注册成功
        }
        catch (ArgumentException aex)
        {
            ModelState.AddModelError(aex.ParamName, aex.Message);
        }

        var model = GetViewModel(null);
        model.OAuthItems = OAuthConfig.GetVisibles();

        return View("Login", model);
    }

    /// <summary>清空密码</summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [EntityAuthorize(PermissionFlags.Update)]
    public ActionResult ClearPassword(Int32 id)
    {
        if (!ManageProvider.User.Roles.Any(e => e.IsSystem)) throw new Exception("清除密码操作需要管理员权限，非法操作！");

        // 前面表单可能已经清空密码
        var user = FindByID(id);
        //user.Password = "nopass";
        user.Password = null;
        user.SaveWithoutValid();

        if (IsJsonRequest) return Ok();

        return RedirectToAction("Edit", new { id });
    }

    /// <summary>设置租户</summary>
    /// <returns></returns>
    [EntityAuthorize]
    public ActionResult TenantSetting()
    {
        var user = ManageProvider.User as User;
        if (user == null) return RedirectToAction("Login");

        var tlist = TenantUser.FindAllByUserId(user.ID);
        var model = new TenantSettingModel(user.Name)
        {
            Tenants = tlist.ToDictionary(e => e.TenantId, v => v.TenantName)
        };

        if (IsJsonRequest) return Ok(data: model);

        var tid = HttpContext.GetTenantId();
        var t = Tenant.FindById(tid);

        ViewData["TenantId"] = t?.Id ?? 0;

        return View(model);
    }

    /// <summary>插入实体</summary>
    protected override Int32 OnInsert(User entity)
    {
        var ef = base.OnInsert(entity);

        if (TenantContext.CurrentId > 0)//默认插入当前租户下的用户
        {
            var tu = new TenantUser
            {
                TenantId = TenantContext.CurrentId,
                UserId = entity.ID,
                CreateIP = entity.RegisterIP,
                Enable = entity.Enable,

            };
            tu.InsertAsync();
        }

        return ef;
    }

    /// <summary>租户设置</summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPost]
    [EntityAuthorize]
    public ActionResult TenantSetting(TenantSettingModel model)
    {
        var tagTenantId = Request.Form["TagTenantId"].ToInt(-1);

        if (tagTenantId > 0) HttpContext.SaveTenant(tagTenantId);

        ViewBag.StatusMessage = "保存成功";
        if (IsJsonRequest) return Ok(ViewBag.StatusMessage);

        return TenantSetting();
    }

    ///// <summary>设置租户</summary>
    ///// <param name="userId">当前用户编号</param>
    //private void SetTenant(Int32 userId)
    //{
    //    var tenantUser = TenantUser.FindAllByUserId(userId);
    //    if (tenantUser != null && tenantUser.Count > 0)
    //    {
    //        var entity = tenantUser.FirstOrDefault().Tenant;

    //        if (entity == null || !entity.Enable) return;

    //        HttpContext.SaveTenant(tenantUser.FirstOrDefault().TenantId);
    //    }
    //}

    ///// <summary>批量启用</summary>
    ///// <param name="keys"></param>
    ///// <returns></returns>
    //[EntityAuthorize(PermissionFlags.Update)]
    //public ActionResult EnableSelect(String keys) => EnableOrDisableSelect();

    ///// <summary>批量禁用</summary>
    ///// <param name="keys"></param>
    ///// <returns></returns>
    //[EntityAuthorize(PermissionFlags.Update)]
    //public ActionResult DisableSelect(String keys) => EnableOrDisableSelect(false);

    //private ActionResult EnableOrDisableSelect(Boolean isEnable = true)
    //{
    //    var count = 0;
    //    var ids = GetRequest("keys").SplitAsInt();
    //    if (ids.Length > 0)
    //    {
    //        foreach (var id in ids)
    //        {
    //            var user = FindByID(id);
    //            if (user != null && user.Enable != isEnable)
    //            {
    //                user.Enable = isEnable;
    //                user.Update();

    //                Interlocked.Increment(ref count);
    //            }
    //        }
    //    }

    //    return JsonRefresh($"共{(isEnable ? "启用" : "禁用")}[{count}]个用户");
    //}

    #region 密码辅助工具
    /// <summary>
    /// 创建RSA密钥对（临时方案，后续会newlife.core中增加相关生成代码）
    /// </summary>
    /// <param name="strength"></param>
    /// <returns></returns>
    public static Tuple<String, String> NCreateKeyPair(Int32 strength = 2048)
    {
        var result = RSTool.GeneratePemKey();

        return new Tuple<String, String>(result[0], result[1]);
    }

    /// <summary>解密代码</summary>
    /// <param name="privateKey"></param>
    /// <param name="decryptString"></param>
    /// <returns></returns>
    public static String Decrypt(String privateKey, String decryptString)
    {
        var decryptedData = RSAHelper.Decrypt(Convert.FromBase64String(decryptString), privateKey, false);

        return Encoding.UTF8.GetString(decryptedData);
    }
    #endregion
}