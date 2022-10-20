using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Matching;
using Microsoft.AspNetCore.Routing.Patterns;

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;

namespace BigCookieKit.AspCore.RouteSelector
{
    internal class DfaMatcherBuilder
    {
        private readonly List<RouteEndpoint> _endpoints = new List<RouteEndpoint>();

        private readonly ParameterPolicyFactory _parameterPolicyFactory;
        private readonly EndpointSelector _selector;
        private readonly EndpointDataSource _endpointDataSource;
        private readonly IEndpointSelectorPolicy[] _endpointSelectorPolicies;
        private readonly INodeBuilderPolicy[] _nodeBuilders;
        private readonly EndpointComparer _comparer;

        private readonly Dictionary<string, int> _assignments;
        private readonly List<KeyValuePair<string, object>> _slots;
        private readonly List<(string parameterName, int segmentIndex, int slotIndex)> _captures;
        private readonly List<(RoutePatternPathSegment pathSegment, int segmentIndex)> _complexSegments;
        private readonly List<KeyValuePair<string, IRouteConstraint>> _constraints;

        int _stateIndex;

        public DfaMatcherBuilder(
            ParameterPolicyFactory parameterPolicyFactory,
            EndpointSelector selector,
            EndpointDataSource endpointDataSource,
            IEnumerable<MatcherPolicy> policies)
        {
            _parameterPolicyFactory = parameterPolicyFactory;
            _selector = selector;
            _endpointDataSource = endpointDataSource;
            var (nodeBuilderPolicies, endpointComparerPolicies, endpointSelectorPolicies) = ExtractPolicies(policies.OrderBy(p => p.Order));
            _endpointSelectorPolicies = endpointSelectorPolicies;
            _nodeBuilders = nodeBuilderPolicies;
            _comparer = new EndpointComparer(endpointComparerPolicies);

            _assignments = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            _slots = new List<KeyValuePair<string, object>>();
            _captures = new List<(string parameterName, int segmentIndex, int slotIndex)>();
            _complexSegments = new List<(RoutePatternPathSegment pathSegment, int segmentIndex)>();
            _constraints = new List<KeyValuePair<string, IRouteConstraint>>();
        }

        internal EndpointComparer Comparer => _comparer;

        private void AddEndpoint(RouteEndpoint endpoint)
        {
            _endpoints.Add(endpoint);
        }

        public Matcher CreateMatcher()
        {
            var endpoints = _endpointDataSource.Endpoints;
            var seenEndpointNames = new Dictionary<string, string?>();
            for (var i = 0; i < endpoints.Count; i++)
            {
                if (endpoints[i] is RouteEndpoint endpoint)
                {
                    // Validate that endpoint names are unique.
                    var endpointName = endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName;
                    if (endpointName is not null)
                    {
                        if (seenEndpointNames.TryGetValue(endpointName, out var existingEndpoint))
                        {
                            throw new InvalidOperationException($"Duplicate endpoint name '{endpointName}' found on '{endpoint.DisplayName}' and '{existingEndpoint}'. Endpoint names must be globally unique.");
                        }

                        seenEndpointNames.Add(endpointName, endpoint.DisplayName ?? endpoint.RoutePattern.RawText);
                    }

                    if (endpoint.Metadata.GetMetadata<ISuppressMatchingMetadata>()?.SuppressMatching != true)
                    {
                        AddEndpoint(endpoint);
                    }
                }
            }

            return Build();
        }

        private Matcher Build()
        {
            var root = BuildDfaTree(false);

            var stateCount = 1;
            var maxSegmentCount = 0;
            root.Visit((node) =>
            {
                stateCount++;
                maxSegmentCount = Math.Max(maxSegmentCount, node.PathDepth);
            });
            _stateIndex = 0;

            maxSegmentCount++;

            var states = new DfaState[stateCount];
            var exitDestination = stateCount - 1;
            AddNode(root, states, exitDestination);

            states[exitDestination] = new DfaState(
                Array.Empty<Candidate>(),
                Array.Empty<IEndpointSelectorPolicy>(),
                JumpTableBuilder.Build(exitDestination, exitDestination, null),
                null);

            return new DfaMatcher(_selector, states, maxSegmentCount);
        }

