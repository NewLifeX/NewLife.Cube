using NewLife.Cube.Entity;
using NewLife.Http;
using NewLife.Log;
using NewLife.Reflection;
using NewLife.Security;
using NewLife.Web;
using XCode;
using XCode.Membership;
using IManageUser = NewLife.Model.IManageUser;

namespace NewLife.Cube.Services.Sso;

/// <summary>用户绑定服务实现</summary>
public class UserBindingService : IUserBindingService
{
    #region 属性
    /// <summary>用户管理提供者</summary>
    public IManageProvider Provider { get; set; }

    /// <summary>性能追踪器</summary>
    public ITracer Tracer { get; set; }

    /// <summary>安全密钥。keyName$keyValue</summary>
    public String SecurityKey { get; set; }

    /// <summary>登录回调扩展点</summary>
    public IEnumerable<ILoginCallback> LoginCallbacks { get; set; }
    #endregion

    #region 构造
    /// <summary>实例化</summary>
    public UserBindingService()
    {
        Provider = ManageProvider.Provider;
    }
    #endregion

    #region 连接与绑定
    /// <summary>获取连接信息</summary>
    /// <param name="client"></param>
    /// <returns></returns>
    public virtual UserConnect GetConnect(OAuthClient client)
    {
        using var span = Tracer?.NewSpan("SsoProviderConnect", $"openid={client.OpenID} username={client.UserName}");

        var openid = client.OpenID;
        if (openid.IsNullOrEmpty()) openid = client.UserName;

        var uc = UserConnect.FindByProviderAndOpenID(client.Name, openid);
        uc ??= new UserConnect { Provider = client.Name, OpenID = openid };

        return uc;
    }

    /// <summary>绑定用户，用户未有效绑定或需要强制绑定时</summary>
    /// <param name="uc"></param>
    /// <param name="client"></param>
    /// <param name="userId"></param>
    public virtual IManageUser OnBind(UserConnect uc, OAuthClient client, Int32 userId)
    {
        using var span = Tracer?.NewSpan("SsoProviderBind", $"connectid={uc.ID} openid={uc.OpenID} username={uc.UserName} userId={userId}");

        var log = LogProvider.Provider;
        var prv = Provider;
        var mode = "";

        // 如果未登录，需要注册一个
        var user = prv.Current;
        if (user == null && userId > 0)
        {
            user = prv.FindByID(userId);
        }
        if (user == null)
        {
            // 匹配UnionId
            if (user == null && !client.UnionID.IsNullOrEmpty())
            {
                var list = UserConnect.FindAllByUnionId(client.UnionID);

                var ids = list.Where(e => e.Enable && e.UserID > 0).Select(e => e.UserID).Distinct().ToArray();
                var users = ids.Select(e => User.FindByID(e)).Where(e => e != null).ToList();
                if (users.Count > 0)
                {
                    mode = "UnionID";
                    user = users.OrderByDescending(e => e.Logins).FirstOrDefault();
                }
            }

            var set = CubeSetting.Current;
            var cfg = OAuthConfig.FindByName(client.Name);
            if (user == null && !set.AutoRegister && !cfg.AutoRegister)
            {
                log?.WriteLog(typeof(User), "SSO登录", false, $"无法找到[{client.Name}]的[{client.NickName}]在本地的绑定，且没有打开自动注册，准备进入登录页面，利用其它登录方式后再绑定", 0, user + "");

                return null;
            }

            // 先找用户名，如果存在，就加上提供者前缀，直接覆盖
            var name = client.UserName;
            if (user == null && !name.IsNullOrEmpty())
            {
                if (set.ForceBindUser)
                {
                    mode = "UserName";
                    user = prv.FindByName(name);

                    if (user != null && user.Name != name) user = null;
                }
                if (user == null)
                {
                    mode = "Provider-UserName";
                    user = prv.FindByName(client.Name + "_" + name);
                }
            }

            // 匹配Code
            if (user == null && set.ForceBindUserCode)
            {
                mode = "UserCode";
                if (!client.UserCode.IsNullOrEmpty()) user = User.FindByCode(client.UserCode);
            }

            // 匹配Mobile
            if (user == null && set.ForceBindUserMobile)
            {
                mode = "UserMobile";
                if (!client.Mobile.IsNullOrEmpty()) user = User.FindByMobile(client.Mobile);
            }

            // 匹配Mail
            if (user == null && set.ForceBindUserMail)
            {
                mode = "UserMail";
                if (!client.Mail.IsNullOrEmpty()) user = User.FindByMail(client.Mail);
            }

            // 匹配NickName
            if (user == null && set.ForceBindNickName)
            {
                mode = "NickName";
                if (!client.NickName.IsNullOrEmpty() && !client.NickName.EqualIgnoreCase("微信用户", "欢乐马"))
                    user = User.FindByName(client.NickName);
            }

            // 准备注册用的用户名
            if (user == null)
            {
                name = client.UserName;
                if (!name.IsNullOrEmpty() && User.FindByName(name) != null) name = null;

                if (name.IsNullOrEmpty())
                {
                    name = client.NickName;
                    if (!name.IsNullOrEmpty() && User.FindByName(name) != null) name = null;
                }
            }

            // QQ、微信等不返回用户名
            if (user == null && name.IsNullOrEmpty())
            {
                var openid = client.OpenID;
                if (openid.IsNullOrEmpty()) openid = client.AccessToken;

                var num = openid.GetBytes().Crc();

                mode = "OpenID-Crc";
                name = client.Name + "_" + num.ToString("X8");
                user = prv.FindByName(name);
            }

            if (user == null)
            {
                mode = "Register";

                var rid = Role.GetOrAdd(set.DefaultRole).ID;

                user = prv.Register(name, Rand.NextString(16), rid, true);
            }
        }

        uc.UserID = user.ID;
        uc.Enable = true;

        // 写日志
        log?.WriteLog(typeof(User), "绑定", true, $"[{user}]依据[{mode}]绑定到[{client.Name}]的[{client.NickName}]", user.ID, user + "");

        return user;
    }

