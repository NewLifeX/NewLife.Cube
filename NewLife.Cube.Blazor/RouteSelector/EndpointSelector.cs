using Microsoft.AspNetCore.Http;

using System.Threading.Tasks;

namespace BigCookieKit.AspCore.RouteSelector
{
    internal abstract class EndpointSelector
    {
        public abstract Task SelectAsync(HttpContext httpContext, CandidateSet candidates);
    }
}
