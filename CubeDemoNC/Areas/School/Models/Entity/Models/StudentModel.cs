using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Web.Script.Serialization;
using System.Xml.Serialization;
using NewLife.Data;
using NewLife.Reflection;

namespace NewLife.School.Entity;

/// <summary>学生</summary>
public partial class StudentModel : IModel
{
    #region 属性
    /// <summary>编号</summary>
    public Int32 Id { get; set; }

    /// <summary>租户</summary>
    public Int32 TenantId { get; set; }

    /// <summary>班级</summary>
    public Int32 ClassId { get; set; }

    /// <summary>名称</summary>
    public String Name { get; set; }

    /// <summary>性别</summary>
    public XCode.Membership.SexKinds Sex { get; set; }

    /// <summary>年龄</summary>
    public Int32 Age { get; set; }

    /// <summary>手机</summary>
    public String Mobile { get; set; }

    /// <summary>地址</summary>
    public String Address { get; set; }

    /// <summary>启用</summary>
    public Boolean Enable { get; set; }

    /// <summary>创建者</summary>
    public Int32 CreateUserID { get; set; }

    /// <summary>创建时间</summary>
    public DateTime CreateTime { get; set; }

    /// <summary>创建地址</summary>
    public String CreateIP { get; set; }

    /// <summary>更新者</summary>
    public Int32 UpdateUserID { get; set; }

    /// <summary>更新时间</summary>
    public DateTime UpdateTime { get; set; }

    /// <summary>更新地址</summary>
    public String UpdateIP { get; set; }

    /// <summary>备注</summary>
    public String Remark { get; set; }
    #endregion

    #region 获取/设置 字段值
    /// <summary>获取/设置 字段值</summary>
    /// <param name="name">字段名</param>
    /// <returns></returns>
    public virtual Object this[String name]
    {
        get
        {
            return name switch
            {
                "Id" => Id,
                "TenantId" => TenantId,
                "ClassId" => ClassId,
                "Name" => Name,
                "Sex" => Sex,
                "Age" => Age,
                "Mobile" => Mobile,
                "Address" => Address,
                "Enable" => Enable,
                "CreateUserID" => CreateUserID,
                "CreateTime" => CreateTime,
                "CreateIP" => CreateIP,
                "UpdateUserID" => UpdateUserID,
                "UpdateTime" => UpdateTime,
                "UpdateIP" => UpdateIP,
                "Remark" => Remark,
                _ => this.GetValue(name, false),
            };
        }
        set
        {
            switch (name)
            {
                case "Id": Id = value.ToInt(); break;
                case "TenantId": TenantId = value.ToInt(); break;
                case "ClassId": ClassId = value.ToInt(); break;
                case "Name": Name = Convert.ToString(value); break;
                case "Sex": Sex = (XCode.Membership.SexKinds)value.ToInt(); break;
                case "Age": Age = value.ToInt(); break;
                case "Mobile": Mobile = Convert.ToString(value); break;
                case "Address": Address = Convert.ToString(value); break;
                case "Enable": Enable = value.ToBoolean(); break;
                case "CreateUserID": CreateUserID = value.ToInt(); break;
                case "CreateTime": CreateTime = value.ToDateTime(); break;
                case "CreateIP": CreateIP = Convert.ToString(value); break;
                case "UpdateUserID": UpdateUserID = value.ToInt(); break;
                case "UpdateTime": UpdateTime = value.ToDateTime(); break;
                case "UpdateIP": UpdateIP = Convert.ToString(value); break;
                case "Remark": Remark = Convert.ToString(value); break;
                default: this.SetValue(name, value); break;
            }
        }
    }
    #endregion

    #region 拷贝
    /// <summary>拷贝模型对象</summary>
    /// <param name="model">模型</param>
    public void Copy(IStudent model)
    {
        Id = model.Id;
        TenantId = model.TenantId;
        ClassId = model.ClassId;
        Name = model.Name;
        Sex = model.Sex;
        Age = model.Age;
        Mobile = model.Mobile;
        Address = model.Address;
        Enable = model.Enable;
        CreateUserID = model.CreateUserID;
        CreateTime = model.CreateTime;
        CreateIP = model.CreateIP;
        UpdateUserID = model.UpdateUserID;
        UpdateTime = model.UpdateTime;
        UpdateIP = model.UpdateIP;
        Remark = model.Remark;
    }
    #endregion
}
