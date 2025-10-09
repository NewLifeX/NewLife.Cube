﻿using System.ComponentModel;
using System.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewLife.Caching;
using NewLife.Common;
using NewLife.Cube.Areas.Admin.Models;
using NewLife.Cube.Entity;
using NewLife.Cube.Extensions;
using NewLife.Cube.Services;
using NewLife.Log;
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
[Menu(100, true, Icon = "fa-user")]
public class UserController : EntityController<User, UserModel>
{
    /// <summary>用于防爆破登录。即使内存缓存，也有一定用处，最糟糕就是每分钟重试次数等于集群节点数的倍数</summary>
    private readonly ICache _cache;
    private readonly PasswordService _passwordService;

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

    /// <summary>
    /// 实例化用户控制器
    /// </summary>
    /// <param name="passwordService"></param>
    /// <param name="cacheProvider"></param>
    public UserController(PasswordService passwordService, ICacheProvider cacheProvider)
    {
        _passwordService = passwordService;
        _cache = cacheProvider.Cache;
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

    /// <summary>密码登录</summary>
    /// <returns></returns>
    [HttpPost]
    [AllowAnonymous]
    public ActionResult Login(LoginModel loginModel)
    {
        var username = loginModel.Username;
        var password = loginModel.Password;
        var remember = loginModel.Remember;

        // 连续错误校验
        var key = $"CubeLogin:{username}";
        var errors = _cache.Get<Int32>(key);
        var ipKey = $"CubeLogin:{UserHost}";
        var ipErrors = _cache.Get<Int32>(ipKey);

        var set = CubeSetting.Current;
        var returnUrl = GetRequest("r");
        if (returnUrl.IsNullOrEmpty()) returnUrl = GetRequest("ReturnUrl");
        try
        {
            if (username.IsNullOrEmpty()) throw new ArgumentNullException(nameof(username), "用户名不能为空！");
            if (password.IsNullOrEmpty()) throw new ArgumentNullException(nameof(password), "密码不能为空！");

            if (errors >= set.MaxLoginError && set.MaxLoginError > 0) throw new InvalidOperationException($"[{username}]登录错误过多，请在{set.LoginForbiddenTime}秒后再试！");
            if (ipErrors >= set.MaxLoginError && set.MaxLoginError > 0) throw new InvalidOperationException($"IP地址[{UserHost}]登录错误过多，请在{set.LoginForbiddenTime}秒后再试！");

            var provider = ManageProvider.Provider;
            if (ModelState.IsValid && provider.Login(username, password, remember) != null)
            {
                // 登录成功，清空错误数
                if (errors > 0) _cache.Remove(key);
                if (ipErrors > 0) _cache.Remove(ipKey);

                var tokens = HttpContext.IssueTokenAndRefreshToken(provider.Current, TimeSpan.FromSeconds(set.TokenExpire));
                return Json(0, "ok", new { Token = tokens.Item1, RefreshToken = tokens.Item2 });
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

            // 累加错误数，首次出错时设置过期时间
            _cache.Increment(key, 1);
            _cache.Increment(ipKey, 1);
            var time = 300;
            if (set.LoginForbiddenTime > 0) time = set.LoginForbiddenTime;
            if (errors <= 0) _cache.SetExpire(key, TimeSpan.FromSeconds(time));
            if (ipErrors <= 0) _cache.SetExpire(ipKey, TimeSpan.FromSeconds(time));

            return Json(500, ex.Message);
        }

        ////云飞扬2019-02-15修改，密码错误后会走到这，需要给ViewBag.IsShowTip重赋值，否则抛异常
        //ViewBag.IsShowTip = XCode.Membership.User.Meta.Count == 1;

        var model = GetViewModel(returnUrl);
        model.OAuthItems = OAuthConfig.GetVisibles();

        return Json(0, null, model);
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

        var tokens= HttpContext.RefreshToken(user, refreshToken);

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

        var userInfo = new UserInfo();
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


        var res = "".ToOkApiResponse();
        return Json(res.Code, res.Message, res.Data);
        return this.Json(0, null, "修改成功！");

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
        var ms = OAuthConfig.GetValids(GrantTypes.AuthorizationCode);

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
            var user2 = ManageProvider.Provider.Register(username, password, r.ID, true);

            // 注册成功
        }
        catch (ArgumentException aex)
        {
            ModelState.AddModelError(aex.ParamName, aex.Message);
            return Json(500, aex.Message, null);//api版本发生异常时应及时返回错误信息
        }

        var model = GetViewModel(null);
        model.OAuthItems = OAuthConfig.GetVisibles();

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
}