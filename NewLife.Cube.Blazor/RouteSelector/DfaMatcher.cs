using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;

namespace BigCookieKit.AspCore.RouteSelector
{
    internal sealed partial class DfaMatcher : Matcher
    {
        private readonly EndpointSelector _selector;
        private readonly DfaState[] _states;
        private readonly int _maxSegmentCount;
        private readonly bool _isDefaultEndpointSelector;

        public DfaMatcher(EndpointSelector selector, DfaState[] states, int maxSegmentCount)
        {
            _selector = selector;
            _states = states;
            _maxSegmentCount = maxSegmentCount;
            _isDefaultEndpointSelector = selector is DefaultEndpointSelector;
        }

        public sealed override Task MatchAsync(HttpContext httpContext)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            var path = httpContext.Request.Path.Value!;

            Span<PathSegment> buffer = stackalloc PathSegment[_maxSegmentCount];
            var count = FastPathTokenizerHelpers.Tokenize(path, buffer);
            var segments = buffer.Slice(0, count);

            var (candidates, policies) = FindCandidateSet(httpContext, path, segments);
            var candidateCount = candidates.Length;
            if (candidateCount == 0)
            {

                return Task.CompletedTask;
            }

            var policyCount = policies.Length;

            if (candidateCount == 1 && policyCount == 0 && _isDefaultEndpointSelector)
            {
                ref readonly var candidate = ref candidates[0];

                if (candidate.Flags == Candidate.CandidateFlags.None)
                {
                    httpContext.SetEndpoint(candidate.Endpoint);

                    return Task.CompletedTask;
                }
            }

            var candidateState = new CandidateState[candidateCount];

            for (var i = 0; i < candidateCount; i++)
            {
                ref readonly var candidate = ref candidates[i];
                ref var state = ref candidateState[i];
                state = new CandidateState(candidate.Endpoint, candidate.Score);

                var flags = candidate.Flags;

                if ((flags & Candidate.CandidateFlags.HasSlots) != 0)
                {
                    var prototype = candidate.Slots;
                    var slots = new KeyValuePair<string, object?>[prototype.Length];

                    if ((flags & Candidate.CandidateFlags.HasDefaults) != 0)
                    {
                        Array.Copy(prototype, 0, slots, 0, prototype.Length);
                    }

                    if ((flags & Candidate.CandidateFlags.HasCaptures) != 0)
                    {
                        ProcessCaptures(slots, candidate.Captures, path, segments);
                    }

                    if ((flags & Candidate.CandidateFlags.HasCatchAll) != 0)
                    {
                        ProcessCatchAll(slots, candidate.CatchAll, path, segments);
                    }

                    state.Values = RouteValueDictionary.FromArray(slots);
                }

                var isMatch = true;
                if ((flags & Candidate.CandidateFlags.HasComplexSegments) != 0)
                {
                    state.Values ??= new RouteValueDictionary();
                    if (!ProcessComplexSegments(candidate.Endpoint, candidate.ComplexSegments, path, segments, state.Values))
                    {
                        CandidateSet.SetValidity(ref state, false);
                        isMatch = false;
                    }
                }

                if ((flags & Candidate.CandidateFlags.HasConstraints) != 0)
                {
                    state.Values ??= new RouteValueDictionary();
                    if (!ProcessConstraints(candidate.Endpoint, candidate.Constraints, httpContext, state.Values))
                    {
                        CandidateSet.SetValidity(ref state, false);
                        isMatch = false;
                    }
                }
            }

            if (policyCount == 0 && _isDefaultEndpointSelector)
            {
                DefaultEndpointSelector.Select(httpContext, candidateState);
                return Task.CompletedTask;
            }
            else if (policyCount == 0)
            {
                return _selector.SelectAsync(httpContext, new CandidateSet(candidateState));
            }

            return SelectEndpointWithPoliciesAsync(httpContext, policies, new CandidateSet(candidateState));
        }

