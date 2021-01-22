using System;
using System.Runtime.Serialization;

namespace NewLife.Cube.Web.Models
{
    /// <summary>打卡记录</summary>
    public class CheckInData
    {
        /// <summary>用户标识</summary>
        public String UserId { get; set; }

        /// <summary>打卡组</summary>
        public String GroupName { get; set; }

        /// <summary>打开类型。上班打卡、下班打卡、外出打卡</summary>
        [DataMember(Name = "checkin_type")]
        public String CheckInType { get; set; }

        /// <summary>异常类型</summary>
        [DataMember(Name = "exception_type")]
        public String ExceptionType { get; set; }

        /// <summary>打卡时间</summary>
        [DataMember(Name = "checkin_time")]
        public Int32 CheckInTime { get; set; }

        /// <summary>位置标题</summary>
        [DataMember(Name = "location_title")]
        public String LocationTitle { get; set; }

        /// <summary>位置明细</summary>
        [DataMember(Name = "location_detail")]
        public String LocationDetail { get; set; }

        /// <summary>纬度</summary>
        [DataMember(Name = "lat")]
        public Int32 Latitude { get; set; }

        /// <summary>经度</summary>
        [DataMember(Name = "lng")]
        public Int32 Longitude { get; set; }

        /// <summary>设备</summary>
        public String DeviceId { get; set; }

        /// <summary>Wifi名称</summary>
        public String WifiName { get; set; }

        /// <summary>Wifi的MAC地址</summary>
        public String WifiMac { get; set; }

        /// <summary>备注</summary>
        public String Notes { get; set; }

        /// <summary>相片媒体</summary>
        public String[] MediaIds { get; set; }

        /// <summary>计划打卡时间</summary>
        [DataMember(Name = "sch_checkin_time")]
        public Int32 ScheduleCheckInTime { get; set; }

        /// <summary>考勤组</summary>
        public Int32 GroupId { get; set; }

        /// <summary>计划标识</summary>
        [DataMember(Name = "schedule_id")]
        public Int32 ScheduleId { get; set; }

        /// <summary>考勤段</summary>
        [DataMember(Name = "timeline_id")]
        public Int32 TimelineId { get; set; }

        /// <summary>已重载。</summary>
        public override String ToString() => $"[{UserId}]{CheckInType} {CheckInTime.ToDateTime().ToLocalTime().ToFullString()}";
    }
}