    /// <summary>登录后绑定当前用户</summary>
    public virtual OAuthLog BindAfterLogin(Int64 oauthId)
    {
        var prv = Provider;
        var mode = nameof(BindAfterLogin);

        var user = prv.Current;
        if (user == null) return null;

        using var span = DefaultTracer.Instance?.NewSpan(nameof(BindAfterLogin), new { oauthId, user.Name, user.NickName });

        var log = OAuthLog.FindById(oauthId);
        if (log == null) return null;

        var uc = UserConnect.FindByID(log.ConnectId);
        if (uc == null) return null;

        uc.UserID = user.ID;
        uc.Enable = true;
        uc.UpdateTime = DateTime.Now;
        uc.Update();

        log.UserId = user.ID;
        log.SaveAsync();

        LogProvider.Provider?.WriteLog(typeof(User), "绑定", true, $"[{user}]依据[{mode}]绑定到[{uc.Provider}]的[{uc.NickName}]", user.ID, user + "");

        return log;
    }
    #endregion

    #region 填充用户信息
    /// <summary>填充用户信息，登录成功并获取用户信息之后</summary>
    /// <param name="client"></param>
    /// <param name="user"></param>
    /// <param name="context">服务提供者</param>
    public virtual void Fill(OAuthClient client, IManageUser user, IServiceProvider context = null)
    {
        client.Fill(user);

        var dic = client.Items;
        if (dic != null && user is User user2)
        {
            var set = CubeSetting.Current;

            if (user2.Code.IsNullOrEmpty() || set.ForceBindUserCode && !client.UserCode.IsNullOrEmpty()) user2.Code = client.UserCode;

            if (user2.Mobile.IsNullOrEmpty() || set.ForceBindUserMobile && !client.Mobile.IsNullOrEmpty()) user2.Mobile = client.Mobile;

            if (user2.Mail.IsNullOrEmpty() || set.ForceBindUserMail && !client.Mail.IsNullOrEmpty()) user2.Mail = client.Mail;

            if (client.Sex > 0) user2.Sex = (SexKinds)client.Sex;
            if (!client.Detail.IsNullOrEmpty()) user2.Remark = client.Detail;

            FillRoles(client, user2, set);
            FillDepartment(client, user2, set);
            FillTenant(client, user2, user);

            if (client.AreaId > 10_00_00)
                user2.AreaId = client.AreaId;
            else if (!client.AreaName.IsNullOrEmpty())
            {
                var ps = client.AreaName.Split('/');
                var r = Area.FindByNames(ps);
                if (r != null) user2.AreaId = r.ID;
            }

            if (client.Birthday.Year > 1000) user2.Birthday = client.Birthday;

            FillAvatar(client, user2, user, set);
        }

        // 调用登录回调扩展点
        if (LoginCallbacks != null)
        {
            foreach (var callback in LoginCallbacks)
            {
                callback.OnLoginAsync(client, null, user, context).ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }
    }

    /// <summary>填充头像信息</summary>
    protected virtual void FillAvatar(OAuthClient client, User user2, IManageUser user, CubeSetting set)
    {
        var av = client.GetAvatarUrl();
        if (av.IsNullOrEmpty()) return;

        var avatarUrl = av;
        var remoteHash = "";
        var idx = av.IndexOf('#');
        if (idx >= 0)
        {
            avatarUrl = av[..idx];
            remoteHash = av[(idx + 1)..];
        }

        if (!avatarUrl.StartsWithIgnoreCase("http://", "https://"))
            avatarUrl = client.GetUrl("Avatar", avatarUrl);

        var needUpdate = false;
        var localFile = set.AvatarPath.CombinePath(user2.ID + ".png").GetBasePath();

        if (!File.Exists(localFile))
        {
            needUpdate = true;
            LogProvider.Provider?.WriteLog(user.GetType(), "更新头像", true, $"本地文件不存在 {avatarUrl}", user.ID, user + "");
        }
        else if (!remoteHash.IsNullOrEmpty() && !localFile.AsFile().VerifyHash(remoteHash))
        {
            needUpdate = true;
            LogProvider.Provider?.WriteLog(user.GetType(), "更新头像", true, $"哈希不匹配 {avatarUrl}", user.ID, user + "");
        }
        else if (!user2.Avatar.IsNullOrEmpty() && user2.Avatar.StartsWithIgnoreCase("https://", "http://"))
        {
            needUpdate = true;
        }

        if (needUpdate)
        {
            if (client.Config != null && client.Config.FetchAvatar)
                Task.Factory.StartNew(() => FetchAvatar(user, av, client.AccessToken), TaskCreationOptions.LongRunning);
            else
                user2.Avatar = avatarUrl;
        }
    }

    /// <summary>填充角色信息</summary>
    protected virtual void FillRoles(OAuthClient client, User user, CubeSetting set)
    {
        var dic = client.Items;
        var roleId = 0;
        List<Int32> roleIds = null;

        if (set.UseSsoRole)
        {
            var sys = user.Roles.Where(e => e.IsSystem).Select(e => e.ID).ToList();
            if (sys.Count > 0)
            {
                roleIds ??= [];
                roleIds.AddRange(sys);
            }
            roleId = GetRole(dic, true);
            if (roleId > 0)
            {
                user.RoleID = roleId;

                var ids = GetRoles(client.Items, true).ToList();
                roleIds ??= [];
                roleIds.AddRange(ids);
            }
        }
        if (user.RoleID <= 0 && !set.DefaultRole.IsNullOrEmpty())
            user.RoleID = roleId = Role.GetOrAdd(set.DefaultRole).ID;

        var cfg = OAuthConfig.FindAllWithCache().FirstOrDefault(e => e.Name.EqualIgnoreCase(client.Name));
        if (cfg != null && !cfg.AutoRole.IsNullOrEmpty())
        {
            var ids = GetRoles(cfg.AutoRole, true).ToList();
            roleIds ??= [];
            roleIds.AddRange(ids);
        }

        if (roleIds != null)
        {
            roleIds = roleIds.Distinct().ToList();
            roleIds.Remove(roleId);
            if (roleIds.Count == 0)
                user.RoleIds = null;
            else
                user.RoleIds = "," + roleIds.OrderBy(e => e).Join() + ",";
        }

        if (!set.RoleRules.IsNullOrEmpty())
        {
            var currentRoleName = Role.FindByID(user.RoleID)?.Name;
            var departmentName = client.DepartmentName;
            foreach (var token in set.RoleRules.Split([',', '\uff0c'], StringSplitOptions.RemoveEmptyEntries))
            {
                var rule = token.Trim();
                if (rule.IsNullOrEmpty()) continue;

                var eqIdx = rule.IndexOf('=');
                if (eqIdx <= 0) continue;

                var left = rule[..eqIdx].Trim();
                var targetRole = rule[(eqIdx + 1)..].Trim();
                if (targetRole.IsNullOrEmpty()) continue;

                var plusIdx = left.IndexOf('+');
                var srcRole = plusIdx > 0 ? left[..plusIdx].Trim() : left.Trim();
                var deptPattern = plusIdx > 0 ? left[(plusIdx + 1)..].Trim() : null;

                if (!srcRole.EqualIgnoreCase(currentRoleName)) continue;
                if (!deptPattern.IsNullOrEmpty() && !MatchPattern(departmentName, deptPattern)) continue;

                user.RoleID = Role.GetOrAdd(targetRole).ID;
                break;
            }
        }
    }

    /// <summary>填充部门信息</summary>
    protected virtual void FillDepartment(OAuthClient client, User user, CubeSetting set)
    {
        if (!set.UseSsoDepartment || client.DepartmentCode.IsNullOrEmpty() || client.DepartmentName.IsNullOrEmpty()) return;

        var dep = Department.FindByCode(client.DepartmentCode);
        dep ??= new Department
        {
            Code = client.DepartmentCode,
            Name = client.DepartmentName,
            Enable = true,
            Visible = true,
        };

        if (!client.ParentDepartmentCode.IsNullOrEmpty())
        {
            var pdep = Department.FindByCode(client.ParentDepartmentCode);
            pdep ??= new Department
            {
                Code = client.ParentDepartmentCode,
                Name = client.ParentDepartmentName,
                Enable = true,
                Visible = true,
            };
            pdep.Save();

            dep.ParentID = pdep.ID;
        }
        else if (!client.ParentDepartmentName.IsNullOrEmpty())
        {
            var pdep = Department.FindByCode(client.ParentDepartmentName);
            pdep ??= Department.FindByNameAndParentID(client.ParentDepartmentName, 0);
            pdep ??= new Department
            {
                Code = client.ParentDepartmentName,
                Name = client.ParentDepartmentName,
                Enable = true,
                Visible = true,
            };
            pdep.Save();

            dep.ParentID = pdep.ID;
        }

        dep.Save();

        user.DepartmentID = dep.ID;
    }

    /// <summary>填充租户信息</summary>
    protected virtual void FillTenant(OAuthClient client, User user, IManageUser manageUser)
    {
        var log = LogProvider.Provider;

        if (!client.TenantCode.IsNullOrEmpty() && !client.TenantName.IsNullOrEmpty())
        {
            var tenant = Tenant.FindByCode(client.TenantCode);
            tenant ??= new XCode.Membership.Tenant
            {
                Code = client.TenantCode,
                Name = client.TenantName,
                Enable = true,
            };
            tenant.Save();

            var tenantUser = TenantUser.FindByTenantIdAndUserId(tenant.Id, user.ID);
            if (tenantUser == null)
            {
                tenantUser = new XCode.Membership.TenantUser
                {
                    TenantId = tenant.Id,
                    UserId = user.ID,
                    Enable = true,
                };
                tenantUser.Insert();

                log?.WriteLog(typeof(User), "SSO租户", true, $"[{manageUser}]自动创建租户关系[{tenant.Name}]", manageUser.ID, manageUser + "");
            }
            else if (!tenantUser.Enable)
            {
                tenantUser.Enable = true;
                tenantUser.Update();

                log?.WriteLog(typeof(User), "SSO租户", true, $"[{manageUser}]启用租户关系[{tenant.Name}]", manageUser.ID, manageUser + "");
            }
        }
        else if (client.TenantId > 0)
        {
            var tenant = Tenant.FindById(client.TenantId);
            if (tenant != null)
            {
                var tenantUser = TenantUser.FindByTenantIdAndUserId(tenant.Id, user.ID);
                if (tenantUser == null)
                {
                    tenantUser = new XCode.Membership.TenantUser
                    {
                        TenantId = tenant.Id,
                        UserId = user.ID,
                        Enable = true,
                    };
                    tenantUser.Insert();

                    log?.WriteLog(typeof(User), "SSO租户", true, $"[{manageUser}]自动创建租户关系[{tenant.Name}]", manageUser.ID, manageUser + "");
                }
                else if (!tenantUser.Enable)
                {
                    tenantUser.Enable = true;
                    tenantUser.Update();

                    log?.WriteLog(typeof(User), "SSO租户", true, $"[{manageUser}]启用租户关系[{tenant.Name}]", manageUser.ID, manageUser + "");
                }
            }
        }
    }
    #endregion

    #region 辅助
    /// <summary>注销</summary>
    public virtual void Logout() => Provider?.Logout();

    /// <summary>通配符模式匹配</summary>
    protected static Boolean MatchPattern(String value, String pattern)
    {
        if (pattern.IsNullOrEmpty() || pattern == "*") return true;
        if (value.IsNullOrEmpty()) return false;

        var starIdx = pattern.IndexOf('*');
        if (starIdx < 0) return value.EqualIgnoreCase(pattern);

        if (starIdx == pattern.Length - 1)
            return value.StartsWith(pattern[..starIdx], StringComparison.OrdinalIgnoreCase);

        if (starIdx == 0)
            return value.EndsWith(pattern[1..], StringComparison.OrdinalIgnoreCase);

        var prefix = pattern[..starIdx];
        var suffix = pattern[(starIdx + 1)..];
        return value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            && value.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)
            && value.Length >= prefix.Length + suffix.Length;
    }

