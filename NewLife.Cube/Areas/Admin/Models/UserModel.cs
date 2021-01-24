using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NewLife.Cube.Entity;
using NewLife.Web;

namespace NewLife.Cube.Areas.Admin.Models
{
    /// <summary>
    /// 继承此接口，可通过json方式传值
    /// </summary>
    public interface ICubeModel
    {

    }

    /// <summary>
    /// 登录模型
    /// </summary>
    public class LoginModel : ICubeModel
    {
        public String Username { get; set; }
        public String Password { get; set; }
        public Boolean Remember { get; set; }
    }


    /// <summary>注册模型</summary>
    public class RegisterModel : ICubeModel
    {
        public String Email { get; set; }
        public String Username { get; set; }
        public String Password { get; set; }
        public String Password2 { get; set; }
    }
}