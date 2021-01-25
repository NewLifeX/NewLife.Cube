using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NewLife.Cube.Entity;
using NewLife.Web;

namespace NewLife.Cube.Areas.Admin.Models
{
    /// <summary>第三方绑定模型</summary>
    public class BindsModel : ICubeModel
    {
        /// <summary>用户名</summary>
        public String Name { get; set; }

        /// <summary>用户链接集合</summary>
        public IList<UserConnect> Connects { get; set; }

        /// <summary>可选的第三方</summary>
        public IList<OAuthItem> OAuthItems { get; set; }
    }
}