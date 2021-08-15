using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.ViewModels;
using System;
using System.Linq;
using NewLife.Cube.Services;

#if __CORE__
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
#else
using System.Web.Mvc;
#endif

namespace NewLife.Cube.Admin.Controllers
{
    /// <summary>系统设置控制器</summary>
    [DisplayName("魔方设置")]
    [Area("Admin")]
    public class CubeController : ConfigController<Setting>
    {
        static CubeController() => MenuOrder = 34;

        private Boolean _has;
        private readonly UIService _uIService;

        /// <summary>实例化</summary>
        /// <param name="uIService"></param>
        public CubeController(UIService uIService) => _uIService = uIService;

        /// <summary>执行前</summary>
        /// <param name="filterContext"></param>
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (!_has)
            {
                var list = GetMembers(typeof(Setting));
                var pi = list.FirstOrDefault(e => e.Name == "Theme");
                if (pi != null)
                    pi.Description = $"可选主题 {_uIService.Themes.Join("/")}";

                pi = list.FirstOrDefault(e => e.Name == "Skin");
                if (pi != null)
                    pi.Description = $"可选皮肤 {_uIService.Skins.Join("/")}";

                _has = true;
            }

            base.OnActionExecuting(filterContext);
        }

        /// <summary>
        /// 获取登录设置
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        public ActionResult GetLoginConfig() => Ok(data: new LoginConfigModel());
    }
}