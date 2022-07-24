using Microsoft.AspNetCore.Http;

using System.Threading.Tasks;

namespace BigCookieKit.AspCore.RouteSelector
{
    internal abstract class Matcher
    {
        public abstract Task MatchAsync(HttpContext httpContext);

        public abstract Task<Endpoint> SelectorAsync(string path, string httpMethod = "GET");
    }
}
