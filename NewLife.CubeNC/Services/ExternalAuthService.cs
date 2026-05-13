using System.Net.Http;
using System.Text;
using System.Text.Json;
using NewLife.Log;
using XCode.Membership;

namespace NewLife.Cube.Services;

/// <summary>外部认证返回的用户信息</summary>
public class ExternalAuthModel
{
    /// <summary>用户名</summary>
    public String UserName { get; set; }

    /// <summary>显示名称/昵称</summary>
    public String DisplayName { get; set; }

    /// <summary>邮件</summary>
    public String Mail { get; set; }

    /// <summary>手机</summary>
    public String Mobile { get; set; }

    /// <summary>头像</summary>
    public String Avatar { get; set; }

    /// <summary>角色名</summary>
    public String RoleName { get; set; }

    /// <summary>代码。身份证、员工编号等</summary>
    public String Code { get; set; }
}

/// <summary>外部验证助手。调用外部接口验证用户名密码，支持与第三方认证体系集成</summary>
/// <remarks>
/// 外部验证接口协议：
/// 请求：POST {ExternalAuthUrl}，Content-Type: application/json，Body: {"username":"...","password":"..."}
/// 响应：{"code":0,"data":{"username":"...","displayName":"...","mail":"...","mobile":"...","roleName":"...","avatar":"...","code":"..."}}
/// 其中 code=0 表示验证成功，data 包含用户信息；
/// 也支持 {"success":true,"data":{...}} 格式。
/// </remarks>
public static class ExternalAuthHelper
{
    private static readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(10) };

    /// <summary>调用外部验证接口，验证用户名密码。成功返回用户信息，失败或未配置返回null</summary>
    /// <param name="username">用户名</param>
    /// <param name="password">密码</param>
    /// <param name="url">外部验证地址</param>
    /// <returns>外部用户信息，验证失败时返回null</returns>
    public static ExternalAuthModel Validate(String username, String password, String url)
    {
        if (url.IsNullOrEmpty()) return null;

        using var span = DefaultTracer.Instance?.NewSpan("ExternalAuth", username);
        try
        {
            var body = JsonSerializer.Serialize(new { username, password });
            var content = new StringContent(body, Encoding.UTF8, "application/json");

            // ASP.NET Core 无 SynchronizationContext，GetAwaiter().GetResult() 安全
            var response = _httpClient.PostAsync(url, content).GetAwaiter().GetResult();
            var json = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            if (json.IsNullOrEmpty()) return null;

            return ParseResponse(json, username);
        }
        catch (Exception ex)
        {
            span?.SetError(ex, new { username, url });
            XTrace.WriteException(ex);
            return null;
        }
    }

    /// <summary>根据外部验证结果创建或更新本地用户</summary>
    /// <param name="extUser">外部用户信息</param>
    /// <param name="ip">客户端IP</param>
    /// <param name="set">魔方配置</param>
    /// <returns>本地用户实体</returns>
    public static User CreateOrUpdateUser(ExternalAuthModel extUser, String ip, CubeSetting set)
    {
        // 先按用户名查找，再按手机/邮箱查找
        var user = User.FindByName(extUser.UserName);
        if (user == null && !extUser.Mobile.IsNullOrEmpty())
            user = User.FindByMobile(extUser.Mobile);
        if (user == null && !extUser.Mail.IsNullOrEmpty())
            user = User.FindByMail(extUser.Mail);

        if (user == null)
        {
            // 自动注册新用户
            user = new User
            {
                Name = extUser.UserName,
                DisplayName = extUser.DisplayName,
                Mail = extUser.Mail,
                Mobile = extUser.Mobile,
                Avatar = extUser.Avatar,
                Code = extUser.Code,
                Enable = true,
                RegisterIP = ip,
                RegisterTime = DateTime.Now,
            };

            // 设置角色：优先使用外部返回的角色名，否则使用默认角色
            if (!extUser.RoleName.IsNullOrEmpty())
            {
                var role = Role.FindByName(extUser.RoleName);
                if (role != null) user.RoleID = role.ID;
            }
            if (user.RoleID <= 0 && !set.DefaultRole.IsNullOrEmpty())
            {
                var role = Role.FindByName(set.DefaultRole);
                if (role != null) user.RoleID = role.ID;
            }

            user.Insert();
            LogProvider.Provider?.WriteLog(typeof(User), "外部验证注册", true, $"用户：{extUser.UserName}", user.ID, user + "", ip);
        }
        else
        {
            // 更新已有用户信息（仅更新有值的字段）
            var dirty = false;
            if (!extUser.DisplayName.IsNullOrEmpty() && user.DisplayName != extUser.DisplayName)
            {
                user.DisplayName = extUser.DisplayName;
                dirty = true;
            }
            if (!extUser.Mail.IsNullOrEmpty() && user.Mail != extUser.Mail)
            {
                user.Mail = extUser.Mail;
                dirty = true;
            }
            if (!extUser.Mobile.IsNullOrEmpty() && user.Mobile != extUser.Mobile)
            {
                user.Mobile = extUser.Mobile;
                dirty = true;
            }
            if (!extUser.Avatar.IsNullOrEmpty() && user.Avatar != extUser.Avatar)
            {
                user.Avatar = extUser.Avatar;
                dirty = true;
            }
            if (!extUser.Code.IsNullOrEmpty() && user.Code != extUser.Code)
            {
                user.Code = extUser.Code;
                dirty = true;
            }
            if (dirty) user.Update();
        }

        if (!user.Enable) throw new InvalidOperationException($"用户[{user.Name}]已禁用");

        return user;
    }

    private static ExternalAuthModel ParseResponse(String json, String username)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // 检查响应是否成功：code=0 或 success=true
            if (root.TryGetProperty("code", out var codeProp))
            {
                // 严格解析 code：仅当 code 为整数 0 时视为成功，其他均失败
                Int32? codeVal = null;
                if (codeProp.ValueKind == JsonValueKind.Number)
                    codeVal = codeProp.GetInt32();
                else if (codeProp.ValueKind == JsonValueKind.String &&
                         Int32.TryParse(codeProp.GetString(), out var parsed))
                    codeVal = parsed;

                if (codeVal != 0) return null;
            }
            else if (root.TryGetProperty("success", out var successProp))
            {
                var ok = successProp.ValueKind switch
                {
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.String => successProp.GetString().ToBoolean(),
                    _ => false,
                };
                if (!ok) return null;
            }

            // 获取 data 子对象；若无则使用根对象
            var data = root.TryGetProperty("data", out var dataProp) && dataProp.ValueKind == JsonValueKind.Object
                ? dataProp
                : root;

            // username 字段缺失时回退使用登录时输入的用户名
            return new ExternalAuthModel
            {
                UserName = GetStr(data, "username", "userName", "name") ?? username,
                DisplayName = GetStr(data, "displayName", "nickName", "nickname"),
                Mail = GetStr(data, "mail", "email"),
                Mobile = GetStr(data, "mobile", "phone"),
                Avatar = GetStr(data, "avatar"),
                RoleName = GetStr(data, "roleName", "role"),
                Code = GetStr(data, "code"),
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>大小写不敏感地从 JsonElement 读取字符串属性值，依次尝试多个候选 key</summary>
    private static String GetStr(JsonElement element, params String[] keys)
    {
        foreach (var key in keys)
        {
            foreach (var prop in element.EnumerateObject())
            {
                if (prop.Name.EqualIgnoreCase(key) && prop.Value.ValueKind == JsonValueKind.String)
                {
                    var val = prop.Value.GetString();
                    if (!val.IsNullOrEmpty()) return val;
                }
            }
        }
        return null;
    }
}
