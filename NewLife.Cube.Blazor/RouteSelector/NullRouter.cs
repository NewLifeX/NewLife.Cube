using Microsoft.AspNetCore.Routing;

using System.Threading.Tasks;

namespace BigCookieKit.AspCore.RouteSelector
{
    internal class NullRouter : IRouter
    {
        public static readonly NullRouter Instance = new NullRouter();

        private NullRouter()
        {
        }

        public VirtualPathData? GetVirtualPath(VirtualPathContext context)
        {
            return null;
        }

        public Task RouteAsync(RouteContext context)
        {
            return Task.CompletedTask;
        }
    }
}