        private int AddNode(
            DfaNode node,
            DfaState[] states,
            int exitDestination)
        {
            node.Matches?.Sort(_comparer);

            var currentStateIndex = _stateIndex;

            var currentDefaultDestination = exitDestination;
            var currentExitDestination = exitDestination;
            (string text, int destination)[] pathEntries = null;
            PolicyJumpTableEdge[] policyEntries = null;

            if (node.Literals != null)
            {
                pathEntries = new (string text, int destination)[node.Literals.Count];

                var index = 0;
                foreach (var kvp in node.Literals)
                {
                    var transition = Transition(kvp.Value);
                    pathEntries[index++] = (kvp.Key, transition);
                }
            }

            if (node.Parameters != null &&
                node.CatchAll != null &&
                ReferenceEquals(node.Parameters, node.CatchAll))
            {
                currentExitDestination = currentDefaultDestination = Transition(node.Parameters);
            }
            else if (node.Parameters != null && node.CatchAll != null)
            {
                currentDefaultDestination = Transition(node.Parameters);
                currentExitDestination = Transition(node.CatchAll);
            }
            else if (node.Parameters != null)
            {
                currentDefaultDestination = Transition(node.Parameters);
            }
            else if (node.CatchAll != null)
            {
                currentExitDestination = currentDefaultDestination = Transition(node.CatchAll);
            }

            if (node.PolicyEdges != null && node.PolicyEdges.Count > 0)
            {
                policyEntries = new PolicyJumpTableEdge[node.PolicyEdges.Count];

                var index = 0;
                foreach (var kvp in node.PolicyEdges)
                {
                    policyEntries[index++] = new PolicyJumpTableEdge(kvp.Key, Transition(kvp.Value));
                }
            }

            var candidates = CreateCandidates(node.Matches);

            // Perf: most of the time there aren't any endpoint selector policies, create
            // this lazily.
            List<IEndpointSelectorPolicy> endpointSelectorPolicies = null;
            if (node.Matches?.Count > 0)
            {
                for (var i = 0; i < _endpointSelectorPolicies.Length; i++)
                {
                    var endpointSelectorPolicy = _endpointSelectorPolicies[i];
                    if (endpointSelectorPolicy.AppliesToEndpoints(node.Matches))
                    {
                        endpointSelectorPolicies ??= new List<IEndpointSelectorPolicy>();

                        endpointSelectorPolicies.Add(endpointSelectorPolicy);
                    }
                }
            }

            states[currentStateIndex] = new DfaState(
                candidates,
                endpointSelectorPolicies?.ToArray() ?? Array.Empty<IEndpointSelectorPolicy>(),
                JumpTableBuilder.Build(currentDefaultDestination, currentExitDestination, pathEntries),
                BuildPolicy(exitDestination, node.NodeBuilder, policyEntries));

            return currentStateIndex;

            int Transition(DfaNode next)
            {
                // Break cycles
                if (ReferenceEquals(node, next))
                {
                    return _stateIndex;
                }
                else
                {
                    _stateIndex++;
                    return AddNode(next, states, exitDestination);
                }
            }
        }

        private static PolicyJumpTable BuildPolicy(int exitDestination, INodeBuilderPolicy nodeBuilder, PolicyJumpTableEdge[] policyEntries)
        {
            if (policyEntries == null)
            {
                return null;
            }

            return nodeBuilder.BuildJumpTable(exitDestination, policyEntries);
        }

