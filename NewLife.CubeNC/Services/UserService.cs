using NewLife.Cube.Entity;
using NewLife.Cube.Web;
using NewLife.Log;
using NewLife.Threading;
using XCode;
using XCode.Membership;

namespace NewLife.Cube.Services;

/// <summary>
/// 用户服务
/// </summary>
public class UserService
{
    private readonly ITracer _tracer;

    /// <summary>
    /// 实例化用户服务
    /// </summary>
    /// <param name="provider"></param>
    public UserService(IServiceProvider provider) => _tracer = provider?.GetService<ITracer>();

    #region 核心控制
    private TimerX _timer;
    private TimerX _timer2;
    private Int32 _onlines;

    private void StartTimer()
    {
        if (_timer != null) return;

        lock (this)
        {
            if (_timer != null) return;

            //!!! 临时关闭OnlineTime累加字段
            User.Meta.Factory.AdditionalFields.Remove(nameof(User.__.OnlineTime));

            _timer = new TimerX(s => ClearExpire(), null, 1000, 60 * 1000) { Async = true };
            _timer2 = new TimerX(DoStat, null, 1000, 60 * 1000) { Async = true };
        }
    }
    #endregion

    #region 用户在线
    /// <summary>设置会话状态</summary>
    /// <param name="sessionId"></param>
    /// <param name="deviceId"></param>
    /// <param name="page"></param>
    /// <param name="status"></param>
    /// <param name="userAgent"></param>
    /// <param name="userid"></param>
    /// <param name="name"></param>
    /// <param name="ip"></param>
    /// <returns></returns>
    public UserOnline SetStatus(String sessionId, String deviceId, String page, String status, UserAgentParser userAgent, Int32 userid = 0, String name = null, String ip = null)
    {
        // LastError 设计缺陷，非空设计导致无法在插入中忽略
        var entity = UserOnline.GetOrAdd(sessionId, UserOnline.FindBySessionID, k => new UserOnline
        {
            SessionID = k,
            LastError = new DateTime(1970, 1, 2),//MSSql不能使用1973年之前的日期
            CreateIP = ip,
            CreateTime = DateTime.Now
        });
        //var entity = FindBySessionID(sessionid) ?? new UserOnline();
        //entity.SessionID = sessionid;
        entity.DeviceId = deviceId;
        entity.Page = page;

        if (userAgent != null)
        {
            entity.Platform = userAgent.Platform;
            entity.OS = userAgent.OSorCPU;
            if (userAgent.Device.IsNullOrEmpty() || !userAgent.DeviceBuild.IsNullOrEmpty() && userAgent.DeviceBuild.Contains(userAgent.Device))
                entity.Device = userAgent.DeviceBuild;
            else
                entity.Device = userAgent.Device;
            entity.Brower = userAgent.Brower;
            entity.NetType = userAgent.NetType;
        }

        if (!status.IsNullOrEmpty() || entity.LastError.AddMinutes(3) < DateTime.Now) entity.Status = status;

        entity.Times++;
        if (userid > 0) entity.UserID = userid;
        if (!name.IsNullOrEmpty()) entity.Name = name;

        entity.Address = ip.IPToAddress();

        // 累加在线时间
        entity.UpdateTime = DateTime.Now;
        entity.UpdateIP = ip;
        entity.OnlineTime = (Int32)(entity.UpdateTime - entity.CreateTime).TotalSeconds;
        entity.TraceId = DefaultSpan.Current?.TraceId;
        entity.SaveAsync(5_000);

        Interlocked.Increment(ref _onlines);

        return entity;
    }

    /// <summary>设置网页会话状态</summary>
    /// <param name="sessionId"></param>
    /// <param name="deviceId"></param>
    /// <param name="page"></param>
    /// <param name="status"></param>
    /// <param name="userAgent"></param>
    /// <param name="user"></param>
    /// <param name="ip"></param>
    /// <returns></returns>
    public UserOnline SetWebStatus(String sessionId, String deviceId, String page, String status, UserAgentParser userAgent, IUser user, String ip)
    {
        // 网页使用一个定时器来清理过期
        StartTimer();

        if (user == null) return SetStatus(sessionId, deviceId, page, status, userAgent, 0, null, ip);

        // 根据IP修正用户城市
        if (user is User user2 && (user2.AreaId == 0 || user2.AreaId % 10000 == 0))
        {
            try
            {
                var rs = Area.SearchIP(ip, 2);
                if (rs.Count > 0)
                {
                    user2.AreaId = rs[rs.Count - 1].ID;
                    user2.SaveAsync();
                }
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);
            }
        }

