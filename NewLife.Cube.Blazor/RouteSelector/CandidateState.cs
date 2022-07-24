using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace BigCookieKit.AspCore.RouteSelector
{
    internal struct CandidateState
    {
        internal CandidateState(Endpoint endpoint, int score)
        {
            Endpoint = endpoint;
            Score = score;
            Values = null;
        }

        internal CandidateState(Endpoint endpoint, RouteValueDictionary? values, int score)
        {
            Endpoint = endpoint;
            Values = values;
            Score = score;
        }

        public Endpoint Endpoint { get; }

        public int Score { get; }

        public RouteValueDictionary? Values { get; internal set; }
    }
}