        public DfaNode BuildDfaTree(bool includeLabel = false)
        {
            var work = new List<DfaBuilderWorkerWorkItem>(_endpoints.Count);

            var root = new DfaNode() { PathDepth = 0, Label = includeLabel ? "/" : null };

            var maxDepth = 0;
            for (var i = 0; i < _endpoints.Count; i++)
            {
                var endpoint = _endpoints[i];
                var precedenceDigit = GetPrecedenceDigitAtDepth(endpoint, depth: 0);
                work.Add(new DfaBuilderWorkerWorkItem(endpoint, precedenceDigit, new List<DfaNode>() { root, }));
                maxDepth = Math.Max(maxDepth, endpoint.RoutePattern.PathSegments.Count);
            }

            var precedenceDigitComparer = Comparer<DfaBuilderWorkerWorkItem>.Create((x, y) =>
            {
                return x.PrecedenceDigit.CompareTo(y.PrecedenceDigit);
            });

            var dfaWorker = new DfaBuilderWorker(work, precedenceDigitComparer, includeLabel, _parameterPolicyFactory);

            for (var depth = 0; depth <= maxDepth; depth++)
            {
                dfaWorker.ProcessLevel(depth);
            }

            root.Visit(ApplyPolicies);

            return root;
        }

        private static int GetPrecedenceDigitAtDepth(RouteEndpoint endpoint, int depth)
        {
            var segment = GetCurrentSegment(endpoint, depth);
            if (segment is null)
            {
                // Treat "no segment" as high priority. it won't effect the algorithm, but we need to define a sort-order.
                return 0;
            }

            return ComputeInboundPrecedenceDigit(endpoint.RoutePattern, segment);
        }

        internal static int ComputeInboundPrecedenceDigit(RoutePattern routePattern, RoutePatternPathSegment pathSegment)
        {
            if (pathSegment.Parts.Count > 1)
            {
                // Multi-part segments should appear after literal segments and along with parameter segments
                return 2;
            }

            var part = pathSegment.Parts[0];
            // Literal segments always go first
            if (part.IsLiteral)
            {
                return 1;
            }
            else if (part is RoutePatternParameterPart parameterPart)
            {
                // Parameter with a required value is matched as a literal
                if (routePattern.RequiredValues.TryGetValue(parameterPart.Name, out var requiredValue) &&
                    !new RouteValueEqualityComparer().Equals(requiredValue, string.Empty))
                {
                    return 1;
                }

                var digit = parameterPart.IsCatchAll ? 5 : 3;

                // If there is a route constraint for the parameter, reduce order by 1
                // Constrained parameters end up with order 2, Constrained catch alls end up with order 4
                if (parameterPart.ParameterPolicies.Count > 0)
                {
                    digit--;
                }

                return digit;
            }
            else
            {
                // Unreachable
                throw new NotSupportedException();
            }
        }

        private static RoutePatternPathSegment GetCurrentSegment(RouteEndpoint endpoint, int depth)
        {
            if (depth < endpoint.RoutePattern.PathSegments.Count)
            {
                return endpoint.RoutePattern.PathSegments[depth];
            }

            if (endpoint.RoutePattern.PathSegments.Count == 0)
            {
                return null;
            }

            var lastSegment = endpoint.RoutePattern.PathSegments[endpoint.RoutePattern.PathSegments.Count - 1];
            if (lastSegment.IsSimple && lastSegment.Parts[0] is RoutePatternParameterPart parameterPart && parameterPart.IsCatchAll)
            {
                return lastSegment;
            }

            return null;
        }

