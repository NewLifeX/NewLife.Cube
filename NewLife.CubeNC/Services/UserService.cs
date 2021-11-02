using System;
using System.Collections.Generic;
using System.Threading;
using NewLife.Cube.Entity;
using NewLife.Model;
using NewLife.Threading;
using XCode;
using XCode.Membership;

namespace NewLife.Cube.Services
{
    /// <summary>
    /// 用户服务
    /// </summary>
    public class UserService
    {
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
        public UserOnline SetWebStatus(String sessionid, String page, String status, IManageUser user, String ip)
        {
            // 网页使用一个定时器来清理过期
            StartTimer();

            if (user == null) return SetStatus(sessionid, page, status, 0, null, ip);

            //if (user is IAuthUser user2) user2.Online = true;
            //(user as IEntity).SaveAsync(1000);

            return SetStatus(sessionid, page, status, user.ID, user + "", ip);
        }

        private static TimerX _timer;
        private static Int32 _onlines;
        /// <summary>
        /// 启动定时器，定时清理离线用户
        /// </summary>
        /// <param name="period"></param>
        public static void StartTimer(Int32 period = 60)
        {
            if (_timer == null)
            {
                lock (typeof(UserOnline))
                {
                    if (_timer == null) _timer = new TimerX(s => ClearExpire(), null, 1000, period * 1000) { Async = true };
                }
            }
        }

        /// <summary>
        /// 关闭定时器
        /// </summary>
        public static void StopTimer()
        {
            _timer.TryDispose();
            _timer = null;
        }

        /// <summary>删除过期，指定过期时间</summary>
        /// <param name="secTimeout">超时时间，20 * 60秒</param>
        /// <returns></returns>
        public static IList<UserOnline> ClearExpire(Int32 secTimeout = 20 * 60)
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
    }
}