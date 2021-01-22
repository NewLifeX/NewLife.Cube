using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace NewLife.Cube.Web.Models
{
    /// <summary>审批信息</summary>
    public class ApprovalInfo
    {
        #region 属性
        /// <summary>编号</summary>
        [DataMember(Name = "sp_no")]
        public String No { get; set; }

        /// <summary>名称</summary>
        [DataMember(Name = "sp_name")]
        public String Name { get; set; }

        /// <summary>状态</summary>
        [DataMember(Name = "sp_status")]
        public Int32 Status { get; set; }

        /// <summary>模板</summary>
        [DataMember(Name = "template_id")]
        public String TemplateId { get; set; }

        /// <summary>申请时间</summary>
        [DataMember(Name = "apply_time")]
        public Int32 ApplyTime { get; set; }

        /// <summary>申请者</summary>
        internal ApplyerInfo Applyer { get; set; }

        /// <summary>记录</summary>
        [DataMember(Name = "sp_record")]
        public Object Record { get; set; }

        /// <summary>通知人</summary>
        public Object Notifyer { get; set; }

        /// <summary>申请数据</summary>
        [DataMember(Name = "apply_data")]
        internal ContentData ApplyData { get; set; }

        /// <summary>备注</summary>
        public Object Comments { get; set; }
        #endregion

        #region 构造
        /// <summary>已重载。显示名称</summary>
        /// <returns></returns>
        public override String ToString() => Name;
        #endregion

        #region 方法
        /// <summary>获取补卡数据</summary>
        /// <returns></returns>
        public PunchCorrection GetPunchCorrection()
        {
            var contents = ApplyData?.Contents;

            var info = new PunchCorrection();

            // 用户
            info.UserId = Applyer?.UserId;

            // 补卡
            {
                var pc = contents?.FirstOrDefault(_ => _.Control == "PunchCorrection");
                if (pc == null) return null;

                if (pc.Title != null && pc.Title.Length > 0) info.Title = pc.Title[0]?.Text;

                if (pc.Value["punch_correction"] is IDictionary<String, Object> dic)
                {
                    info.Time = dic["time"].ToDateTime().ToLocalTime();
                    info.State = dic["state"] as String;
                }
            }
            // 补卡事由
            {
                var txt = contents?.FirstOrDefault(_ => _.Control == "Textarea");
                if (txt != null)
                {
                    info.Reason = txt.Value["text"] as String;
                }
            }

            return info;
        }
        #endregion
    }

    /// <summary>申请人</summary>
    class ApplyerInfo
    {
        public String UserId { get; set; }

        public Int32 PartyId { get; set; }
    }

    /// <summary>内容</summary>
    class ContentData
    {
        public ApprovalContentInfo[] Contents { get; set; }
    }

    class ApprovalContentInfo
    {
        public String Id { get; set; }

        public String Control { get; set; }

        public TextInfo[] Title { get; set; }

        public IDictionary<String, Object> Value { get; set; }
    }

    class TextInfo
    {
        public String Text { get; set; }

        public String Lang { get; set; }
    }

    /// <summary>补卡</summary>
    public class PunchCorrection
    {
        /// <summary>用户标识</summary>
        public String UserId { get; set; }

        /// <summary>标题</summary>
        public String Title { get; set; }

        /// <summary>时间</summary>
        public DateTime Time { get; set; }

        /// <summary>状态</summary>
        public String State { get; set; }

        /// <summary>原因</summary>
        public String Reason { get; set; }
    }
}