        private void ApplyPolicies(DfaNode node)
        {
            if (node.Matches == null || node.Matches.Count == 0)
            {
                return;
            }

            var work = new List<DfaNode>() { node, };
            List<DfaNode> previousWork = null;
            for (var i = 0; i < _nodeBuilders.Length; i++)
            {
                var nodeBuilder = _nodeBuilders[i];

                List<DfaNode> nextWork;
                if (previousWork == null)
                {
                    nextWork = new List<DfaNode>();
                }
                else
                {
                    previousWork.Clear();
                    nextWork = previousWork;
                }

                for (var j = 0; j < work.Count; j++)
                {
                    var parent = work[j];
                    if (!nodeBuilder.AppliesToEndpoints(parent.Matches ?? (IReadOnlyList<Endpoint>)Array.Empty<Endpoint>()))
                    {
                        nextWork.Add(parent);
                        continue;
                    }

                    var edges = nodeBuilder.GetEdges(parent.Matches ?? (IReadOnlyList<Endpoint>)Array.Empty<Endpoint>());
                    for (var k = 0; k < edges.Count; k++)
                    {
                        var edge = edges[k];

                        var next = new DfaNode()
                        {
                            Label = (parent.Label != null) ? parent.Label + " " + edge.State.ToString() : null,
                        };

                        if (edge.Endpoints.Count > 0)
                        {
                            next.AddMatches(edge.Endpoints);
                        }
                        nextWork.Add(next);

                        parent.AddPolicyEdge(edge.State, next);
                    }

                    parent.NodeBuilder = nodeBuilder;

                    parent.Matches?.Clear();
                }

                previousWork = work;
                work = nextWork;
            }
        }

        private static bool HasAdditionalRequiredSegments(RouteEndpoint endpoint, int depth)
        {
            for (var i = depth; i < endpoint.RoutePattern.PathSegments.Count; i++)
            {
                var segment = endpoint.RoutePattern.PathSegments[i];
                if (!segment.IsSimple)
                {
                    return true;
                }

                var parameterPart = segment.Parts[0] as RoutePatternParameterPart;
                if (parameterPart == null)
                {
                    return true;
                }

                if (!parameterPart.IsOptional &&
                    !parameterPart.IsCatchAll &&
                    parameterPart.Default == null)
                {
                    return true;
                }
            }

            return false;
        }

        internal Candidate[] CreateCandidates(IReadOnlyList<Endpoint> endpoints)
        {
            if (endpoints == null || endpoints.Count == 0)
            {
                return Array.Empty<Candidate>();
            }

            var candiates = new Candidate[endpoints.Count];

            var score = 0;
            var examplar = endpoints[0];
            candiates[0] = CreateCandidate(examplar, score);

            for (var i = 1; i < endpoints.Count; i++)
            {
                var endpoint = endpoints[i];
                if (!_comparer.Equals(examplar, endpoint))
                {
                    // This endpoint doesn't have the same priority.
                    examplar = endpoint;
                    score++;
                }

                candiates[i] = CreateCandidate(endpoint, score);
            }

            return candiates;
        }

