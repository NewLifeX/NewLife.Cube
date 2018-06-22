using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace NewLife.CubeNC.WebMiddleware
{
    public class ErrorModule
    {
        private readonly RequestDelegate _next;

        public ErrorModule(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            // Do something with context near the beginning of request processing.

            await _next.Invoke(context);

            // Clean up.
        }
    }

    public static class ErrorModuleMiddlewareExtensions
    {
        public static IApplicationBuilder UseErrorModuleMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ErrorModule>();
        }
    }
}