using System.ComponentModel;
using System.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewLife.Caching;
using NewLife.Common;
using NewLife.Cube.Areas.Admin.Models;
using NewLife.Cube.Entity;
using NewLife.Cube.Enums;
using NewLife.Cube.Extensions;
using NewLife.Cube.Models;
using NewLife.Cube.Services;
using NewLife.Cube.Web.Models;
using NewLife.Log;
using NewLife.Reflection;
using NewLife.Serialization;
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
[Menu(100, true, Icon = "fa-user")]
public class UserController : EntityController<User, UserModel>
{
    #region 短信验证码缓存Key前缀常量
    // 登录相关的缓存Key已移至UserService中统一管理

    // 短信绑定/重置相关缓存Key前缀已移至UserService统一管理
    #endregion

    /// <summary>用于防爆破登录。即使内存缓存，也有一定用处，最糟糕就是每分钟重试次数等于集群节点数的倍数</summary>
    private readonly ICache _cache;
    private readonly UserService _userService;
    private readonly PasswordService _passwordService;
    private readonly ISmsVerifyCode _smsVerifyCode;

    static UserController()
    {
        ListFields.RemoveField("Avatar", "RoleIds", "Online", "Age", "Birthday", "LastLoginIP", "RegisterIP", "RegisterTime");
        ListFields.RemoveField("Phone", "Code", "Question", "Answer");
        ListFields.RemoveField("Ex1", "Ex2", "Ex3", "Ex4", "Ex5", "Ex6");
        ListFields.RemoveUpdateField();
        ListFields.RemoveField("Remark");

        {
            // 为RoleId搜索字段增加LovCode
            var df = SearchFields.GetField(XCode.Membership.User._.RoleID);
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
    public UserController(UserService userService, PasswordService passwordService, ICacheProvider cacheProvider, ISmsVerifyCode smsVerifyCode = null)
    {
        _userService = userService;
        _passwordService = passwordService;
        _cache = cacheProvider.Cache;
        _smsVerifyCode = smsVerifyCode;
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
    public ApiResponse<TokenInfo> Login(LoginModel model)
    {
        var res = new TokenInfo();
        if (string.IsNullOrWhiteSpace(model.Username))
            return res.ToFailApiResponse("用户名不能为空");
        if (string.IsNullOrWhiteSpace(model.Password))
            return res.ToFailApiResponse("密码不能为空");

        LoginResult loginResult = null;
        try
        {
            //if (ModelState.IsValid)
            //{
            loginResult = _userService.Login(model, HttpContext);
            if (loginResult == null || loginResult.AccessToken.IsNullOrEmpty())
                return res.ToFailApiResponse(loginResult?.Result); //登录失败

            res.AccessToken = loginResult.AccessToken;
            res.RefreshToken = loginResult.RefreshToken;
            return res.ToOkApiResponse("登录成功");
            //}
            //else
            //{
            //    return res.ToFailApiResponse(ModelState.ToJson());//TODO 处理模型状态错误
            //}

        }
        catch (Exception ex)
        {
            return res.ToErrorApiResponse(ex.Message);
        }

        //TODO 地址跳转，应该直接操作Response，而不是返回一个视图。API暂时不需要跳转，由前端处理
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

        return Json(0, "ok", new { Token = tokens.Item1, RefreshToken = tokens.Item2 });
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
    public async Task<ActionResult> Info(User user)
    {
        var cur = ManageProvider.User;
        if (cur == null) return RedirectToAction("Login");

        if (user.ID != cur.ID) throw new Exception("禁止修改非当前登录用户资料");

        var entity = user as IEntity;
        if (entity.Dirtys["Name"]) throw new Exception("禁止修改用户名！");
        if (entity.Dirtys["RoleID"]) throw new Exception("禁止修改角色！");
        if (entity.Dirtys["Enable"]) throw new Exception("禁止修改禁用！");

        var file = HttpContext.Request.Form.Files["avatar"];
        if (file != null)
        {
            var ext = Path.GetExtension(file.FileName);
            //if (ext.EqualIgnoreCase(".exe", ".bat", ".com", ".vbs", ".js", ".jar", ".msi", ".lnk"))
            //    throw new Exception("禁止上传可执行文件！");
            if (!ext.EqualIgnoreCase(".png", ".jpg", ".gif", ".bmp", ".tiff", ".svg"))
                throw new Exception("仅支持上传图片文件！");

            //var set = CubeSetting.Current;
            //var fileName = user.ID + Path.GetExtension(file.FileName);
            var att = await SaveFile(user, file, null, null);
            if (att != null) user.Avatar = att.FilePath;
        }

        user.Update();

        var user2 = user.CloneEntity();
        user2.Password = null;

        return Json(0, null, user);
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
        var ms = OAuthConfig.GetValids(TenantContext.CurrentId, GrantTypes.AuthorizationCode);

        var model = new BindsModel
        {
            Name = user.Name,
            Connects = ucs,
            OAuthItems = ms,
        };

        return Json(0, null, model);
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

        var tenantId = TenantContext.CurrentId;
        try
        {
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

    #region 验证码登录
    /// <summary>发送登录验证码：手机、邮箱 </summary>
    /// <param name="model">登录模型:Username手机号/邮箱</param>
    /// <returns></returns>
    [HttpPost]
    [AllowAnonymous]
    public async Task<ApiResponse<Boolean>> SendVerifyCode(VerifyCodeModel model)
    {
        var ip = UserHost;
        try
        {
            var result = await _userService.SendVerifyCode(model, ip);

            return true.ToOkApiResponse("验证码已发送");
        }
        catch (Exception ex)
        {
            return false.ToRemotingErrorApiResponse("发送失败：" + ex.Message);
        }
    }
     
    #endregion

    #region 绑定手机号
    ///// <summary>发送绑定手机验证码</summary>
    ///// <param name="model">登录模型:Username手机号</param>
    ///// <returns></returns>
    //[HttpPost]
    //[EntityAuthorize]
    //public async Task<ApiResponse<Boolean>> SendBindMobileSms(LoginModel model)
    //{
    //    var mobile = model.Username?.Trim() ?? "";

    //    // 1. 验证手机号格式
    //    if (mobile.IsNullOrEmpty()) return false.ToFailApiResponse("手机号不能为空");
    //    if (!SmsService.IsValidPhone(mobile)) return false.ToFailApiResponse("手机号格式不正确");

    //    // 2. 检查当前用户是否已登录
    //    var currentUser = ManageProvider.User as User;
    //    if (currentUser == null || currentUser.ID <= 0) return false.ToFailApiResponse("用户未登录，请先登录");

    //    // 3. 检查手机号是否已被其他用户绑定
    //    var existingUser = XCode.Membership.User.FindByMobile(mobile);
    //    if (existingUser != null && existingUser.ID > 0 && existingUser.ID != currentUser.ID)
    //        return false.ToFailApiResponse("该手机号已被其他账户绑定");

    //    // 4. 检查短信服务是否启用
    //    var set = CubeSetting.Current;
    //    if (!set.EnableSms) return false.ToErrorApiResponse("短信验证码功能未启用");

    //    if (_smsVerifyCode == null) return false.ToErrorApiResponse("短信服务未配置");

    //    // 检查短信配置是否完整
    //    if (set.SmsAccessKeyId.IsNullOrEmpty() || set.SmsAccessKeySecret.IsNullOrEmpty())
    //        return false.ToErrorApiResponse("短信AccessKey未配置，请在系统参数中配置SmsAccessKeyId和SmsAccessKeySecret");

    //    if (set.SmsSignName.IsNullOrEmpty())
    //        return false.ToErrorApiResponse("短信签名未配置，请在系统参数中配置SmsSignName");

    //    var ip = UserHost;
    //    var ipKey = $"SmsBind:IP:{ip}";

    //    // 防止频繁发送（IP限制）
    //    var ipCount = _cache.Get<Int32>(ipKey);
    //    if (ipCount >= 5) return false.ToFailApiResponse("发送频繁，请稍后再试");

    //    // 防止频繁发送（手机号限制，60秒内只能发一次）
    //    var lastSend = _cache.Get<DateTime>($"SmsBind:LastSend:{mobile}");
    //    if (lastSend > DateTime.MinValue && (DateTime.Now - lastSend).TotalSeconds < 60)
    //    {
    //        var wait = 60 - (Int32)(DateTime.Now - lastSend).TotalSeconds;
    //        return false.ToFailApiResponse($"请{wait}秒后再试");
    //    }

    //    try
    //    {
    //        // 发送短信验证码
    //        var expireMinutes = set.SmsExpireMinutes;
    //        var code = SmsService.GenerateVerifyCode();
    //        var rs = await _smsVerifyCode.SendBind(mobile, code, expireMinutes);
    //        if (String.IsNullOrWhiteSpace(rs) || rs != "OK")
    //            return false.ToRemotingErrorApiResponse("短信发送失败");


    //        // 缓存验证码用于校验
    //        var codeKey = $"SmsBind:Code:{mobile}";
    //        _cache.Set(codeKey, code, expireMinutes * 60);

    //        // 记录发送时间
    //        _cache.Set($"SmsBind:LastSend:{mobile}", DateTime.Now, 60);

    //        // 累计IP发送次数
    //        _cache.Increment(ipKey, 1);
    //        if (ipCount <= 0) _cache.SetExpire(ipKey, TimeSpan.FromMinutes(10));

    //        LogProvider.Provider.WriteLog(typeof(User), "发送绑定验证码", true, $"手机号：{mobile}", currentUser.ID, currentUser + "", ip);

    //        return true.ToOkApiResponse("验证码已发送");
    //    }
    //    catch (Exception ex)
    //    {
    //        XTrace.WriteException(ex);
    //        LogProvider.Provider.WriteLog(typeof(User), "发送绑定验证码", false, $"手机号：{mobile}，错误：{ex.Message}", currentUser.ID, currentUser + "", ip);
    //        return false.ToRemotingErrorApiResponse("发送失败：" + ex.Message);
    //    }
    //}

    /// <summary>绑定手机号到当前登录用户</summary>
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

        var result = _userService.BindMobile(mobile, code, currentUser, ip);
        return result.Success ? true.ToOkApiResponse(result.Message) : false.ToFailApiResponse(result.Message);
    }
    #endregion

    #region 手机验证码重置密码
    ///// <summary>发送重置密码验证码</summary>
    ///// <param name="model">登录模型:Username手机号</param>
    ///// <returns></returns>
    //[HttpPost]
    //[AllowAnonymous]
    //public async Task<ApiResponse<Boolean>> SendResetPasswordSms(LoginModel model)
    //{
    //    var mobile = model.Username?.Trim() ?? "";

    //    // 1. 验证手机号格式
    //    if (mobile.IsNullOrEmpty()) return false.ToFailApiResponse("手机号不能为空");
    //    if (!SmsService.IsValidPhone(mobile)) return false.ToFailApiResponse("手机号格式不正确");

    //    // 2. 检查手机号是否已注册
    //    var existingUser = XCode.Membership.User.FindByMobile(mobile);
    //    if (existingUser == null || existingUser.ID <= 0)
    //        return false.ToFailApiResponse("该手机号未注册");

    //    // 3. 检查短信服务是否启用
    //    var set = CubeSetting.Current;
    //    if (!set.EnableSms) return false.ToErrorApiResponse("短信验证码功能未启用");

    //    if (_smsVerifyCode == null) return false.ToErrorApiResponse("短信服务未配置");

    //    // 检查短信配置是否完整
    //    if (set.SmsAccessKeyId.IsNullOrEmpty() || set.SmsAccessKeySecret.IsNullOrEmpty())
    //        return false.ToErrorApiResponse("短信AccessKey未配置，请在系统参数中配置SmsAccessKeyId和SmsAccessKeySecret");

    //    if (set.SmsSignName.IsNullOrEmpty())
    //        return false.ToErrorApiResponse("短信签名未配置，请在系统参数中配置SmsSignName");

    //    var ip = UserHost;
    //    var ipKey = $"SmsReset:IP:{ip}";

    //    // 防止频繁发送（IP限制）
    //    var ipCount = _cache.Get<Int32>(ipKey);
    //    if (ipCount >= 5) return false.ToFailApiResponse("发送频繁，请稍后再试");

    //    // 防止频繁发送（手机号限制，60秒内只能发一次）
    //    var lastSend = _cache.Get<DateTime>($"SmsReset:LastSend:{mobile}");
    //    if (lastSend > DateTime.MinValue && (DateTime.Now - lastSend).TotalSeconds < 60)
    //    {
    //        var wait = 60 - (Int32)(DateTime.Now - lastSend).TotalSeconds;
    //        return false.ToFailApiResponse($"请{wait}秒后再试");
    //    }

    //    try
    //    {
    //        // 发送短信验证码
    //        var expireMinutes = set.SmsExpireMinutes;
    //        var code = SmsService.GenerateVerifyCode();
    //        var rs = await _smsVerifyCode.SendReset(mobile, code, expireMinutes);
    //        if (String.IsNullOrWhiteSpace(rs) || rs != "OK")
    //            return false.ToRemotingErrorApiResponse("短信发送失败");
    //        // 缓存验证码用于校验
    //        var codeKey = $"SmsReset:Code:{mobile}";
    //        _cache.Set(codeKey, code, expireMinutes * 60);

    //        // 记录发送时间
    //        _cache.Set($"SmsReset:LastSend:{mobile}", DateTime.Now, 60);

    //        // 累计IP发送次数
    //        _cache.Increment(ipKey, 1);
    //        if (ipCount <= 0) _cache.SetExpire(ipKey, TimeSpan.FromMinutes(10));

    //        LogProvider.Provider.WriteLog(typeof(User), "发送重置密码验证码", true, $"手机号：{mobile}", 0, mobile, ip);

    //        return true.ToOkApiResponse("验证码已发送");
    //    }
    //    catch (Exception ex)
    //    {
    //        XTrace.WriteException(ex);
    //        LogProvider.Provider.WriteLog(typeof(User), "发送重置密码验证码", false, $"手机号：{mobile}，错误：{ex.Message}", 0, mobile, ip);
    //        return false.ToRemotingErrorApiResponse("发送失败：" + ex.Message);
    //    }
    //}

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

        var result = _userService.ResetPassword(mobile, code, newPassword, confirmPassword, ip);
        return result.Success ? true.ToOkApiResponse(result.Message) : false.ToFailApiResponse(result.Message);
    }
    #endregion
}