        internal Candidate CreateCandidate(Endpoint endpoint, int score)
        {
            (string parameterName, int segmentIndex, int slotIndex) catchAll = default;

            if (endpoint is RouteEndpoint routeEndpoint)
            {
                _assignments.Clear();
                _slots.Clear();
                _captures.Clear();
                _complexSegments.Clear();
                _constraints.Clear();

                foreach (var kvp in routeEndpoint.RoutePattern.Defaults)
                {
                    _assignments.Add(kvp.Key, _assignments.Count);
                    _slots.Add(kvp);
                }

                for (var i = 0; i < routeEndpoint.RoutePattern.PathSegments.Count; i++)
                {
                    var segment = routeEndpoint.RoutePattern.PathSegments[i];
                    if (!segment.IsSimple)
                    {
                        continue;
                    }

                    var parameterPart = segment.Parts[0] as RoutePatternParameterPart;
                    if (parameterPart == null)
                    {
                        continue;
                    }

                    if (!_assignments.TryGetValue(parameterPart.Name, out var slotIndex))
                    {
                        slotIndex = _assignments.Count;
                        _assignments.Add(parameterPart.Name, slotIndex);

                        // A parameter can have a required value, default value/catch all, or be a normal parameter
                        // Add the required value or default value as the slot's initial value
                        if (TryGetRequiredValue(routeEndpoint.RoutePattern, parameterPart, out var requiredValue))
                        {
                            _slots.Add(new KeyValuePair<string, object>(parameterPart.Name, requiredValue));
                        }
                        else
                        {
                            var hasDefaultValue = parameterPart.Default != null || parameterPart.IsCatchAll;
                            _slots.Add(hasDefaultValue ? new KeyValuePair<string, object>(parameterPart.Name, parameterPart.Default) : default);
                        }
                    }

                    if (TryGetRequiredValue(routeEndpoint.RoutePattern, parameterPart, out _))
                    {
                        // Don't capture a parameter if it has a required value
                        // There is no need because a parameter with a required value is matched as a literal
                    }
                    else if (parameterPart.IsCatchAll)
                    {
                        catchAll = (parameterPart.Name, i, slotIndex);
                    }
                    else
                    {
                        _captures.Add((parameterPart.Name, i, slotIndex));
                    }
                }

                for (var i = 0; i < routeEndpoint.RoutePattern.PathSegments.Count; i++)
                {
                    var segment = routeEndpoint.RoutePattern.PathSegments[i];
                    if (segment.IsSimple)
                    {
                        continue;
                    }

                    _complexSegments.Add((segment, i));
                }

                foreach (var kvp in routeEndpoint.RoutePattern.ParameterPolicies)
                {
                    var parameter = routeEndpoint.RoutePattern.GetParameter(kvp.Key); // may be null, that's ok
                    var parameterPolicyReferences = kvp.Value;
                    for (var i = 0; i < parameterPolicyReferences.Count; i++)
                    {
                        var reference = parameterPolicyReferences[i];
                        var parameterPolicy = _parameterPolicyFactory.Create(parameter, reference);
                        if (parameterPolicy is IRouteConstraint routeConstraint)
                        {
                            _constraints.Add(new KeyValuePair<string, IRouteConstraint>(kvp.Key, routeConstraint));
                        }
                    }
                }

                return new Candidate(
                    endpoint,
                    score,
                    _slots.ToArray(),
                    _captures.ToArray(),
                    catchAll,
                    _complexSegments.ToArray(),
                    _constraints.ToArray());
            }
            else
            {
                return new Candidate(
                    endpoint,
                    score,
                    Array.Empty<KeyValuePair<string, object>>(),
                    Array.Empty<(string parameterName, int segmentIndex, int slotIndex)>(),
                    catchAll,
                    Array.Empty<(RoutePatternPathSegment pathSegment, int segmentIndex)>(),
                    Array.Empty<KeyValuePair<string, IRouteConstraint>>());
            }
        }

