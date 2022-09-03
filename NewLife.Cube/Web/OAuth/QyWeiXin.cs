using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using NewLife.Caching;
using NewLife.Cube.Web.Models;
using NewLife.Remoting;
using NewLife.Http;
using NewLife.Serialization;
using XCode.Membership;

namespace NewLife.Web.OAuth
{
    /// <summary>企业微信身份验证提供者</summary>
    public class QyWeiXin : OAuthClient
    {
        #region 属性
        /// <summary>企业Ids</summary>
        public String CorpId { get => Key; set => Key = value; }

        /// <summary>应用凭证</summary>
        public String CorpSecret { get => Secret; set => Secret = value; }

        /// <summary>应用Id</summary>
        public String AgentId { get; set; }

        private readonly String _qyapi = "https://qyapi.weixin.qq.com/cgi-bin/";
        private ICache _cache;
        #endregion

        #region 构造
        /// <summary>实例化</summary>
        public QyWeiXin()
        {
            Server = "https://open.weixin.qq.com/connect/oauth2/";

            AuthUrl = "authorize?response_type={response_type}&appid={key}&redirect_uri={redirect}&state={state}&scope={scope}#wechat_redirect";

            AccessUrl = _qyapi + "gettoken?corpid={key}&corpsecret={secret}";
            UserUrl = _qyapi + "user/getuserinfo?access_token={token}&code={code}";

            Scope = "snsapi_base";

            _cache = Cache.Default ?? new MemoryCache();
        }

        /// <summary>是否支持指定用户端，也就是判断是否在特定应用内打开，例如QQ/DingDing/WeiXin</summary>
        /// <param name="userAgent"></param>
        /// <returns></returns>
        public override Boolean Support(String userAgent) => !userAgent.IsNullOrEmpty() && userAgent.Contains(" wxwork/");

        /// <summary>针对指定客户端进行初始化</summary>
        /// <param name="userAgent"></param>
        public override void Init(String userAgent)
        {
            var key = CorpId;
            if (!key.IsNullOrEmpty())
            {
                var p = key.IndexOf('#');
                if (p > 0)
                {
                    CorpId = key[..p];
                    AgentId = key[(p + 1)..];
                }
            }

            // 钉钉内打开时，自动切换为应用内免登
            if (Support(userAgent))
            {
                Scope = "snsapi_base";
            }
            else
            {
                Scope = null;
                AuthUrl = "https://open.work.weixin.qq.com/wwopen/sso/qrConnect?appid={key}&agentid={agentid}&redirect_uri={redirect}&state={state}".Replace("{agentid}", AgentId);
            }
        }
        #endregion

        #region 令牌
        /// <summary>获取令牌</summary>
        /// <returns></returns>
        public async Task<String> GetToken()
        {
            if (CorpId.IsNullOrEmpty()) throw new ArgumentNullException(nameof(CorpId));
            if (CorpSecret.IsNullOrEmpty()) throw new ArgumentNullException(nameof(CorpSecret));

            //var url = $"https://qyapi.weixin.qq.com/cgi-bin/gettoken?corpid={CorpId}&corpsecret={CorpSecret}";

            var rs = await GetAsync<IDictionary<String, Object>>("gettoken", new { corpid = CorpId, corpsecret = CorpSecret });

            AccessToken = rs["access_token"] as String;
            var exp = rs["expires_in"].ToInt(-1);
            if (exp > 0) Expire = DateTime.Now.AddSeconds(exp);

            return AccessToken;
        }

        /// <summary>获取访问令牌</summary>
        /// <returns></returns>
        protected async Task<String> GetAccessToken()
        {
            if (!String.IsNullOrEmpty(AccessToken) && Expire > DateTime.Now.AddMinutes(1)) return AccessToken;

            return await GetToken();
        }
        #endregion

        #region 通信录
        /// <summary>获取部门列表</summary>
        /// <returns></returns>
        public async Task<DepartmentInfo[]> GetDepartments()
        {
            var token = await GetAccessToken();

            var rs = await GetAsync<DepartmentInfo[]>("department/list", new { access_token = token }, "department");

            return rs;
        }

        /// <summary>获取用户</summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<UserInfo> GetUser(String userId)
        {
            var token = await GetAccessToken();

            var rs = await GetAsync<UserInfo>("user/get", new
            {
                userid = userId,
                access_token = token
            }, "");

            return rs;
        }

        /// <summary>获取部门内用户列表</summary>
        /// <param name="departmentId"></param>
        /// <param name="fetchChild"></param>
        /// <returns></returns>
        public async Task<UserInfo[]> GetUsers(Int32 departmentId, Boolean fetchChild = false)
        {
            var token = await GetAccessToken();

            var rs = await GetAsync<UserInfo[]>("user/list", new
            {
                department_id = departmentId,
                fetch_child = fetchChild ? 1 : 0,
                access_token = token
            }, "userlist");

            return rs;
        }
        #endregion

        #region 考勤
        /// <summary>获取考勤数据</summary>
        /// <param name="userIds"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public async Task<CheckInData[]> GetCheckIn(String[] userIds, DateTime start, DateTime end)
        {
            var token = await GetAccessToken();

            var rs = await PostAsync<CheckInData[]>("checkin/getcheckindata?access_token=" + token, new
            {
                opencheckindatatype = 3,
                starttime = start.ToInt(),
                endtime = end.ToInt(),
                useridlist = userIds,
            }, "checkindata");

            return rs;
        }
        #endregion