    /// <summary>抓取远程头像</summary>
    /// <param name="user">用户对象</param>
    /// <param name="url">头像URL</param>
    /// <param name="accessToken">访问令牌</param>
    /// <returns></returns>
    public virtual async Task<Boolean> FetchAvatar(IManageUser user, String url = null, String accessToken = null)
    {
        using var span = Tracer?.NewSpan(nameof(FetchAvatar), new { user.ID, user.Name, user.NickName, url });

        if (url.IsNullOrEmpty()) url = user.GetValue("Avatar") as String;

        if (url.IsNullOrEmpty() || !url.StartsWithIgnoreCase("http://", "https://"))
        {
            var list = UserConnect.FindAllByUserID(user.ID);
            url = list.OrderByDescending(e => e.UpdateTime)
                .Where(e => !e.Avatar.IsNullOrEmpty() && e.Avatar.StartsWithIgnoreCase("http://", "https://"))
                .FirstOrDefault()?.Avatar;
        }

        if (url.IsNullOrEmpty()) return false;
        if (!url.StartsWithIgnoreCase("http://", "https://")) return false;

        var downloadUrl = url;
        var expectedHash = "";
        var idx = url.IndexOf('#');
        if (idx >= 0)
        {
            downloadUrl = url[..idx];
            expectedHash = url[(idx + 1)..];
        }

        var set = CubeSetting.Current;
        var (existPath, _) = global::NewLife.Cube.Services.SvgAvatarService.FindAvatarFile(set.AvatarPath, user.ID);
        var dest = existPath ?? set.AvatarPath.CombinePath(user.ID + ".png").GetBasePath();

        try
        {
            var fi = dest.AsFile();
            if (!fi.Exists || !fi.VerifyHash(expectedHash))
            {
                dest = set.AvatarPath.CombinePath(user.ID + ".png").GetBasePath();

                LogProvider.Provider?.WriteLog(user.GetType(), "抓取头像", true, $"{downloadUrl} => {dest} (hash={expectedHash})", user.ID, user + "");

                var client = new HttpClient();

                if (!accessToken.IsNullOrEmpty())
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

                await client.DownloadFileAsync(downloadUrl, dest, expectedHash, default);

                var fi2 = dest.AsFile();
                if (fi2.Exists && fi2.Length > 0)
                {
                    var firstByte = new Byte[1];
                    using (var fs = fi2.OpenRead())
                        fs.Read(firstByte, 0, 1);

                    if (firstByte[0] == (Byte)'<')
                    {
                        var svgDest = set.AvatarPath.CombinePath(user.ID + ".svg").GetBasePath();
                        if (System.IO.File.Exists(svgDest))
                            System.IO.File.Delete(svgDest);
                        System.IO.File.Move(dest, svgDest);
                        dest = svgDest;
                    }
                }
            }

            user.SetValue("Avatar", "/Sso/Avatar?id=" + user.ID);
            (user as IEntity)?.Update();

            return true;
        }
        catch (Exception ex)
        {
            span?.SetError(ex, null);

            XTrace.WriteLine("抓取头像失败，{0}, {1}", user, downloadUrl);
            XTrace.WriteException(ex);
        }

        return false;
    }