        private static (INodeBuilderPolicy[] nodeBuilderPolicies, IEndpointComparerPolicy[] endpointComparerPolicies, IEndpointSelectorPolicy[] endpointSelectorPolicies) ExtractPolicies(IEnumerable<MatcherPolicy> policies)
        {
            var nodeBuilderPolicies = new List<INodeBuilderPolicy>();
            var endpointComparerPolicies = new List<IEndpointComparerPolicy>();
            var endpointSelectorPolicies = new List<IEndpointSelectorPolicy>();

            foreach (var policy in policies)
            {
                if (policy is INodeBuilderPolicy nodeBuilderPolicy)
                {
                    nodeBuilderPolicies.Add(nodeBuilderPolicy);
                }

                if (policy is IEndpointComparerPolicy endpointComparerPolicy)
                {
                    endpointComparerPolicies.Add(endpointComparerPolicy);
                }

                if (policy is IEndpointSelectorPolicy endpointSelectorPolicy)
                {
                    endpointSelectorPolicies.Add(endpointSelectorPolicy);
                }
            }

            return (nodeBuilderPolicies.ToArray(), endpointComparerPolicies.ToArray(), endpointSelectorPolicies.ToArray());
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

            // Find last literal segment and get its last index in the string
            var lastIndex = requestSegment.Length;

            RoutePatternParameterPart parameterNeedsValue = null; // Keeps track of a parameter segment that is pending a value
            RoutePatternPart lastLiteral = null; // Keeps track of the left-most literal we've encountered

            var outValues = new RouteValueDictionary();

            while (indexOfLastSegmentUsed >= 0)
            {
                var newLastIndex = lastIndex;

                var part = routeSegment.Parts[indexOfLastSegmentUsed];
                if (part.IsParameter)
                {
                    // Hold on to the parameter so that we can fill it in when we locate the next literal
                    parameterNeedsValue = (RoutePatternParameterPart)part;
                }
                else
                {
                    Debug.Assert(part.IsLiteral || part.IsSeparator);
                    lastLiteral = part;

                    var startIndex = lastIndex;
                    // If we have a pending parameter subsegment, we must leave at least one character for that
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

        private static void AddLiteralNode(bool includeLabel, List<DfaNode> nextParents, DfaNode parent, string literal)
        {
            if (parent.Literals == null ||
                !parent.Literals.TryGetValue(literal, out var next))
            {
                next = new DfaNode()
                {
                    PathDepth = parent.PathDepth + 1,
                    Label = includeLabel ? parent.Label + literal + "/" : null,
                };
                parent.AddLiteral(literal, next);
            }

            nextParents.Add(next);
        }

        private static bool TryGetRequiredValue(RoutePattern routePattern, RoutePatternParameterPart parameterPart, out object value)
        {
            if (!routePattern.RequiredValues.TryGetValue(parameterPart.Name, out value))
            {
                return false;
            }

            return !RouteValueEqualityComparer.Default.Equals(value, string.Empty);
        }

        public class DfaBuilderWorker
        {
            private List<DfaBuilderWorkerWorkItem> _previousWork;
            private List<DfaBuilderWorkerWorkItem> _work;
            private int _workCount;
            private readonly Comparer<DfaBuilderWorkerWorkItem> _precedenceDigitComparer;
            private readonly bool _includeLabel;
            private readonly ParameterPolicyFactory _parameterPolicyFactory;

            public DfaBuilderWorker(
                List<DfaBuilderWorkerWorkItem> work,
                Comparer<DfaBuilderWorkerWorkItem> precedenceDigitComparer,
                bool includeLabel,
                ParameterPolicyFactory parameterPolicyFactory)
            {
                _work = work;
                _previousWork = new List<DfaBuilderWorkerWorkItem>();
                _workCount = work.Count;
                _precedenceDigitComparer = precedenceDigitComparer;
                _includeLabel = includeLabel;
                _parameterPolicyFactory = parameterPolicyFactory;
            }

            internal void ProcessLevel(int depth)
            {
                var nextWork = _previousWork;
                var nextWorkCount = 0;

                _work.Sort(0, _workCount, _precedenceDigitComparer);

                for (var i = 0; i < _workCount; i++)
                {
                    var (endpoint, _, parents) = _work[i];

                    if (!HasAdditionalRequiredSegments(endpoint, depth))
                    {
                        for (var j = 0; j < parents.Count; j++)
                        {
                            var parent = parents[j];
                            parent.AddMatch(endpoint);
                        }
                    }

                    List<DfaNode> nextParents;
                    if (nextWorkCount < nextWork.Count)
                    {
                        nextParents = nextWork[nextWorkCount].Parents;
                        nextParents.Clear();

                        var nextPrecedenceDigit = GetPrecedenceDigitAtDepth(endpoint, depth + 1);
                        nextWork[nextWorkCount] = new DfaBuilderWorkerWorkItem(endpoint, nextPrecedenceDigit, nextParents);
                    }
                    else
                    {
                        nextParents = new List<DfaNode>();

                        var nextPrecedenceDigit = GetPrecedenceDigitAtDepth(endpoint, depth + 1);
                        nextWork.Add(new DfaBuilderWorkerWorkItem(endpoint, nextPrecedenceDigit, nextParents));
                    }

                    var segment = GetCurrentSegment(endpoint, depth);
                    if (segment == null)
                    {
                        continue;
                    }

                    ProcessSegment(endpoint, parents, nextParents, segment);

                    if (nextParents.Count > 0)
                    {
                        nextWorkCount++;
                    }
                }

                _previousWork = _work;
                _work = nextWork;
                _workCount = nextWorkCount;
            }

            private void ProcessSegment(
                RouteEndpoint endpoint,
                List<DfaNode> parents,
                List<DfaNode> nextParents,
                RoutePatternPathSegment segment)
            {
                for (var i = 0; i < parents.Count; i++)
                {
                    var parent = parents[i];
                    var part = segment.Parts[0];
                    var parameterPart = part as RoutePatternParameterPart;
                    if (segment.IsSimple && part is RoutePatternLiteralPart literalPart)
                    {
                        AddLiteralNode(_includeLabel, nextParents, parent, literalPart.Content);
                    }
                    else if (segment.IsSimple && parameterPart != null && parameterPart.IsCatchAll)
                    {
                        if (parent.Literals != null)
                        {
                            nextParents.AddRange(parent.Literals.Values);
                        }
                        if (parent.Parameters != null)
                        {
                            nextParents.Add(parent.Parameters);
                        }

                        if (parent.CatchAll == null)
                        {
                            parent.CatchAll = new DfaNode()
                            {
                                PathDepth = parent.PathDepth + 1,
                                Label = _includeLabel ? parent.Label + "{*...}/" : null,
                            };

                            // The catchall node just loops.
                            parent.CatchAll.Parameters = parent.CatchAll;
                            parent.CatchAll.CatchAll = parent.CatchAll;
                        }

                        parent.CatchAll.AddMatch(endpoint);
                    }
                    else if (segment.IsSimple && parameterPart != null && TryGetRequiredValue(endpoint.RoutePattern, parameterPart, out var requiredValue))
                    {
                        AddRequiredLiteralValue(endpoint, nextParents, parent, parameterPart, requiredValue);
                    }
                    else if (segment.IsSimple && parameterPart != null)
                    {
                        parent.Parameters ??= new DfaNode()
                            {
                                PathDepth = parent.PathDepth + 1,
                                Label = _includeLabel ? parent.Label + "{...}/" : null,
                            };

                        if (parent.Literals != null)
                        {
                            if (endpoint.RoutePattern.ParameterPolicies.TryGetValue(parameterPart.Name, out var parameterPolicyReferences))
                            {
                                AddParentsWithMatchingLiteralConstraints(nextParents, parent, parameterPart, parameterPolicyReferences);
                            }
                            else
                            {
                                nextParents.AddRange(parent.Literals.Values);
                            }
                        }

                        nextParents.Add(parent.Parameters);
                    }
                    else
                    {
                        parent.Parameters ??= new DfaNode()
                            {
                                PathDepth = parent.PathDepth + 1,
                                Label = _includeLabel ? parent.Label + "{...}/" : null,
                            };

                        if (parent.Literals != null)
                        {
                            AddParentsMatchingComplexSegment(endpoint, nextParents, segment, parent, parameterPart);
                        }
                        nextParents.Add(parent.Parameters);
                    }
                }
            }

            private void AddParentsMatchingComplexSegment(RouteEndpoint endpoint, List<DfaNode> nextParents, RoutePatternPathSegment segment, DfaNode parent, RoutePatternParameterPart parameterPart)
            {
                var routeValues = new RouteValueDictionary();
                foreach (var literal in parent.Literals.Keys)
                {
                    if (MatchComplexSegment(segment, literal, routeValues))
                    {
                        var passedAllPolicies = true;
                        for (var i = 0; i < segment.Parts.Count; i++)
                        {
                            var segmentPart = segment.Parts[i];
                            if (segmentPart is not RoutePatternParameterPart partParameter)
                            {
                                continue;
                            }

                            if (!routeValues.TryGetValue(partParameter.Name, out var parameterValue))
                            {
                                continue;
                            }

                            if (endpoint.RoutePattern.ParameterPolicies.TryGetValue(partParameter.Name, out var parameterPolicyReferences))
                            {
                                for (var j = 0; j < parameterPolicyReferences.Count; j++)
                                {
                                    var reference = parameterPolicyReferences[j];
                                    var parameterPolicy = _parameterPolicyFactory.Create(parameterPart, reference);
                                    if (parameterPolicy is JumpTableBuilder.IParameterLiteralNodeMatchingPolicy constraint && !constraint.MatchesLiteral(partParameter.Name, (string)parameterValue))
                                    {
                                        passedAllPolicies = false;
                                        break;
                                    }
                                }
                            }
                        }

                        if (passedAllPolicies)
                        {
                            nextParents.Add(parent.Literals[literal]);
                        }
                    }

                    routeValues.Clear();
                }
            }

            private void AddParentsWithMatchingLiteralConstraints(List<DfaNode> nextParents, DfaNode parent, RoutePatternParameterPart parameterPart, IReadOnlyList<RoutePatternParameterPolicyReference> parameterPolicyReferences)
            {
                // The list of parameters that fail to meet at least one IParameterLiteralNodeMatchingPolicy.
                var hasFailingPolicy = parent.Literals.Keys.Count < 32 ?
                    (stackalloc bool[32]).Slice(0, parent.Literals.Keys.Count) :
                    new bool[parent.Literals.Keys.Count];

                // Whether or not all parameters have failed to meet at least one constraint.
                for (var i = 0; i < parameterPolicyReferences.Count; i++)
                {
                    var reference = parameterPolicyReferences[i];
                    var parameterPolicy = _parameterPolicyFactory.Create(parameterPart, reference);
                    if (parameterPolicy is JumpTableBuilder.IParameterLiteralNodeMatchingPolicy constraint)
                    {
                        var literalIndex = 0;
                        var allFailed = true;
                        foreach (var literal in parent.Literals.Keys)
                        {
                            if (!hasFailingPolicy[literalIndex] && !constraint.MatchesLiteral(parameterPart.Name, literal))
                            {
                                hasFailingPolicy[literalIndex] = true;
                            }

                            allFailed &= hasFailingPolicy[literalIndex];

                            literalIndex++;
                        }

                        if (allFailed)
                        {
                            // If we get here it means that all literals have failed at least one policy, which means we can skip checking policies
                            // and return early. This will be a very common case when your constraints are things like "int,length or a regex".
                            return;
                        }
                    }
                }

                var k = 0;
                foreach (var literal in parent.Literals.Values)
                {
                    if (!hasFailingPolicy[k])
                    {
                        nextParents.Add(literal);
                    }
                    k++;
                }
            }

            private void AddRequiredLiteralValue(RouteEndpoint endpoint, List<DfaNode> nextParents, DfaNode parent, RoutePatternParameterPart parameterPart, object requiredValue)
            {
                if (endpoint.RoutePattern.ParameterPolicies.TryGetValue(parameterPart.Name, out var parameterPolicyReferences))
                {
                    for (var k = 0; k < parameterPolicyReferences.Count; k++)
                    {
                        var reference = parameterPolicyReferences[k];
                        var parameterPolicy = _parameterPolicyFactory.Create(parameterPart, reference);
                        if (parameterPolicy is IOutboundParameterTransformer parameterTransformer)
                        {
                            requiredValue = parameterTransformer.TransformOutbound(requiredValue);
                            break;
                        }
                    }
                }

                var literalValue = requiredValue?.ToString() ?? throw new InvalidOperationException($"Required value for literal '{parameterPart.Name}' must evaluate to a non-null string.");

                AddLiteralNode(_includeLabel, nextParents, parent, literalValue);
            }
        }
    }
}
