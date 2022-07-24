using Microsoft.AspNetCore.Routing.Matching;

namespace BigCookieKit.AspCore.RouteSelector
{
    internal readonly struct DfaState
    {
        public readonly Candidate[] Candidates;
        public readonly IEndpointSelectorPolicy[] Policies;
        public readonly JumpTable PathTransitions;
        public readonly PolicyJumpTable PolicyTransitions;

        public DfaState(
            Candidate[] candidates,
            IEndpointSelectorPolicy[] policies,
            JumpTable pathTransitions,
            PolicyJumpTable policyTransitions)
        {
            Candidates = candidates;
            Policies = policies;
            PathTransitions = pathTransitions;
            PolicyTransitions = policyTransitions;
        }

    }
}
