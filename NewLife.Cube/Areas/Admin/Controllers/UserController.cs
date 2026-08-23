using System.ComponentModel;
using System.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NewLife.Caching;
using NewLife.Common;
using NewLife.Cube.Areas.Admin.Models;
using NewLife.Cube.Entity;
using NewLife.Cube.Extensions;
using NewLife.Cube.Models;
using NewLife.Cube.Services;
using NewLife.Reflection;
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
[Menu(100, true, Icon = "User", Mode = MenuModes.Admin | MenuModes.Tenant)]
public class UserController : EntityController<User, UserModel>
{
    /// <summary>用于防爆破登录。即使内存缓存，也有一定用处，最糟糕就是每分钟重试次数等于集群节点数的倍数</summary>
    private readonly ICache _cache;
    private readonly UserService _userService;
    private readonly PasswordService _passwordService;
    private readonly ITenantContext _tenantContext;

    static UserController()
    {
        ListFields.RemoveField("Avatar", "RoleIds", "Online", "Age", "Birthday", "LastLoginIP", "RegisterIP", "RegisterTime");
        ListFields.RemoveField("Phone", "Code", "Question", "Answer", "MailVerified", "MobileVerified");
        ListFields.RemoveField("Ex1", "Ex2", "Ex3", "Ex4", "Ex5", "Ex6");
        ListFields.RemoveUpdateField();
        ListFields.RemoveField("Remark");

        {
            // 为RoleId搜索字段增加LovCode
            var df = SearchFields.GetField(_.RoleID);
            df.LovCode = "Role";
        }

        {
            var df = ListFields.AddListField("Link", "Logins");
            //df.Header = "链接";
            df.HeaderTitle = "第三方登录的链接信息";
            df.DisplayName = "链接";
            df.Title = "第三方登录的链接信息";
            df.Url = "/Admin/UserConnect?userId={ID}";
        }

        {
            var df = ListFields.AddListField("Token", "Logins");
            //df.Header = "令牌";
            df.DisplayName = "令牌";
            df.Url = "/Admin/UserToken?userId={ID}";
        }

        {
            var df = ListFields.AddListField("Log", "Logins");
            //df.Header = "日志";
            df.DisplayName = "日志";
            df.Url = "/Admin/Log?userId={ID}";
        }

        {
            var df = ListFields.AddListField("OAuthLog", "Logins");
            //df.Header = "OAuth日志";
            df.DisplayName = "OAuth日志";
            df.Url = "/Admin/OAuthLog?userId={ID}";
        }

        {
            var df = AddFormFields.AddDataField("RoleIds", "RoleNames");
            df.DataSource = entity => Role.FindAllWithCache().OrderByDescending(e => e.Sort).ToDictionary(e => e.ID, e => e.Name);
            AddFormFields.RemoveField("RoleNames");
        }
        //{
        //    var df = AddFormFields.GetField("RegisterTime");
        //    df.DataVisible = (e, f) => f.Name != "RegisterTime";
        //}

        {
            var df = EditFormFields.AddDataField("RoleIds", "RoleNames");
            df.DataSource = entity => Role.FindAllWithCache().OrderByDescending(e => e.Sort).ToDictionary(e => e.ID, e => e.Name);
            EditFormFields.RemoveField("RoleNames");
        }

        {
            AddFormFields.GroupVisible = (entity, group) => (entity as User).ID == 0 && group != "扩展";
        }
    }

