using Microsoft.AspNetCore.Mvc;
using XCode.Membership;

namespace NewLife.Cube.Metronic8.Controllers
{
    /// <summary>
    /// 界面布局控制器，主要用于保存魔方界面状态
    /// </summary>
    /// <seealso cref="Microsoft.AspNetCore.Mvc.Controller" />
    public class LayoutController : Controller
    {
        /// <summary>
        /// 保存界面状态
        /// </summary>
        /// <param name="para">The para.</param>
        /// <returns></returns>
        public EmptyResult SaveLayout(Parameter para) 
        { 
            if(para != null)
                para.Save();

            return new EmptyResult();   
        }
    }
}