    /// <summary>获取指定Key</summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public virtual String GetKey(String name)
    {
        var key = SecurityKey;
        if (key.IsNullOrEmpty())
        {
            if (name.IsNullOrEmpty()) name = "SsoSecurity";

            var prv = Parameter.GetOrAdd(0, "Keys", $"{name}.prvkey");
            var pub = Parameter.GetOrAdd(0, "Keys", $"{name}.pubkey");

            try
            {
                var file = $"..\\Keys\\{name}.prvkey".GetFullPath();
                if (File.Exists(file))
                {
                    if (prv.LongValue.IsNullOrEmpty()) prv.LongValue = File.ReadAllText(file);

                    File.Delete(file);
                }
                file = $"..\\Keys\\{name}.pubkey".GetFullPath();
                if (File.Exists(file))
                {
                    if (pub.LongValue.IsNullOrEmpty()) pub.LongValue = File.ReadAllText(file);

                    File.Delete(file);
                }

                var di = file.AsFile().Directory;
                if (di.Exists && !di.GetAllFiles().Any()) di.Delete(true);
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);
            }

            if (prv.LongValue.IsNullOrEmpty())
            {
                var ks = RSAHelper.GenerateKey();
                prv.LongValue = ks[0];
                pub.LongValue = ks[1];
            }

            key = prv.LongValue;

            prv.Update();
            pub.Update();
        }
        if (key.IsNullOrEmpty()) throw new ArgumentNullException(nameof(SecurityKey), $"无法找到名为[{name}]的密钥");

        return key;
    }

    private Int32 GetRole(IDictionary<String, String> dic, Boolean create)
    {
        if (dic.TryGetValue("RoleName", out var name) && !name.IsNullOrEmpty())
        {
            var r = Role.FindByName(name);
            if (r != null) return r.ID;

            if (create)
            {
                r = new Role { Name = name };
                r.Insert();
                return r.ID;
            }
        }

        return 0;
    }

    private Int32[] GetRoles(IDictionary<String, String> dic, Boolean create)
    {
        if (dic.TryGetValue("RoleNames", out var roleNames)) return GetRoles(roleNames, create);

        return new Int32[0];
    }

    private Int32[] GetRoles(String roleNames, Boolean create)
    {
        var names = roleNames.Split(',');
        var rs = new List<Int32>();
        foreach (var item in names)
        {
            if (item.IsNullOrEmpty()) continue;

            var r = Role.FindByName(item);
            if (r != null)
                rs.Add(r.ID);
            else if (create)
            {
                r = new Role { Name = item };
                r.Insert();
                rs.Add(r.ID);
            }
        }

        if (rs.Count > 0) return rs.Distinct().ToArray();

        return new Int32[0];
    }
    #endregion
}