    /// <summary>实例化用户控制器</summary>
    /// <param name="userService"></param>
    /// <param name="passwordService"></param>
    /// <param name="cacheProvider"></param>
    /// <param name="smsVerifyCode"></param>
    /// <param name="tenantContext">租户上下文</param>
    public UserController(UserService userService, PasswordService passwordService, ICacheProvider cacheProvider, ITenantContext tenantContext)
    {
        _userService = userService;
        _passwordService = passwordService;
        _cache = cacheProvider.Cache;
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

        //var roleId = p["roleId"].ToInt(-1);
        var roleIds = p["roleIds"].SplitAsInt();
        //var departmentId = p["departmentId"].ToInt(-1);
        var departmentIds = p["departmentId"].SplitAsInt();
        var areaIds = p["areaId"].SplitAsInt("/");
        var enable = p["enable"]?.ToBoolean();
        var start = p["dtStart"].ToDateTime();
        var end = p["dtEnd"].ToDateTime();
        var key = p["q"];

        //p.RetrieveState = true;

        //return XCode.Membership.User.Search(roleId, departmentId, enable, start, end, key, p);

        //var exp = new WhereExpression();
        //if (roleId >= 0) exp &= _.RoleID == roleId | _.RoleIds.Contains("," + roleId + ",");
        //if (roleIds != null && roleIds.Length > 0) exp &= _.RoleID.In(roleIds) | _.RoleIds.Contains("," + roleIds.Join(",") + ",");
        //if (departmentId >= 0) exp &= _.DepartmentID == departmentId;
        //if (departmentIds != null && departmentIds.Length > 0) exp &= _.DepartmentID.In(departmentIds);
        //if (enable != null) exp &= _.Enable == enable.Value;
        //exp &= _.LastLogin.Between(start, end);
        //if (!key.IsNullOrEmpty()) exp &= _.Code.StartsWith(key) | _.Name.StartsWith(key) | _.DisplayName.StartsWith(key) | _.Mobile.StartsWith(key) | _.Mail.StartsWith(key);

        //var list2 = XCode.Membership.User.FindAll(exp, p);

        if (areaIds.Length > 0)
        {
            var rs = areaIds.ToList();
            var r = Area.FindByID(areaIds[areaIds.Length - 1]);
            if (r != null)
                // 城市，要下一级
                if (r.Level == 2)
                    rs.AddRange(r.Childs.Select(e => e.ID));
                // 省份，要下面两级
                else if (r.Level == 1)
                {
                    rs.AddRange(r.Childs.Select(e => e.ID));
                    foreach (var item in r.Childs)
                        rs.AddRange(item.Childs.Select(e => e.ID));
                }
            areaIds = rs.ToArray();
        }

        //if (roleId > 0) roleIds.Add(roleId);
        //if (departmentId > 0) departmentIds.Add(departmentId);
        var list2 = XCode.Membership.User.Search(roleIds, departmentIds, areaIds, enable, start, end, key, p);

        foreach (var user in list2)
            user.Password = null;

        return list2;
    }

    /// <summary>验证实体对象</summary>
    /// <param name="entity"></param>
    /// <param name="type"></param>
    /// <param name="post"></param>
    /// <returns></returns>
    protected override Boolean Valid(User entity, DataObjectMethodType type, Boolean post)
    {
        if (!post && type == DataObjectMethodType.Update)
            // 清空密码，不向浏览器输出
            //entity.Password = null;
            entity["Password"] = null;

        if (post && type == DataObjectMethodType.Update)
        {
            var ds = (entity as IEntity).Dirtys;
            if (ds["Password"])
                if (entity.Password.IsNullOrEmpty())
                    ds["Password"] = false;
                else
                    entity.Password = ManageProvider.Provider.PasswordProvider.Hash(entity.Password);

            if (!entity.RoleIds.IsNullOrEmpty()) entity.RoleIds = entity.RoleIds == "-1" ? null : entity.RoleIds.Replace(",-1,", ",");
        }

        return base.Valid(entity, type, post);
    }

    #region 登录注销
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

    /// <summary>登录验证：账号密码登录、手机登录、邮箱登录 </summary>
    /// <returns></returns>
    [HttpPost]
    [AllowAnonymous]
    public ApiResponse<TokenModel> Login(LoginModel model)
    {
        var res = new TokenModel();
        if (String.IsNullOrWhiteSpace(model.Username))
            return res.ToFailApiResponse("用户名不能为空");
        if (String.IsNullOrWhiteSpace(model.Password))
            return res.ToFailApiResponse("密码不能为空");

        try
        {
            var loginResult = _userService.Login(model, HttpContext);
            if (loginResult?.Data == null || loginResult.Data.AccessToken.IsNullOrEmpty())
                return res.ToFailApiResponse(loginResult?.Message); //登录失败

            res.AccessToken = loginResult.Data.AccessToken;
            res.RefreshToken = loginResult.Data.RefreshToken;
            return res.ToOkApiResponse("登录成功");

        }
        catch (Exception ex)
        {
            return res.ToFailApiResponse(ex.Message);
        }

        // 地址跳转，应该直接操作Response，而不是返回一个视图。API暂时不需要跳转，由前端处理
        var returnUrl = GetRequest("r");
        if (returnUrl.IsNullOrEmpty()) returnUrl = GetRequest("ReturnUrl");
        var viewModel = GetViewModel(returnUrl);
        //viewModel.LoginTip = loginResult?.Result;
        //viewModel.OAuthItems = OAuthConfig.GetVisibles(TenantContext.CurrentId);
        //return Json(0, null, viewModel);
        return res.ToFailApiResponse("");
        ////Response.Redirect(returnUrl,true); 
    }