        public sealed override Task<Endpoint> SelectorAsync(string path, string httpMethod = "GET")
        {
            Span<PathSegment> buffer = stackalloc PathSegment[_maxSegmentCount];
            var count = FastPathTokenizerHelpers.Tokenize(path, buffer);
            var segments = buffer.Slice(0, count);

            var (candidates, policies) = FindCandidateSet(path, httpMethod, segments);
            var candidateCount = candidates.Length;
            if (candidateCount == 0)
            {
                return Task.FromResult<Endpoint>(null);
            }

            var policyCount = policies.Length;

            if (candidateCount == 1 && policyCount == 0 && _isDefaultEndpointSelector)
            {
                ref readonly var candidate = ref candidates[0];

                if (candidate.Flags == Candidate.CandidateFlags.None)
                {
                    return Task.FromResult(candidate.Endpoint);
                }
            }

            var candidateState = new CandidateState[candidateCount];

            for (var i = 0; i < candidateCount; i++)
            {
                ref readonly var candidate = ref candidates[i];
                ref var state = ref candidateState[i];
                state = new CandidateState(candidate.Endpoint, candidate.Score);

                var flags = candidate.Flags;

                if ((flags & Candidate.CandidateFlags.HasSlots) != 0)
                {
                    var prototype = candidate.Slots;
                    var slots = new KeyValuePair<string, object?>[prototype.Length];

                    if ((flags & Candidate.CandidateFlags.HasDefaults) != 0)
                    {
                        Array.Copy(prototype, 0, slots, 0, prototype.Length);
                    }

                    if ((flags & Candidate.CandidateFlags.HasCaptures) != 0)
                    {
                        ProcessCaptures(slots, candidate.Captures, path, segments);
                    }

                    if ((flags & Candidate.CandidateFlags.HasCatchAll) != 0)
                    {
                        ProcessCatchAll(slots, candidate.CatchAll, path, segments);
                    }

                    state.Values = RouteValueDictionary.FromArray(slots);
                }

                var isMatch = true;
                if ((flags & Candidate.CandidateFlags.HasComplexSegments) != 0)
                {
                    state.Values ??= new RouteValueDictionary();
                    if (!ProcessComplexSegments(candidate.Endpoint, candidate.ComplexSegments, path, segments, state.Values))
                    {
                        CandidateSet.SetValidity(ref state, false);
                        isMatch = false;
                    }
                }
            }

            return Task.FromResult(DefaultEndpointSelector.ProcessFinalCandidates(candidateState));
        }

        internal (Candidate[] candidates, IEndpointSelectorPolicy[] policies) FindCandidateSet(
            HttpContext httpContext,
            string path,
            ReadOnlySpan<PathSegment> segments)
        {
            var states = _states;

            var destination = 0;
            for (var i = 0; i < segments.Length; i++)
            {
                destination = states[destination].PathTransitions.GetDestination(path, segments[i]);
            }

            var policyTransitions = states[destination].PolicyTransitions;
            while (policyTransitions != null)
            {
                destination = policyTransitions.GetDestination(httpContext);
                policyTransitions = states[destination].PolicyTransitions;
            }

            return (states[destination].Candidates, states[destination].Policies);
        }

        internal (Candidate[] candidates, IEndpointSelectorPolicy[] policies) FindCandidateSet(
            string path,
            string httpMethod,
            ReadOnlySpan<PathSegment> segments)
        {
            var states = _states;

            var destination = 0;
            for (var i = 0; i < segments.Length; i++)
            {
                destination = states[destination].PathTransitions.GetDestination(path, segments[i]);
            }
            var policyTransitions = states[destination].PolicyTransitions;
            while (policyTransitions != null)
            {
                var BindFlag = BindingFlags.NonPublic | BindingFlags.Instance;
                var policyType = policyTransitions.GetType();
                var _method = policyType.GetField("_method", BindFlag).GetValue(policyTransitions);
                var _destination = policyType.GetField("_destination", BindFlag).GetValue(policyTransitions);
                var _exitDestination = policyType.GetField("_exitDestination", BindFlag).GetValue(policyTransitions);
                destination = Convert.ToInt32(Equals(httpMethod, _method) ? _destination : _exitDestination);
                policyTransitions = states[destination].PolicyTransitions;
            }
            return (states[destination].Candidates, states[destination].Policies);
        }