        return SetStatus(sessionId, deviceId, page, status, userAgent, user.ID, user + "", ip);
    }

    /// <summary>删除过期，指定过期时间</summary>
    /// <param name="secTimeout">超时时间，20 * 60秒</param>
    /// <returns></returns>
    public IList<UserOnline> ClearExpire(Int32 secTimeout = 20 * 60)
    {
        // 无在线则不执行
        if (_onlines == 0) return new List<UserOnline>();

        using var span = _tracer?.NewSpan("ClearExpireOnline");

        // 减少Sql日志
        var dal = UserOnline.Meta.Session.Dal;
        var oldSql = dal.Session.ShowSQL;
        dal.Session.ShowSQL = false;
        try
        {
            // 10分钟不活跃将会被删除
            var exp = UserOnline._.UpdateTime < DateTime.Now.AddSeconds(-secTimeout);
            var list = UserOnline.FindAll(exp, null, null, 0, 0);
            list.Delete();

            // 修正在线数
            var total = UserOnline.Meta.Count;
            _onlines = total - list.Count;

            // 设置统计
            var stat = UserStat.GetOrAdd(DateTime.Today);
            if (stat != null)
            {
                if (total > stat.MaxOnline) stat.MaxOnline = total;
            }

            // 设置离线
            foreach (var item in list)
            {
                var user = ManageProvider.Provider.FindByID(item.UserID);
                if (user is User user2)
                {
                    user2.Online = false;
                    user2.OnlineTime += item.OnlineTime;
                    user2.Save();

                    if (stat != null) stat.OnlineTime += item.OnlineTime;
                }
            }
            stat.SaveAsync();

            return list;
        }
        catch (Exception ex)
        {
            span?.SetError(ex, null);
            throw;
        }
        finally
        {
            dal.Session.ShowSQL = oldSql;
        }
    }

    /// <summary>
    /// 注销用户时，更新在线表和统计表
    /// </summary>
    /// <param name="user"></param>
    public static void ClearOnline(User user)
    {
        // 在线表删除
        var olts = UserOnline.FindAllByUserID(user.ID);
        if (olts.Count > 0)
        {
            var stat = UserStat.GetOrAdd(DateTime.Today);
            foreach (var olt in olts)
            {
                user.OnlineTime += olt.OnlineTime;
                olt.Delete();

                if (stat != null) stat.OnlineTime += olt.OnlineTime;
            }
            stat.SaveAsync();
        }

        user.Online = false;
        user.SaveAsync();
    }
    #endregion

    #region 用户统计
    private void DoStat(Object state)
    {
        // 无在线则不执行
        if (_onlines == 0) return;

        var set = CubeSetting.Current;
        if (!set.EnableUserStat) return;

        using var span = _tracer?.NewSpan("UserStat");

        var t1 = DateTime.Today.AddDays(-0);
        var t7 = DateTime.Today.AddDays(-7);
        var t30 = DateTime.Today.AddDays(-30);

        var selects = UserStat._.ID.Count();
        selects &= User._.LastLogin.SumLarge($"'{t1:yyyy-MM-dd}'", "activeT1");
        selects &= User._.LastLogin.SumLarge($"'{t7:yyyy-MM-dd}'", "activeT7");
        selects &= User._.LastLogin.SumLarge($"'{t30:yyyy-MM-dd}'", "activeT30");
        selects &= User._.RegisterTime.SumLarge($"'{t1:yyyy-MM-dd}'", "newT1");
        selects &= User._.RegisterTime.SumLarge($"'{t7:yyyy-MM-dd}'", "newT7");
        selects &= User._.RegisterTime.SumLarge($"'{t30:yyyy-MM-dd}'", "newT30");
        //selects &= User._.OnlineTime.Sum();

        // 减少Sql日志
        var dal = UserOnline.Meta.Session.Dal;
        var oldSql = dal.Session.ShowSQL;
        dal.Session.ShowSQL = false;
        try
        {
            var list = User.FindAll(null, null, selects, 0, 1);
            if (list.Count > 0)
            {
                var user = list[0];

                var st = UserStat.GetOrAdd(DateTime.Today);
                st.Total = user.ID;
                st.Actives = user["activeT1"].ToInt();
                st.ActivesT7 = user["activeT7"].ToInt();
                st.ActivesT30 = user["activeT30"].ToInt();
                st.News = user["newT1"].ToInt();
                st.NewsT7 = user["newT7"].ToInt();
                st.NewsT30 = user["newT30"].ToInt();

                //var sty = UserStat.FindByDate(DateTime.Today.AddDays(-1));
                //if (sty != null)
                //    st.OnlineTime = user.OnlineTime - sty.OnlineTime;
                //else
                //    st.OnlineTime = user.OnlineTime;

                st.Update();
            }
        }
        catch (Exception ex)
        {
            span?.SetError(ex, null);
            throw;
        }
        finally
        {
            dal.Session.ShowSQL = oldSql;
        }
    }
    #endregion
}