using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Matching;

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace BigCookieKit.AspCore.RouteSelector
{
    internal class EndpointComparer : IComparer<Endpoint>, IEqualityComparer<Endpoint>
    {
        private readonly IComparer<Endpoint>[] _comparers;

        public EndpointComparer(IEndpointComparerPolicy[] policies)
        {
            _comparers = new IComparer<Endpoint>[2 + policies.Length];
            _comparers[0] = OrderComparer.Instance;
            _comparers[1] = PrecedenceComparer.Instance;
            for (var i = 0; i < policies.Length; i++)
            {
                _comparers[i + 2] = policies[i].Comparer;
            }
        }

        public int Compare(Endpoint? x, Endpoint? y)
        {
            Debug.Assert(x != null);
            Debug.Assert(y != null);

            var compare = CompareCore(x, y);

            return compare == 0 ? ComparePattern(x, y) : compare;
        }

        private static int ComparePattern(Endpoint x, Endpoint y)
        {
            var routeEndpointX = x as RouteEndpoint;
            var routeEndpointY = y as RouteEndpoint;

            if (routeEndpointX != null)
            {
                if (routeEndpointY != null)
                {
                    return string.Compare(routeEndpointX.RoutePattern.RawText, routeEndpointY.RoutePattern.RawText, StringComparison.OrdinalIgnoreCase);
                }

                return 1;
            }
            else if (routeEndpointY != null)
            {
                return -1;
            }

            return 0;
        }

        public bool Equals(Endpoint? x, Endpoint? y)
        {
            Debug.Assert(x != null);
            Debug.Assert(y != null);

            return CompareCore(x, y) == 0;
        }

        public int GetHashCode(Endpoint obj)
        {
            throw new NotImplementedException();
        }

        private int CompareCore(Endpoint x, Endpoint y)
        {
            for (var i = 0; i < _comparers.Length; i++)
            {
                var compare = _comparers[i].Compare(x, y);
                if (compare != 0)
                {
                    return compare;
                }
            }

            return 0;
        }

        private class OrderComparer : IComparer<Endpoint>
        {
            public static readonly IComparer<Endpoint> Instance = new OrderComparer();

            public int Compare(Endpoint? x, Endpoint? y)
            {
                var routeEndpointX = x as RouteEndpoint;
                var routeEndpointY = y as RouteEndpoint;

                if (routeEndpointX != null)
                {
                    if (routeEndpointY != null)
                    {
                        return routeEndpointX.Order.CompareTo(routeEndpointY.Order);
                    }

                    return 1;
                }
                else if (routeEndpointY != null)
                {
                    return -1;
                }

                return 0;
            }
        }

        private class PrecedenceComparer : IComparer<Endpoint>
        {
            public static readonly IComparer<Endpoint> Instance = new PrecedenceComparer();

            public int Compare(Endpoint? x, Endpoint? y)
            {
                var routeEndpointX = x as RouteEndpoint;
                var routeEndpointY = y as RouteEndpoint;

                if (routeEndpointX != null)
                {
                    if (routeEndpointY != null)
                    {
                        return routeEndpointX.RoutePattern.InboundPrecedence
                            .CompareTo(routeEndpointY.RoutePattern.InboundPrecedence);
                    }

                    return 1;
                }
                else if (routeEndpointY != null)
                {
                    return -1;
                }

                return 0;
            }
        }
    }
}