        private static void ProcessCaptures(
            KeyValuePair<string, object?>[] slots,
            (string parameterName, int segmentIndex, int slotIndex)[] captures,
            string path,
            ReadOnlySpan<PathSegment> segments)
        {
            for (var i = 0; i < captures.Length; i++)
            {
                (var parameterName, var segmentIndex, var slotIndex) = captures[i];

                if ((uint)segmentIndex < (uint)segments.Length)
                {
                    var segment = segments[segmentIndex];
                    if (parameterName != null && segment.Length > 0)
                    {
                        slots[slotIndex] = new KeyValuePair<string, object?>(
                            parameterName,
                            path.Substring(segment.Start, segment.Length));
                    }
                }
            }
        }

        private static void ProcessCatchAll(
            KeyValuePair<string, object?>[] slots,
            in (string parameterName, int segmentIndex, int slotIndex) catchAll,
            string path,
            ReadOnlySpan<PathSegment> segments)
        {
            var segmentIndex = catchAll.segmentIndex;
            if ((uint)segmentIndex < (uint)segments.Length)
            {
                var segment = segments[segmentIndex];
                slots[catchAll.slotIndex] = new KeyValuePair<string, object?>(
                    catchAll.parameterName,
                    path.Substring(segment.Start));
            }
        }

