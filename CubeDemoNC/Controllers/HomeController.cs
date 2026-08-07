using Microsoft.AspNetCore.Mvc;
using NewLife.Cube;
using NewLife.Cube.AI;
using NewLife.Serialization;

namespace CubeDemo.Controllers
{
    /// <summary>主页面。实现 <see cref="IPageDataContext"/> 演示非实体页面为 AI 助手提供服务端数据上下文（get_page_context 工具优先调用）</summary>
    public class HomeController : AiPageControllerBase, IPageDataContext
    {
        /// <summary>主页面</summary>
        /// <returns></returns>
        public ActionResult Index()
        {
            ViewBag.Message = "主页面测试";

            return View();
        }

        /// <summary>收集当前页面数据上下文，供 AI 分析。演示：返回页面摘要与示例统计（仅安全字段，无敏感数据）</summary>
        /// <returns>结构化 JSON 字符串</returns>
        public Task<String> GetPageDataContextAsync()
        {
            var ctx = new
            {
                page = "主页面",
                summary = "魔方 MVC 示例项目主页，演示非实体页面接入 AI 数据上下文（IPageDataContext）",
                stats = new
                {
                    users = 1024,
                    orders = 2048,
                    note = "演示数据，非真实统计",
                },
            };
            return Task.FromResult(ctx.ToJson());
        }
    }
}