﻿#pragma warning disable 1591
//------------------------------------------------------------------------------
// <auto-generated>
//     此代码由工具生成。
//     运行时版本:4.0.30319.42000
//
//     对此文件的更改可能会导致不正确的行为，并且如果
//     重新生成代码，这些更改将会丢失。
// </auto-generated>
//------------------------------------------------------------------------------

namespace ASP
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Web;
    using System.Web.Helpers;
    using System.Web.Mvc;
    using System.Web.Mvc.Ajax;
    using System.Web.Mvc.Html;
    using System.Web.Routing;
    using System.Web.Security;
    using System.Web.UI;
    using System.Web.WebPages;
    using NewLife;
    using NewLife.Cube;
    
    #line 2 "..\..\Areas\Admin\Views\User\_Login_Login3.cshtml"
    using NewLife.Cube.Entity;
    
    #line default
    #line hidden
    
    #line 1 "..\..\Areas\Admin\Views\User\_Login_Login3.cshtml"
    using NewLife.Cube.Web;
    
    #line default
    #line hidden
    using NewLife.Reflection;
    using NewLife.Web;
    using XCode;
    using XCode.Membership;
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("RazorGenerator", "2.0.0.0")]
    [System.Web.WebPages.PageVirtualPathAttribute("~/Areas/Admin/Views/User/_Login_Login3.cshtml")]
    public partial class _Areas_Admin_Views_User__Login_Login3_cshtml : System.Web.Mvc.WebViewPage<dynamic>
    {
        public _Areas_Admin_Views_User__Login_Login3_cshtml()
        {
        }
        public override void Execute()
        {
            
            #line 3 "..\..\Areas\Admin\Views\User\_Login_Login3.cshtml"
  
    var set = NewLife.Cube.Setting.Current;
    var returnUrl = ViewBag.ReturnUrl as String;
    var ms = NewLife.Cube.Entity.OAuthConfig.GetValids();

            
            #line default
            #line hidden
WriteLiteral("\r\n");

            
            #line 8 "..\..\Areas\Admin\Views\User\_Login_Login3.cshtml"
 if (ms.Count > 0 && set.AutoRegister)
{

            
            #line default
            #line hidden
WriteLiteral("    <div");

WriteLiteral(" class=\"text-center\"");

WriteLiteral(">\r\n        <div");

WriteLiteral(" class=\"col-sm-12\"");

WriteLiteral(">\r\n            <p");

WriteLiteral(" style=\"font-size: 22px; position: relative; display: inline-block;width: 100%\"");

WriteLiteral(">\r\n                <span");

WriteLiteral(" style=\"height: 1px; position: absolute; background-color: #928f8f; width: 28%; t" +
"op: 50%; left:65%;\"");

WriteLiteral("></span>\r\n                第三方登录\r\n                <span");

WriteLiteral(" style=\"height: 1px; position: absolute; background-color: #928f8f; width: 28%; t" +
"op: 50%; right:65%;\"");

WriteLiteral("></span>\r\n            </p>\r\n        </div>\r\n        <div");

WriteLiteral(" class=\"row\"");

WriteLiteral(" style=\"padding: 0 0 0 0;\"");

WriteLiteral(">\r\n            <div");

WriteLiteral(" class=\"form-group col-sm-12\"");

WriteLiteral(">\r\n");

            
            #line 20 "..\..\Areas\Admin\Views\User\_Login_Login3.cshtml"
                
            
            #line default
            #line hidden
            
            #line 20 "..\..\Areas\Admin\Views\User\_Login_Login3.cshtml"
                 foreach (var mi in ms)
                {
                    var nickName = !mi.NickName.IsNullOrEmpty() ? mi.NickName : mi.Name;

                    var url = "Sso/Login?name=" + mi.Name;
                    if (!returnUrl.IsNullOrEmpty())
                    {
                        url += "&r=" + HttpUtility.UrlEncode(returnUrl);
                    }

                    url = HttpRuntime.AppDomainAppVirtualPath + url;
                    var logo = !mi.Logo.IsNullOrEmpty() ? mi.Logo : ViewHelper.GetLogo(mi.Name);


            
            #line default
            #line hidden
WriteLiteral("                    <a");

WriteAttribute("href", Tuple.Create(" href=\"", 1419), Tuple.Create("\"", 1430)
            
            #line 33 "..\..\Areas\Admin\Views\User\_Login_Login3.cshtml"
, Tuple.Create(Tuple.Create("", 1426), Tuple.Create<System.Object, System.Int32>(url
            
            #line default
            #line hidden
, 1426), false)
);

WriteLiteral(">\r\n");

            
            #line 34 "..\..\Areas\Admin\Views\User\_Login_Login3.cshtml"
                        
            
            #line default
            #line hidden
            
            #line 34 "..\..\Areas\Admin\Views\User\_Login_Login3.cshtml"
                         if (!logo.IsNullOrEmpty())
                        {

            
            #line default
            #line hidden
WriteLiteral("                            ");

WriteLiteral("<img");

WriteAttribute("src", Tuple.Create(" src=\"", 1552), Tuple.Create("\"", 1563)
            
            #line 36 "..\..\Areas\Admin\Views\User\_Login_Login3.cshtml"
, Tuple.Create(Tuple.Create("", 1558), Tuple.Create<System.Object, System.Int32>(logo
            
            #line default
            #line hidden
, 1558), false)
);

WriteAttribute("title", Tuple.Create(" title=\"", 1564), Tuple.Create("\"", 1581)
            
            #line 36 "..\..\Areas\Admin\Views\User\_Login_Login3.cshtml"
, Tuple.Create(Tuple.Create("", 1572), Tuple.Create<System.Object, System.Int32>(nickName
            
            #line default
            #line hidden
, 1572), false)
);

WriteLiteral(" style=\"width: 64px; height: 64px;\"");

WriteLiteral(" />");

WriteLiteral("\r\n");

            
            #line 37 "..\..\Areas\Admin\Views\User\_Login_Login3.cshtml"
                        }
                        else
                        {

            
            #line default
            #line hidden
WriteLiteral("                            <i");

WriteLiteral(" class=\"glyphicon glyphicon-menu-right\"");

WriteLiteral("></i>\r\n");

WriteLiteral("                            ");

            
            #line 41 "..\..\Areas\Admin\Views\User\_Login_Login3.cshtml"
                             Write(nickName);

            
            #line default
            #line hidden
WriteLiteral("\r\n");

            
            #line 42 "..\..\Areas\Admin\Views\User\_Login_Login3.cshtml"
                        }

            
            #line default
            #line hidden
WriteLiteral("                    </a>\r\n");

            
            #line 44 "..\..\Areas\Admin\Views\User\_Login_Login3.cshtml"
                }

            
            #line default
            #line hidden
WriteLiteral("            </div>\r\n        </div>\r\n    </div>\r\n");

            
            #line 48 "..\..\Areas\Admin\Views\User\_Login_Login3.cshtml"
}
            
            #line default
            #line hidden
        }
    }
}
#pragma warning restore 1591
