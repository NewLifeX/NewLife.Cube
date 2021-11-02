using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using NewLife.Cube.Entity;
using NewLife.Security;
using NewLife.Threading;
using XCode;
using XCode.Membership;

namespace NewLife.Cube.Services
{
    /// <summary>
    /// 用户服务
    /// </summary>
    public class UserService : IHostedService
    {
        #region 核心控制
        private TimerX _timer;
        private TimerX _timer2;
        private Int32 _onlines;

        /// <summary>启动</summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer = new TimerX(s => ClearExpire(), null, 1000, 60 * 1000) { Async = true };
            _timer2 = new TimerX(DoStat, null, 1000, 60 * 1000) { Async = true };

            return Task.CompletedTask;
        }

        /// <summary>停止</summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer.TryDispose();
            _timer2.TryDispose();

            return Task.CompletedTask;
        }
        #endregion

        #region 用户在线
        /// <summary>设置会话状态</summary>
        /// <param name="sessionid"></param>
        /// <param name="page"></param>
        /// <param name="status"></param>
        /// <param name="userid"></param>
        /// <param name="name"></param>
        /// <param name="ip"></param>
        /// <returns></returns>
        public UserOnline SetStatus(String sessionid, String page, String status, Int32 userid = 0, String name = null, String ip = null)
        {
            var entity = UserOnline.GetOrAdd(sessionid, UserOnline.FindBySessionID, k => new UserOnline { SessionID = k, CreateIP = ip, CreateTime = DateTime.Now });
            //var entity = FindBySessionID(sessionid) ?? new UserOnline();
            //entity.SessionID = sessionid;
            entity.Page = page;

            if (!status.IsNullOrEmpty() || entity.LastError.AddMinutes(3) < DateTime.Now) entity.Status = status;

            entity.Times++;
            if (userid > 0) entity.UserID = userid;
            if (!name.IsNullOrEmpty()) entity.Name = name;

            // 累加在线时间
            entity.UpdateTime = DateTime.Now;
            entity.UpdateIP = ip;
            entity.OnlineTime = (Int32)(entity.UpdateTime - entity.CreateTime).TotalSeconds;
            entity.SaveAsync(5_000);

            Interlocked.Increment(ref _onlines);

            return entity;
        }

        /// <summary>设置网页会话状态</summary>
        /// <param name="sessionid"></param>
        /// <param name="page"></param>
        /// <param name="status"></param>
        /// <param name="user"></param>
        /// <param name="ip"></param>
        /// <returns></returns>
        public UserOnline SetWebStatus(String sessionid, String page, String status, IUser user, String ip)
        {
            //// 网页使用一个定时器来清理过期
            //StartTimer();

            if (user == null) return SetStatus(sessionid, page, status, 0, null, ip);

            //if (user is IAuthUser user2) user2.Online = true;
            //(user as IEntity).SaveAsync(1000);

            return SetStatus(sessionid, page, status, user.ID, user + "", ip);
        }

        ///// <summary>
        ///// 启动定时器，定时清理离线用户
        ///// </summary>
        ///// <param name="period"></param>
        //public static void StartTimer(Int32 period = 60)
        //{
        //    if (_timer == null)
        //    {
        //        lock (typeof(UserOnline))
        //        {
        //            if (_timer == null) _timer = new TimerX(s => ClearExpire(), null, 1000, period * 1000) { Async = true };
        //        }
        //    }
        //}

        ///// <summary>
        ///// 关闭定时器
        ///// </summary>
        //public static void StopTimer()
        //{
        //    _timer.TryDispose();
        //    _timer = null;
        //}

        /// <summary>删除过期，指定过期时间</summary>
        /// <param name="secTimeout">超时时间，20 * 60秒</param>
        /// <returns></returns>
        public IList<UserOnline> ClearExpire(Int32 secTimeout = 20 * 60)
        {
            // 无在线则不执行
            if (_onlines == 0 || UserOnline.Meta.Count == 0) return new List<UserOnline>();

            // 10分钟不活跃将会被删除
            var exp = UserOnline._.UpdateTime < DateTime.Now.AddSeconds(-secTimeout);
            var list = UserOnline.FindAll(exp, null, null, 0, 0);
            list.Delete();

            // 修正在线数
            var total = UserOnline.Meta.Count;
            _onlines = total - list.Count;

            // 设置离线
            foreach (var item in list)
            {
                var user = ManageProvider.Provider.FindByID(item.UserID);
                if (user is User user2)
                {
                    user2.Online = false;
                    user2.OnlineTime += item.OnlineTime;
                    user2.Save();
                }
            }

            // 设置统计
            var stat = UserStat.GetOrAdd(DateTime.Today);
            if (stat != null)
            {
                if (total > stat.MaxOnline) stat.MaxOnline = total;
            }

            return list;
        }
        #endregion

        #region 用户统计
        private void DoStat(Object state)
        {
            // 无在线则不执行
            if (_onlines == 0) return;

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
            selects &= User._.OnlineTime.Sum();

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

                var sty = UserStat.FindByDate(DateTime.Today.AddDays(-1));
                if (sty != null)
                    st.OnlineTime = user.OnlineTime - sty.OnlineTime;
                else
                    st.OnlineTime = user.OnlineTime;

                st.Update();
            }
        }
        #endregion
    }
}