        internal static bool MatchComplexSegment(
            RoutePatternPathSegment routeSegment,
            ReadOnlySpan<char> requestSegment,
            RouteValueDictionary values)
        {
            var indexOfLastSegment = routeSegment.Parts.Count - 1;

            if (routeSegment.Parts[indexOfLastSegment] is RoutePatternParameterPart parameter && parameter.IsOptional &&
                routeSegment.Parts[indexOfLastSegment - 1].IsSeparator)
            {
                if (MatchComplexSegmentCore(routeSegment, requestSegment, values, indexOfLastSegment))
                {
                    return true;
                }
                else
                {
                    var separator = (RoutePatternSeparatorPart)routeSegment.Parts[indexOfLastSegment - 1];
                    if (requestSegment.EndsWith(
                        separator.Content,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }

                    return MatchComplexSegmentCore(
                        routeSegment,
                        requestSegment,
                        values,
                        indexOfLastSegment - 2);
                }
            }
            else
            {
                return MatchComplexSegmentCore(routeSegment, requestSegment, values, indexOfLastSegment);
            }
        }

        private static bool MatchComplexSegmentCore(
            RoutePatternPathSegment routeSegment,
            ReadOnlySpan<char> requestSegment,
            RouteValueDictionary values,
            int indexOfLastSegmentUsed)
        {
            Debug.Assert(routeSegment != null);
            Debug.Assert(routeSegment.Parts.Count > 1);

            var lastIndex = requestSegment.Length;

            RoutePatternParameterPart parameterNeedsValue = null;
            RoutePatternPart lastLiteral = null;

            var outValues = new RouteValueDictionary();

            while (indexOfLastSegmentUsed >= 0)
            {
                var newLastIndex = lastIndex;

                var part = routeSegment.Parts[indexOfLastSegmentUsed];
                if (part.IsParameter)
                {
                    parameterNeedsValue = (RoutePatternParameterPart)part;
                }
                else
                {
                    Debug.Assert(part.IsLiteral || part.IsSeparator);
                    lastLiteral = part;

                    var startIndex = lastIndex;
                    if (parameterNeedsValue != null)
                    {
                        startIndex--;
                    }

                    if (startIndex == 0)
                    {
                        return false;
                    }

                    int indexOfLiteral;
                    if (part.IsLiteral)
                    {
                        var literal = (RoutePatternLiteralPart)part;
                        indexOfLiteral = requestSegment.Slice(0, startIndex).LastIndexOf(
                        literal.Content,
                        StringComparison.OrdinalIgnoreCase);
                    }
                    else
                    {
                        var literal = (RoutePatternSeparatorPart)part;
                        indexOfLiteral = requestSegment.Slice(0, startIndex).LastIndexOf(
                        literal.Content,
                        StringComparison.OrdinalIgnoreCase);
                    }

                    if (indexOfLiteral == -1)
                    {
                        return false;
                    }

                    if (indexOfLastSegmentUsed == (routeSegment.Parts.Count - 1))
                    {
                        if (part is RoutePatternLiteralPart literal && ((indexOfLiteral + literal.Content.Length) != requestSegment.Length))
                        {
                            return false;
                        }
                        else if (part is RoutePatternSeparatorPart separator && ((indexOfLiteral + separator.Content.Length) != requestSegment.Length))
                        {
                            return false;
                        }
                    }

                    newLastIndex = indexOfLiteral;
                }

                if ((parameterNeedsValue != null) &&
                    (((lastLiteral != null) && !part.IsParameter) || (indexOfLastSegmentUsed == 0)))
                {

                    int parameterStartIndex;
                    int parameterTextLength;

                    if (lastLiteral == null)
                    {
                        if (indexOfLastSegmentUsed == 0)
                        {
                            parameterStartIndex = 0;
                        }
                        else
                        {
                            parameterStartIndex = newLastIndex;
                            Debug.Assert(false, "indexOfLastSegementUsed should always be 0 from the check above");
                        }
                        parameterTextLength = lastIndex;
                    }
                    else
                    {
                        if ((indexOfLastSegmentUsed == 0) && (part.IsParameter))
                        {
                            parameterStartIndex = 0;
                            parameterTextLength = lastIndex;
                        }
                        else
                        {
                            if (lastLiteral.IsLiteral)
                            {
                                var literal = (RoutePatternLiteralPart)lastLiteral;
                                parameterStartIndex = newLastIndex + literal.Content.Length;
                            }
                            else
                            {
                                var separator = (RoutePatternSeparatorPart)lastLiteral;
                                parameterStartIndex = newLastIndex + separator.Content.Length;
                            }
                            parameterTextLength = lastIndex - parameterStartIndex;
                        }
                    }

                    var parameterValueSpan = requestSegment.Slice(parameterStartIndex, parameterTextLength);

                    if (parameterValueSpan.Length == 0)
                    {
                        return false;
                    }
                    else
                    {
                        outValues.Add(parameterNeedsValue.Name, new string(parameterValueSpan));
                    }

                    parameterNeedsValue = null;
                    lastLiteral = null;
                }

                lastIndex = newLastIndex;
                indexOfLastSegmentUsed--;
            }

            if (lastIndex == 0 || routeSegment.Parts[0].IsParameter)
            {
                foreach (var item in outValues)
                {
                    values[item.Key] = item.Value;
                }

                return true;
            }

            return false;
        }

        private bool ProcessComplexSegments(
            Endpoint endpoint,
            (RoutePatternPathSegment pathSegment, int segmentIndex)[] complexSegments,
            string path,
            ReadOnlySpan<PathSegment> segments,
            RouteValueDictionary values)
        {
            for (var i = 0; i < complexSegments.Length; i++)
            {
                (var complexSegment, var segmentIndex) = complexSegments[i];
                var segment = segments[segmentIndex];
                var text = path.AsSpan(segment.Start, segment.Length);
                if (!MatchComplexSegment(complexSegment, text, values))
                {
                    return false;
                }
            }

            return true;
        }

        private bool ProcessConstraints(
            Endpoint endpoint,
            KeyValuePair<string, IRouteConstraint>[] constraints,
            HttpContext httpContext,
            RouteValueDictionary values)
        {
            for (var i = 0; i < constraints.Length; i++)
            {
                var constraint = constraints[i];
                if (!constraint.Value.Match(httpContext, NullRouter.Instance, constraint.Key, values, RouteDirection.IncomingRequest))
                {
                    return false;
                }
            }

            return true;
        }

        private async Task SelectEndpointWithPoliciesAsync(
            HttpContext httpContext,
            IEndpointSelectorPolicy[] policies,
            CandidateSet candidateSet)
        {
            for (var i = 0; i < policies.Length; i++)
            {
                var policy = policies[i];
                await policy.ApplyAsync(httpContext, candidateSet);
                if (httpContext.GetEndpoint() != null)
                {
                    return;
                }
            }

            await _selector.SelectAsync(httpContext, candidateSet);
        }
    }
}
