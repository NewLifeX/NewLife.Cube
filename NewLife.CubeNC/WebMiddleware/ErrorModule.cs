using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace NewLife.Cube.WebMiddleware
{
    public class ErrorModuleMiddleware
    {
        private readonly RequestDelegate _next;

        public ErrorModuleMiddleware(RequestDelegate next)
        {
            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }

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
        public static IApplicationBuilder UseErrorModule(this IApplicationBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            return app.UseMiddleware<ErrorModuleMiddleware>();
        }
    }
}