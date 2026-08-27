using System.ComponentModel;
using System.Text;
using System.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using NewLife.Caching;
using NewLife.Common;
using NewLife.Cube.Areas.Admin.Models;
using NewLife.Cube.Common;
using NewLife.Cube.Entity;
using NewLife.Cube.Models;
using NewLife.Cube.Services;
using NewLife.Cube.Services.Sso;
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
    private readonly ITenantContext _tenantContext;

    private Boolean _isMobile { get; set; } = false;

    static UserController()
    {
        ListFields.RemoveField("Avatar", "RoleIds", "Online", "Age", "Birthday", "LastLoginIP", "RegisterIP", "RegisterTime");
        ListFields.RemoveField("Phone", "Code", "Question", "Answer", "MailVerified", "MobileVerified");
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
            return $"<a href=\"/Admin/User/Edit?id={user.ID}\" target=\"_blank\"><img src=\"{user.GetAvatarUrl()}\" style=\"width:32px;height:32px;\" /></a>";
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

    /// <summary>实例化用户控制器</summary>
    /// <param name="passwordService"></param>
    /// <param name="cacheProvider"></param>
    /// <param name="userService"></param>
    /// <param name="tracer"></param>
    /// <param name="tenantContext">租户上下文</param>
    public UserController(PasswordService passwordService, ICacheProvider cacheProvider, UserService userService, ITracer tracer, ITenantContext tenantContext)
    {
        _passwordService = passwordService;
        _cache = cacheProvider.Cache;
        _userService = userService;
        _tracer = tracer;
        _tenantContext = tenantContext;
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
        //var tencentId = ManagerProviderHelper.GetTenantId(HttpContext);
        var tencentId = _tenantContext.TenantId;
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
            if (_tenantContext.TenantId == 0)//非租户验证
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
        var tenantId = _tenantContext.TenantId;
        var ms = OAuthConfig.GetValids(tenantId, GrantTypes.AuthorizationCode);
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
                    var client = OAuthClient.Create(tenantId, item.Name);
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
        ViewData["ChallengeKey"] = new KeyValuePair<String, String>(key, dicKey.Item1);

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
            EnableSms = set.EnableSms,
            EnableMail = set.EnableMail,
            //AutoRegister = set.AutoRegister,

            EnablePasswordComplexity = set.EnablePasswordComplexity,
            PasswordStrength = set.PaswordStrength,

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
        ServiceResult<IToken> result = null;
        var returnUrl = GetRequest("r");
        if (returnUrl.IsNullOrEmpty()) returnUrl = GetRequest("ReturnUrl");
        try
        {
            if (ModelState.IsValid)
            {
                result = _userService.Login(loginModel, HttpContext);
                if (result != null && result.IsSuccess && result.Data != null && !result.Data.AccessToken.IsNullOrEmpty())
                {
                    if (IsJsonRequest)
                    {
                        return Json(0, "ok", new { Token = result.Data.AccessToken });//兼容旧API
                    }

                    if (Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);

                    // 不要嵌入自己
                    if (returnUrl.EndsWithIgnoreCase("/Admin", "/Admin/User/Login")) returnUrl = null;

                    // 登录后自动绑定
                    var logId = Session["Cube_OAuthId"].ToLong();
                    if (logId > 0)
                    {
                        Session["Cube_OAuthId"] = null;
                        var bindingService = HttpContext.RequestServices.GetRequiredService<IUserBindingService>();
                        var log = bindingService.BindAfterLogin(logId);
                        if (log != null && log.Success && !log.RedirectUri.IsNullOrEmpty()) return Redirect(log.RedirectUri);
                    }

                    return RedirectToAction("Index", "Index", new { page = returnUrl });
                }
            }

            // 如果我们进行到这一步时某个地方出错，则重新显示表单
            ModelState.AddModelError("username", "提供的用户名或密码不正确。");
        }
        catch (Exception ex)
        {
            if (IsJsonRequest) return Json(500, ex.Message);

            ModelState.AddModelError("", ex.Message);
        }

        var dkey = DateTime.Now.Ticks.ToString();
        var dicKey = _cache.GetOrAdd(dkey, k => NCreateKeyPair(), 300);
        ViewData["ChallengeKey"] = new KeyValuePair<String, String>(dkey, dicKey.Item1);

        var model = GetViewModel(returnUrl);
        model.LoginTip = result?.Message;
        model.OAuthItems = OAuthConfig.GetVisibles(_tenantContext.TenantId);

        return _isMobile ? View("MLogin", model) : View(model);
    }

    /// <summary>获取登录密钥</summary>
    /// <remarks>对齐 WebAPI 版 GET /Auth/Challenge：返回新鲜的 challengeId 和 RSA 公钥。
    /// 公钥为公开信息，无需鉴权；前端应在提交登录前动态获取，避免页面停留过久导致密钥过期。</remarks>
    /// <returns>返回 challengeId 和 publicKey</returns>
    [AllowAnonymous]
    [HttpGet]
    public ActionResult GetLoginKey()
    {
        if (ManageProvider.User != null)
        {
            return Json(new
            {
                code = 500,
                message = "已登录，无需获取密钥"
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
                    challengeId = key,
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

    #region 账号管理
    /// <summary>注销账号（依据《个人信息保护法》提供账号注销功能）。禁用账号并清空个性化数据，吊销令牌、解绑三方</summary>
    /// <returns></returns>
    [HttpPost]
    [EntityAuthorize]
    public ActionResult CloseAccount()
    {
        var user = ManageProvider.User as User;
        if (user == null || user.ID <= 0) return Json(500, "用户未登录，请先登录");

        var result = _userService.CloseAccount(user, UserHost);
        if (!result.IsSuccess) return Json(500, result.Message);

        // 注销当前会话
        ManageProvider.Provider.Logout();

        return Json(0, "账号已注销");
    }

    /// <summary>导出个人数据（依据《个人信息保护法》提供数据可携带权）。下载 JSON 文件</summary>
    /// <returns></returns>
    [HttpGet]
    [EntityAuthorize]
    public ActionResult ExportData()
    {
        var cur = ManageProvider.User as XCode.Membership.User;
        if (cur == null) return RedirectToAction("Login");

        var user = XCode.Membership.User.FindByKeyForEdit(cur.ID);
        if (user == null) throw new Exception("无效用户编号！");

        var data = new Dictionary<String, Object>
        {
            ["说明"] = "本文件为您的个人数据导出，依据《中华人民共和国个人信息保护法》第四十五条提供数据可携带权。",
            ["导出时间"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            ["个人资料"] = new
            {
                user.ID,
                user.Name,
                user.DisplayName,
                user.Sex,
                user.Mail,
                user.MailVerified,
                user.Mobile,
                user.MobileVerified,
                user.Code,
                user.Avatar,
                user.RoleID,
                user.DepartmentID,
                user.Enable,
                user.Birthday,
                user.Logins,
                user.LastLogin,
                user.LastLoginIP,
                user.RegisterTime,
                user.RegisterIP,
            },
            ["第三方绑定"] = UserConnect.FindAllByUserID(user.ID).Select(e => new
            {
                e.Provider,
                e.OpenID,
                e.NickName,
                e.Enable,
                e.CreateTime,
                e.UpdateTime,
            }),
            ["令牌记录"] = UserToken.FindAllByUserID(user.ID).Select(e => new
            {
                Token = e.Token?.Length > 8 ? e.Token[..8] + "..." : e.Token,
                e.Expire,
                e.CreateTime,
                e.CreateIP,
            }),
        };

        var json = NewLife.Serialization.JsonHelper.ToJson(data, true);
        var bytes = Encoding.UTF8.GetBytes(json);
        var fileName = $"{user.Name}-个人数据-{DateTime.Now:yyyyMMdd}.json";

        return File(bytes, "application/json; charset=utf-8", fileName);
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
        var ms = OAuthConfig.GetValids(_tenantContext.TenantId, GrantTypes.AuthorizationCode);

        var model = new BindsModel
        {
            Name = user.Name,
            Connects = ucs,
            OAuthItems = ms,
        };

        if (IsJsonRequest) return Ok(data: model);

        return View(model);
    }

    /// <summary>注册（统一认证：用户名密码/手机验证码/邮箱验证码）</summary>
    /// <returns></returns>
    [HttpPost]
    [AllowAnonymous]
    public ActionResult Register(AuthRegisterModel registerModel)
    {
        var set = CubeSetting.Current;
        if (!set.AllowRegister) throw new Exception("禁止注册！");

        var returnUrl = GetRequest("r");
        if (returnUrl.IsNullOrEmpty()) returnUrl = GetRequest("ReturnUrl");

        try
        {
            // 复用统一认证服务：涵盖验证码校验、去重、租户绑定、登录 Cookie 写入（CompleteLogin）
            var result = _userService.Register(registerModel, HttpContext);
            if (result == null || !result.IsSuccess || result.Data == null)
            {
                var msg = result?.Message ?? "注册失败";
                if (IsJsonRequest) return Json(500, msg);

                throw new ArgumentException(msg, nameof(registerModel));
            }

            if (IsJsonRequest) return Json(0, "ok", new { Token = result.Data.AccessToken });

            // 注册成功（已写入登录 Cookie），跳转
            if (Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);

            return RedirectToAction(nameof(Login));
        }
        catch (ArgumentException aex)
        {
            ModelState.AddModelError(aex.ParamName, aex.Message);
        }

        var model = GetViewModel(returnUrl);
        model.OAuthItems = OAuthConfig.GetVisibles(_tenantContext.TenantId);

        return _isMobile ? View("MLogin", model) : View(model);
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

    /// <summary>吊销令牌。吊销指定用户的所有访问令牌，不依赖在线状态，适用于安全运维场景</summary>
    /// <param name="id">用户编号</param>
    /// <returns></returns>
    [DisplayName("吊销令牌")]
    [EntityAuthorize(PermissionFlags.Update)]
    public ActionResult RevokeTokens(Int32 id)
    {
        var user = FindByID(id);
        if (user == null)
        {
            if (IsJsonRequest) return Json(1, "用户不存在");
            return RedirectToAction("Edit", new { id });
        }

        var count = UserToken.RevokeByUser(id);

        LogProvider.Provider.WriteLog("用户", "吊销令牌", true,
            $"吊销用户[{user.Name}]的{count}个令牌", id, user.Name);

        if (IsJsonRequest) return Json(0, $"已吊销 {count} 个令牌");

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

        //var tid = HttpContext.GetTenantId();
        //var t = Tenant.FindById(tid);

        ViewData["TenantId"] = _tenantContext.TenantId;

        return View(model);
    }

    /// <summary>插入实体</summary>
    protected override Int32 OnInsert(User entity)
    {
        var ef = base.OnInsert(entity);

        if (_tenantContext.TenantId > 0)//默认插入当前租户下的用户
        {
            var tu = new TenantUser
            {
                TenantId = _tenantContext.TenantId,
                UserId = entity.ID,
                CreateIP = entity.RegisterIP,
                Enable = entity.Enable,

            };
            tu.InsertAsync();
        }

        return ef;
    }

    /// <summary>导出用户附加信息</summary>
    /// <param name="data"></param>
    /// <param name="page"></param>
    protected override void OnExportZip(IDictionary<Type, IEnumerable<IEntity>> data, Pager page)
    {
        // 导出用户时，附带导出所属角色和部门等信息
        // （仅用于演示，不具备实际业务意义）

        var p = page;
        var roleIds = p["roleIds"].SplitAsInt();
        var departmentIds = p["departmentId"].SplitAsInt();

        // 角色
        if (roleIds != null && roleIds.Length > 0)
            data[typeof(Role)] = Role.FindAll(Role._.ID.In(roleIds));
        else
            data[typeof(Role)] = Role.FindAllWithCache();

        // 部门
        if (departmentIds != null && departmentIds.Length > 0)
            data[typeof(Department)] = Department.FindAll(Department._.ID.In(departmentIds));
        else
            data[typeof(Department)] = Department.FindAllWithCache();

        var start = p["dtStart"].ToDateTime();
        var end = p["dtEnd"].ToDateTime();

        if (start.Year > 2000 || end.Year > 2000)
        {
            // 连接
            data[typeof(UserConnect)] = UserConnect.Search(start, end, null, null);
            data[typeof(UserToken)] = UserToken.Search(start, end, null, null);
        }
    }

    /// <summary>租户设置</summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPost]
    [EntityAuthorize]
    public ActionResult TenantSetting(TenantSettingModel model)
    {
        var tagTenantId = Request.Form["TagTenantId"].ToInt(-1);

        // 仅允许切换到当前用户所属的有效租户，防止越权写入任意租户Cookie
        if (tagTenantId > 0)
        {
            var user = ManageProvider.User;
            var tu = TenantUser.FindByTenantIdAndUserId(tagTenantId, user.ID);
            if (tu == null || !tu.Enable)
                throw new InvalidOperationException("无权切换到该租户！");

            HttpContext.SaveTenant(tagTenantId);
        }

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