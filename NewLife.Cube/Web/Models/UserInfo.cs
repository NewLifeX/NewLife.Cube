using System;
using System.Runtime.Serialization;

namespace NewLife.Cube.Web.Models
{
    /// <summary>用户信息</summary>
    public class UserInfo
    {
        /// <summary>标识userid</summary>
        [DataMember(Name = "userid")]
        public String Id { get; set; }

        /// <summary>名称</summary>
        public String Name { get; set; }

        /// <summary>手机</summary>
        public String Mobile { get; set; }

        /// <summary>部门</summary>
        public Int32[] Department { get; set; }

        /// <summary>顺序</summary>
        public Int32[] Order { get; set; }

        /// <summary>岗位</summary>
        public String Position { get; set; }

        /// <summary>性别</summary>
        public Int32 Gender { get; set; }

        /// <summary>邮箱</summary>
        [DataMember(Name = "mail")]
        public String Mail { get; set; }

        /// <summary>是否部门领导</summary>
        [DataMember(Name = "is_leader_in_dept")]
        public Int32[] IsLeader { get; set; }

        /// <summary>头像</summary>
        public String Avatar { get; set; }

        /// <summary>电话</summary>
        public String Telephone { get; set; }

        /// <summary>昵称</summary>
        public String Alias { get; set; }

        /// <summary>状态</summary>
        public Int32 Status { get; set; }

        /// <summary>地址</summary>
        public String Address { get; set; }

        /// <summary>开放平台标识</summary>
        [DataMember(Name = "open_userid")]
        public String OpenId { get; set; }

        /// <summary>主要部门</summary>
        [DataMember(Name = "main_department")]
        public String MainDepartment { get; set; }

        /// <summary>已重载。显示姓名</summary>
        public override String ToString() => Name;
    }
}