using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.Entity;
using NewLife.Web;
using XCode;
using XCode.Membership;
using System.Web;
using NewLife.Caching;
using NewLife.Log;
using NewLife.Cube.Areas.Admin.Models;
using NewLife.Common;
using NewLife.Reflection;
using System.IO;
#if __CORE__
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using NewLife.Cube.Extensions;
#else
using System.Web.Mvc;
using System.Web.Security;
#endif
using static XCode.Membership.User;

namespace NewLife.Cube.Admin.Controllers
{
    /// <summary>用户控制器</summary>
    [DataPermission(null, "ID={#userId}")]
    [DisplayName("用户")]
    [Description("系统基于角色授权，每个角色对不同的功能模块具备添删改查以及自定义权限等多种权限设定。")]
    [Area("Admin")]
    public class UserController : EntityController<User>
    {
        /// <summary>用于防爆破登录。即使内存缓存，也有一定用处，最糟糕就是每分钟重试次数等于集群节点数的倍数</summary>
        private static ICache _cache = Cache.Default ?? new MemoryCache();

        static UserController()
        {
            MenuOrder = 100;

            ListFields.RemoveField("Avatar", "RoleIds", "Online", "RegisterIP", "RegisterTime");
            ListFields.RemoveField("Phone", "Code", "Question", "Answer");
            ListFields.RemoveField("Ex1", "Ex2", "Ex3", "Ex4", "Ex5", "Ex6");
            ListFields.RemoveUpdateField();
            ListFields.RemoveField("Remark");

            {
                var df = ListFields.AddListField("Link", "Logins");
                df.Header = "链接";
                df.HeaderTitle = "第三方登录的链接信息";
                df.DisplayName = "链接";
                df.Title = "第三方登录的链接信息";
                df.Url = "UserConnect?userId={ID}";
            }

            {
                var df = ListFields.AddListField("Token", "Logins");
                df.Header = "令牌";
                df.DisplayName = "令牌";
                df.Url = "UserToken?userId={ID}";
            }

            {
                var df = ListFields.AddListField("Log", "Logins");
                df.Header = "日志";
                df.DisplayName = "日志";
                df.Url = "Log?userId={ID}";
            }

            {
                var df = ListFields.AddListField("OAuthLog", "Logins");
                df.Header = "OAuth日志";
                df.DisplayName = "OAuth日志";
                df.Url = "OAuthLog?userId={ID}";
            }

            {
                var df = AddFormFields.AddDataField("RoleIds", "RoleNames");
                df.DataSource = (entity, field) => Role.FindAllWithCache().ToDictionary(e => e.ID, e => e.Name);
                AddFormFields.RemoveField("RoleNames");
            }

            {
                var df = EditFormFields.AddDataField("RoleIds", "RoleNames");
                df.DataSource = (entity, field) => Role.FindAllWithCache().ToDictionary(e => e.ID, e => e.Name);
                EditFormFields.RemoveField("RoleNames");
            }
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

            var roleId = p["roleId"].ToInt(-1);
            var roleIds = p["roleIds"]?.Split(",");
            var departmentId = p["departmentId"].ToInt(-1);
            var departmentIds = p["departmentIds"]?.Split(",");
            var enable = p["enable"]?.ToBoolean();
            var start = p["dtStart"].ToDateTime();
            var end = p["dtEnd"].ToDateTime();
            var key = p["q"];

            //p.RetrieveState = true;

            //return XCode.Membership.User.Search(roleId, departmentId, enable, start, end, key, p);

            var exp = new WhereExpression();
            if (roleId >= 0) exp &= _.RoleID == roleId | _.RoleIds.Contains("," + roleId + ",");
            if (roleIds != null && roleIds.Length > 0) exp &= _.RoleID.In(roleIds) | _.RoleIds.Contains("," + roleIds.Join(",") + ",");
            if (departmentId >= 0) exp &= _.DepartmentID == departmentId;
            if (departmentIds != null && departmentIds.Length > 0) exp &= _.DepartmentID.In(departmentIds);
            if (enable != null) exp &= _.Enable == enable.Value;
            exp &= _.LastLogin.Between(start, end);
            if (!key.IsNullOrEmpty()) exp &= _.Code.StartsWith(key) | _.Name.StartsWith(key) | _.DisplayName.StartsWith(key) | _.Mobile.StartsWith(key) | _.Mail.StartsWith(key);

            var list2 = XCode.Membership.User.FindAll(exp, p);

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
            if (!post && type == DataObjectMethodType.Update)
            {
                // 清空密码，不向浏览器输出
                //entity.Password = null;
                entity["Password"] = null;
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
            if (ms != null && !Setting.Current.AllowLogin)
            {
                if (ms.Count == 0) throw new Exception("禁用了本地密码登录，且没有配置第三方登录");
                if (logId > 0) throw new Exception("已完成第三方登录，但无法绑定本地用户且没有开启自动注册，建议开启OAuth应用的自动注册");

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
#if __CORE__
                var agent = Request.Headers["User-Agent"] + "";
#else
                var agent = Request.UserAgent;
#endif
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

            return View(model);
        }

        private LoginViewModel GetViewModel(String returnUrl)
        {
            var set = Setting.Current;
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

            // 连续错误校验
            var key = $"Login:{username}";
            var errors = _cache.Get<Int32>(key);
            var ipKey = $"Login:{UserHost}";
            var ipErrors = _cache.Get<Int32>(ipKey);

            var returnUrl = GetRequest("r");
            try
            {
                if (username.IsNullOrEmpty()) throw new ArgumentNullException(nameof(username), "用户名不能为空！");
                if (password.IsNullOrEmpty()) throw new ArgumentNullException(nameof(password), "密码不能为空！");

                if (errors >= 5) throw new InvalidOperationException($"[{username}]登录错误过多，请在60秒后再试！");
                if (ipErrors >= 5) throw new InvalidOperationException($"IP地址[{UserHost}]登录错误过多，请在60秒后再试！");

                var provider = ManageProvider.Provider;
                if (ModelState.IsValid && provider.Login(username, password, remember) != null)
                {
                    // 登录成功，清空错误数
                    if (errors > 0) _cache.Remove(key);
                    if (ipErrors > 0) _cache.Remove(ipKey);

                    if (IsJsonRequest)
                    {
                        var token = HttpContext.Items["jwtToken"];
                        return Json(0, "ok", new { /*provider.Current.ID,*/ Token = token });
                    }

                    //FormsAuthentication.SetAuthCookie(username, remember ?? false);

                    if (Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);

                    // 不要嵌入自己
                    if (returnUrl.EndsWithIgnoreCase("/Admin", "/Admin/User/Login")) returnUrl = null;

                    // 登录后自动绑定
                    var logId = Session["Cube_OAuthId"].ToLong();
                    if (logId > 0)
                    {
                        Session["Cube_OAuthId"] = null;
                        var log = Cube.Controllers.SsoController.Provider.BindAfterLogin(logId);
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
                XTrace.WriteLine("[{0}]登录失败！{1}", username, ex.Message);
                XTrace.WriteException(ex);

                // 累加错误数，首次出错时设置过期时间
                _cache.Increment(key, 1);
                _cache.Increment(ipKey, 1);
                if (errors <= 0) _cache.SetExpire(key, TimeSpan.FromSeconds(60));
                if (ipErrors <= 0) _cache.SetExpire(ipKey, TimeSpan.FromSeconds(60));

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

            return View(model);
        }

        /// <summary>注销</summary>
        /// <returns></returns>
        [AllowAnonymous]
        public ActionResult Logout()
        {
            var returnUrl = GetRequest("r");

            var set = Setting.Current;
            if (set.LogoutAll)
            {
                // 如果是单点登录，则走单点登录注销
                var name = Session["Cube_Sso"] as String;
                if (!name.IsNullOrEmpty()) return Redirect($"~/Sso/Logout?name={name}&r={HttpUtility.UrlEncode(returnUrl)}");
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
            if (ViewBag.Fields == null) ViewBag.Fields = EditFormFields;
            ViewBag.Factory = XCode.Membership.User.Meta.Factory;

            // 必须指定视图名，因为其它action会调用
            return View("Info", user);
        }

        /// <summary>更新用户资料</summary>
        /// <param name="user"></param>
        /// <returns></returns>
        [HttpPost]
        //[AllowAnonymous]
        [EntityAuthorize]
        public ActionResult Info(User user)
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
                var set = Setting.Current;
                var fileName = user.ID + Path.GetExtension(file.FileName);
                user.Avatar = SaveFile(user, file, set.AvatarPath, null, fileName);
            }

            user.Update();

            return Info(user.ID);
        }

        /// <summary>保存文件</summary>
        /// <param name="entity">实体对象</param>
        /// <param name="file">文件</param>
        /// <param name="uploadPath">上传目录，默认使用UploadPath配置</param>
        /// <param name="datePath">日期目录，可以在中间增加应用和日期的子目录</param>
        /// <param name="fileName">文件名，如若指定则忽略前面的目录</param>
        /// <returns></returns>
        protected override String SaveFile(User entity, IFormFile file, String uploadPath, String datePath, String fileName)
        {
            // 修改保存目录和文件名
            var set = Setting.Current;
            if (file.Name.EqualIgnoreCase("avatar")) fileName = entity.ID + Path.GetExtension(file.FileName);

            return base.SaveFile(entity, file, set.AvatarPath, null, fileName);
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

            return View(model);
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

            var set = Setting.Current;
            if (model.NewPassword.Length < set.MinPasswordLength) throw new ArgumentException($"最短密码要求{set.MinPasswordLength}位", nameof(model.NewPassword));

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

            var set = Setting.Current;
            if (!set.AllowRegister) throw new Exception("禁止注册！");

            try
            {
                //if (String.IsNullOrEmpty(email)) throw new ArgumentNullException("email", "邮箱地址不能为空！");
                if (String.IsNullOrEmpty(username)) throw new ArgumentNullException("username", "用户名不能为空！");
                if (String.IsNullOrEmpty(password)) throw new ArgumentNullException("password", "密码不能为空！");
                if (String.IsNullOrEmpty(password2)) throw new ArgumentNullException("password2", "重复密码不能为空！");
                if (password != password2) throw new ArgumentOutOfRangeException("password2", "两次密码必须一致！");

                if (password.Length < set.MinPasswordLength) throw new ArgumentException($"最短密码要求{set.MinPasswordLength}位", nameof(password));

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
            if (!ManageProvider.User.Role.IsSystem) throw new Exception("清除密码操作需要管理员权限，非法操作！");

            // 前面表单可能已经清空密码
            var user = FindByID(id);
            //user.Password = "nopass";
            user.Password = null;
            user.SaveWithoutValid();

            if (IsJsonRequest) return Ok();

            return RedirectToAction("Edit", new { id });
        }

        /// <summary>批量启用</summary>
        /// <param name="keys"></param>
        /// <returns></returns>
        [EntityAuthorize(PermissionFlags.Update)]
        public ActionResult EnableSelect(String keys) => EnableOrDisableSelect();

        /// <summary>批量禁用</summary>
        /// <param name="keys"></param>
        /// <returns></returns>
        [EntityAuthorize(PermissionFlags.Update)]
        public ActionResult DisableSelect(String keys) => EnableOrDisableSelect(false);

        private ActionResult EnableOrDisableSelect(Boolean isEnable = true)
        {
            var count = 0;
            var ids = GetRequest("keys").SplitAsInt();
            if (ids.Length > 0)
            {
                foreach (var id in ids)
                {
                    var user = FindByID(id);
                    if (user != null && user.Enable != isEnable)
                    {
                        user.Enable = isEnable;
                        user.Update();

                        Interlocked.Increment(ref count);
                    }
                }
            }

            return JsonRefresh($"共{(isEnable ? "启用" : "禁用")}[{count}]个用户");
        }
    }
}