    /// <summary>刷新令牌</summary>
    /// <returns></returns>
    [HttpPost]
    [AllowAnonymous]
    public ActionResult RefreshToken(RefreshTokenModel refreshTokenModel)
    {
        var userName = refreshTokenModel.UserName;
        var refreshToken = refreshTokenModel.RefreshToken;
        var user = ManageProvider.Provider.FindByName(userName);

        var tokens = HttpContext.RefreshToken(user, refreshToken);

        return Json(0, "ok", new { Token = tokens.AccessToken, RefreshToken = tokens.RefreshToken, tokens.ExpireIn });
    }

    /// <summary>注销</summary>
    /// <returns></returns>
    [AllowAnonymous]
    [HttpGet]
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

        return Json(0, "ok");
    }
    #endregion

    /// <summary>获取当前登录用户资料</summary>
    /// <returns></returns>
    [HttpGet]
    [EntityAuthorize]
    public ActionResult Info()
    {
        if (ManageProvider.User is not User user) throw new Exception("当前登录用户无效！");

        user = XCode.Membership.User.FindByKeyForEdit(user.ID);
        if (user == null) throw new Exception("无效用户编号！");

        user["Password"] = null;

        var userInfo = new Models.UserInfo();
        userInfo.Copy(user);
        userInfo.SetPermission(user.Roles);
        userInfo.SetRoleNames(user.Roles);

        return Json(0, "ok", userInfo);

    }

    /// <summary>更新用户资料</summary>
    /// <param name="user"></param>
    /// <returns></returns>
    [HttpPost]
    [EntityAuthorize]
    public ActionResult Info(User user)
    {
        var cur = ManageProvider.User;
        if (cur == null) return RedirectToAction("Login");

        if (user.ID != cur.ID) throw new Exception("禁止修改非当前登录用户资料");

        var entity = user as IEntity;
        // 自助更新：仅当用户名实际变更时才拦截。
        // 原逻辑「请求出现 Name 字段即拦」会与移动端回传当前用户名冲突，导致昵称无法保存。
        // 改为值比对：Name 与当前登录用户名一致则放行。
        if (entity.Dirtys["Name"] && !user.Name.EqualIgnoreCase(cur.Name))
            throw new Exception("禁止修改用户名！");
        if (entity.Dirtys["RoleID"]) throw new Exception("禁止修改角色！");
        if (entity.Dirtys["Enable"]) throw new Exception("禁止修改禁用！");

        // 头像上传已拆分为独立接口 UploadFile（POST /Admin/User/UploadFile），
        // 本接口仅负责文本字段更新（昵称/邮箱/手机等）以及 avatar 字段回填。
        // 前端在上传头像后，将 UploadFile 返回的文件路径回填到 avatar 字段再调用本接口持久化。
        user.Update();

        return Json(0, null, user);
    }

    /// <summary>上传头像。复用基类 UploadFile（SaveFile 核心一致），仅做三件事：
    /// 1) 增加登录鉴权（基类 UploadFile 无 [EntityAuthorize]，直接复用会变成未登录可访问的上传端点）；
    /// 2) 强制 id 等于当前登录用户，防止越权给其它用户上传头像；
    /// 3) 仅允许图片类型（覆写 ValidateUploadFile，在基类「非空 + 危险扩展名黑名单」基础上加图片白名单）。
    /// 返回附件信息 { attId, filePath, contentType }，前端再调用 Info(avatar=filePath) 持久化头像。</summary>
    [HttpPost]
    [EntityAuthorize]
    public override async Task<ActionResult> UploadFile(IFormFile file, String id = null, String title = null)
    {
        var cur = ManageProvider.User;
        if (cur == null) return RedirectToAction("Login");

        // 强制只能为当前登录用户上传头像，避免越权
        var targetId = id.IsNullOrEmpty() ? cur.ID + "" : id;
        if (!targetId.EqualIgnoreCase(cur.ID + ""))
            return new JsonResult(new { error = "只能为当前登录用户上传头像！" });

        return await base.UploadFile(file, cur.ID + "", title);
    }

    /// <summary>头像上传校验：在基类「非空 + 危险扩展名黑名单」基础上，限制仅图片类型</summary>
    protected override Boolean ValidateUploadFile(IFormFile file, out String error)
    {
        if (!base.ValidateUploadFile(file, out error)) return false;

        var ext = Path.GetExtension(file.FileName);
        if (!ext.EqualIgnoreCase(".png", ".jpg", ".jpeg", ".gif", ".bmp", ".tiff", ".svg"))
        {
            error = "仅支持上传图片文件！";
            return false;
        }
        return true;
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
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPost]
    //[AllowAnonymous]
    [EntityAuthorize]
    public ActionResult<String> ChangePassword(ChangePasswordModel model)
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

        //ViewBag.StatusMessage = "修改成功！";
        //return Ok(ViewBag.StatusMessage);

        var res = false.ToOkApiResponse();
        return Json(res.Code, res.Message, res.Data);
        //return this.Json(0, null, "修改成功！");
    }

    /// <summary>用户绑定</summary>
    /// <returns></returns>
    //[AllowAnonymous]
    [EntityAuthorize]
    [HttpGet]
    public ActionResult Binds()
    {
        var user = ManageProvider.User as User;
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

        return Json(0, null, model);
    }

    private Int32 ResolveRegisterTenantId()
    {
        var set = CubeSetting.Current;
        if (!set.EnableTenant) return 0;

        var appId = HttpContext.Request.Headers["X-App-Id"] + "";
        if (!appId.IsNullOrEmpty())
        {
            var config = OAuthConfig.FindByAppId(appId);
            if (config == null) throw new ArgumentException($"应用{nameof(OAuthConfig.AppId)}未配置", nameof(appId));

            if (config.TenantId > 0)
            {
                var tenant = Tenant.FindById(config.TenantId);
                if (tenant == null || !tenant.Enable)
                    throw new ArgumentException($"租户[{config.TenantId}]不存在或已禁用", nameof(appId));
            }

            return config.TenantId;
        }

        var tenantStr = HttpContext.Request.Headers["X-Tenant"] + "";
        if (!tenantStr.IsNullOrEmpty())
        {
            var tenant = Tenant.FindByCode(tenantStr);
            if (tenant == null || !tenant.Enable)
                throw new ArgumentException($"租户[{tenantStr}]不存在或已禁用", nameof(tenantStr));

            return tenant.Id;
        }

        if (TenantContext.Current.GetTenantMode() == TenantMode.Tenant) return TenantContext.CurrentId;

        // Shadow 期：无租户标识注册兼容放行，不自动绑定；Enforce 期：必须显式带租户
        if (set.TenantEnforceMode == TenantEnforceModes.Shadow) return 0;

        throw new ArgumentException("多租户模式下，注册必须携带X-App-Id或X-Tenant租户信息");
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

        var tenantId = _tenantContext.TenantId;
        try
        {
            tenantId = ResolveRegisterTenantId();
            if (tenantId > 0) TenantContext.Current = new TenantContext { TenantId = tenantId };

            //if (String.IsNullOrEmpty(email)) throw new ArgumentNullException("email", "邮箱地址不能为空！");
            if (String.IsNullOrEmpty(username)) throw new ArgumentNullException("username", "用户名不能为空！");
            if (String.IsNullOrEmpty(password)) throw new ArgumentNullException("password", "密码不能为空！");
            if (String.IsNullOrEmpty(password2)) throw new ArgumentNullException("password2", "重复密码不能为空！");
            if (password != password2) throw new ArgumentOutOfRangeException("password2", "两次密码必须一致！");

            if (!_passwordService.Valid(password)) throw new ArgumentException($"密码太弱，要求8位起且包含数字大小写字母和符号", nameof(password));

            // 不得使用OAuth前缀
            foreach (var item in OAuthConfig.GetValids(tenantId))
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
            var user2 = ManageProvider.Provider.Register(username, password, r.ID, true);

            if (user2 != null && user2 is User user3)
            {
                var changed = false;
                if (!email.IsNullOrEmpty() && !email.EqualIgnoreCase(user3.Mail))
                {
                    user3.Mail = email;
                    // user3.MailVerified = true;
                    changed = true;
                }

                if (user3.RegisterIP.IsNullOrEmpty())
                {
                    user3.RegisterIP = UserHost;
                    changed = true;
                }
                if (user3.RegisterTime.Year < 2000)
                {
                    user3.RegisterTime = DateTime.Now;
                    changed = true;
                }

                if (changed) user3.Update();
            }

            // 多租户开启且解析到租户时，自动绑定用户到该租户（参考 SSO 登录的租户绑定流程）
            if (set.EnableTenant && tenantId > 0 && user2 != null)
            {
                var tenantUser = TenantUser.FindByTenantIdAndUserId(tenantId, user2.ID);
                if (tenantUser == null)
                {
                    tenantUser = new TenantUser
                    {
                        TenantId = tenantId,
                        UserId = user2.ID,
                        Enable = true,
                        CreateIP = UserHost,
                        CreateTime = DateTime.Now,
                    };
                    tenantUser.Insert();
                }
                else if (!tenantUser.Enable)
                {
                    tenantUser.Enable = true;
                    tenantUser.Update();
                }
            }

            // 注册成功
        }
        catch (ArgumentException aex)
        {
            ModelState.AddModelError(aex.ParamName, aex.Message);
            return Json(500, aex.Message, null);//api版本发生异常时应及时返回错误信息
        }

        var model = GetViewModel(null);
        model.OAuthItems = OAuthConfig.GetVisibles(tenantId);

        return Json(0, null, model);
    }

    /// <summary>清空密码</summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [EntityAuthorize(PermissionFlags.Update)]
    [HttpPost]
    public ActionResult ClearPassword(Int32 id)
    {
        if (!ManageProvider.User.Roles.Any(e => e.IsSystem)) throw new Exception("清除密码操作需要管理员权限，非法操作！");

        // 前面表单可能已经清空密码
        var user = FindByID(id);
        //user.Password = "nopass";
        user.Password = null;
        user.SaveWithoutValid();

        return Json(0, "ok");
    }

    /// <summary>吊销令牌。吊销指定用户的所有访问令牌，不依赖在线状态，适用于安全运维场景</summary>
    /// <param name="id">用户编号</param>
    /// <returns></returns>
    [DisplayName("吊销令牌")]
    [EntityAuthorize(PermissionFlags.Update)]
    [HttpPost]
    public ActionResult RevokeTokens(Int32 id)
    {
        var user = FindByID(id);
        if (user == null) return Json(1, "用户不存在");

        var count = UserToken.RevokeByUser(id);

        LogProvider.Provider.WriteLog("用户", "吊销令牌", true,
            $"吊销用户[{user.Name}]的{count}个令牌", id, user.Name);

        return Json(0, $"已吊销 {count} 个令牌");
    }

    #region  验证码
    /// <summary>发送验证码：手机、邮箱 </summary>
    /// <param name="model"> </param>
    /// <remarks>登录模型:Username手机号/邮箱; action:login/bind/reset/notify
    /// </remarks>
    /// <returns></returns>
    [HttpPost]
    [AllowAnonymous]
    public async Task<ApiResponse<Int64>> SendVerifyCode(VerifyCodeModel model)
    {
        try
        {
            var ip = UserHost;
            var result = await _userService.SendVerifyCode(model, ip);
            return result.Id.ToOkApiResponse("验证码已发送");
        }
        catch (Exception ex)
        {
            return 0L.ToRemotingErrorApiResponse("发送失败：" + ex.Message);
        }
    }


    /// <summary>验证码绑定（手机号/邮箱）</summary>
    /// <param name="model">Username为手机号，Password为验证码</param>
    /// <returns></returns>
    [HttpPost]
    [EntityAuthorize]
    public ApiResponse<Boolean> BindByVerifyCode(LoginModel model)
    {
        var mobile = model.Username?.Trim() ?? "";
        var code = model.Password?.Trim() ?? "";
        var currentUser = ManageProvider.User;
        var ip = UserHost;

        var result = _userService.BindByVerifyCode(mobile, code, currentUser, ip);
        return result.IsSuccess ? true.ToOkApiResponse(result.Message) : false.ToFailApiResponse(result.Message);
    }
    #endregion

    #region 手机验证码重置密码 
    /// <summary>通过手机验证码重置密码</summary>
    /// <param name="model">重置密码模型</param>
    /// <returns></returns>
    [HttpPost]
    [AllowAnonymous]
    public ApiResponse<Boolean> ResetByVerifyCode(ResetPwdModel model)
    {
        var mobile = model.Username?.Trim() ?? "";
        var code = model.Code?.Trim() ?? "";
        var newPassword = model.NewPassword?.Trim() ?? "";
        var confirmPassword = model.ConfirmPassword?.Trim() ?? "";
        var ip = UserHost;

        var result = _userService.ResetPassword(mobile, code, newPassword, confirmPassword, "", ip);
        return result.IsSuccess ? true.ToOkApiResponse(result.Message) : false.ToFailApiResponse(result.Message);
    }
    #endregion
}