using Microsoft.AspNetCore.Http;

using System.Collections.Generic;
using System.Threading.Tasks;

namespace BigCookieKit.AspCore.RouteSelector
{
    internal interface IEndpointSelectorPolicy
    {
        bool AppliesToEndpoints(IReadOnlyList<Endpoint> endpoints);

        Task ApplyAsync(HttpContext httpContext, CandidateSet candidates);
    }
}