        #region 审批
        /// <summary>获取审批数据</summary>
        /// <param name="templateId"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="cursor"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public async Task<String[]> GetApprovals(String templateId, DateTime start, DateTime end, Int32 cursor = 0, Int32 size = 100)
        {
            var token = await GetAccessToken();

            var rs = await PostAsync<String[]>("oa/getapprovalinfo?access_token=" + token, new
            {
                starttime = start.ToInt(),
                endtime = end.ToInt(),
                cursor,
                size,
                filters = new[] {
                    new { key = "template_id", value = templateId },
                    new { key = "sp_status", value = "2" },
                },
            }, "sp_no_list");

            return rs;
        }

        /// <summary>获取审批单详情</summary>
        /// <param name="sp_no"></param>
        /// <returns></returns>
        public async Task<ApprovalInfo> GetApproval(String sp_no)
        {
            var token = await GetAccessToken();

            var rs = await PostAsync<ApprovalInfo>("oa/getapprovaldetail?access_token=" + token, new
            {
                sp_no
            }, "info");

            return rs;
        }
        #endregion

        #region 辅助
        /// <summary>获取配置</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static String GetConfig(String name) => Parameter.GetOrAdd(0, "QyWeiXin", name)?.Value;

        /// <summary>依据魔方字典参数的配置，创建企业微信对象</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static QyWeiXin CreateFor(String name)
        {
            return new QyWeiXin
            {
                CorpId = QyWeiXin.GetConfig("CorpId"),
                CorpSecret = QyWeiXin.GetConfig(name),
            };
        }

        private HttpClient _Client;
        /// <summary>获取客户端</summary>
        /// <returns></returns>
        protected virtual HttpClient GetQyClient()
        {
            if (_Client != null) return _Client;

            var client = CreateClient();
            client.BaseAddress = new Uri(_qyapi);

            return _Client = client;
        }

        /// <summary>异步请求接口</summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="action"></param>
        /// <param name="args"></param>
        /// <param name="dataName"></param>
        /// <returns></returns>
        protected virtual async Task<TResult> GetAsync<TResult>(String action, Object args, String dataName = "data")
        {
            var buf = await GetQyClient().GetAsync<Byte[]>(action, args);
            if (buf == null || buf.Length == 0) return default;

            var str = buf.ToStr()?.Trim();

            if (Log != null && Log.Enable)
            {
                WriteLog("GET {0} {1}", action, str);

                // 合并字典
                var dic = GetNameValues(str);
                if (Items == null)
                    Items = dic;
                else
                {
                    foreach (var item in dic)
                    {
                        Items[item.Key] = item.Value;
                    }
                }
            }

            return ApiHelper.ProcessResponse<TResult>(str, null, dataName);
        }

        /// <summary>异步请求接口</summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="action"></param>
        /// <param name="args"></param>
        /// <param name="dataName"></param>
        /// <returns></returns>
        protected virtual async Task<TResult> PostAsync<TResult>(String action, Object args, String dataName = "data")
        {
            var buf = await GetQyClient().PostAsync<Byte[]>(action, args);
            if (buf == null || buf.Length == 0) return default;

            var str = buf.ToStr()?.Trim();

            if (Log != null && Log.Enable)
            {
                WriteLog("POST {0} {1}", action, str);

                // 合并字典
                var dic = GetNameValues(str);
                if (Items == null)
                    Items = dic;
                else
                {
                    foreach (var item in dic)
                    {
                        Items[item.Key] = item.Value;
                    }
                }
            }

            return ApiHelper.ProcessResponse<TResult>(str, null, dataName);
        }
        #endregion

        #region 用户信息增强
        /// <summary>企业内部应用获取用户信息</summary>
        /// <returns></returns>
        public override String GetUserInfo()
        {
            var html = base.GetUserInfo();

            // 获取用户详情
            if (!UserName.IsNullOrEmpty())
            {
                var user = Task.Run(() => GetUser(UserName)).Result;
                if (user != null)
                {
                    NickName = user.Name;
                    Mobile = user.Mobile;
                    Mail = user.Mail;
                    Avatar = user.Avatar;
                    Detail = user.Alias;
                    DepartmentCode = user.MainDepartment;
                }

                // 根据部门编码，填充部门名称
                var code = DepartmentCode.ToInt();
                //if (!DepartmentCode.IsNullOrEmpty() && DepartmentName.IsNullOrEmpty())
                if (code > 0 && DepartmentName.IsNullOrEmpty())
                {
                    var key = $"sso:dps:{CorpId}";
                    var dps = _cache.Get<DepartmentInfo[]>(key);
                    if (dps == null || dps.Length == 0)
                    {
                        dps = Task.Run(() => GetDepartments()).Result;

                        _cache.Set(key, dps, 3600);
                    }

                    var dp = dps?.FirstOrDefault(e => e.Id == code);
                    if (dp != null) DepartmentName = dp.Name;
                }
            }

            return html;
        }

        /// <summary>从响应数据中获取信息</summary>
        /// <param name="dic"></param>
        protected override void OnGetInfo(IDictionary<String, String> dic)
        {
            base.OnGetInfo(dic);

            if (dic.TryGetValue("UserId", out var str)) UserName = str.Trim();
            if (dic.TryGetValue("DeviceId", out str)) DeviceId = str.Trim();
            //if (dic.TryGetValue("OpenId", out str)) OpenID = str.Trim();
            if (dic.TryGetValue("biz_mail", out str)) Mail = str.Trim();

            if (dic.TryGetValue("errmsg", out str) && str != "ok") throw new InvalidOperationException(str);

        }
        #endregion
    }
}