using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;

using System;
using System.Collections.Generic;

namespace BigCookieKit.AspCore.RouteSelector
{
    internal readonly struct Candidate
    {
        public readonly Endpoint Endpoint;

        public readonly CandidateFlags Flags;

        public readonly KeyValuePair<string, object>[] Slots;

        public readonly (string parameterName, int segmentIndex, int slotIndex)[] Captures;

        public readonly (string parameterName, int segmentIndex, int slotIndex) CatchAll;

        public readonly (RoutePatternPathSegment pathSegment, int segmentIndex)[] ComplexSegments;

        public readonly KeyValuePair<string, IRouteConstraint>[] Constraints;

        public readonly int Score;

        public Candidate(Endpoint endpoint)
        {
            Endpoint = endpoint;

            Slots = Array.Empty<KeyValuePair<string, object>>();
            Captures = Array.Empty<(string parameterName, int segmentIndex, int slotIndex)>();
            CatchAll = default;
            ComplexSegments = Array.Empty<(RoutePatternPathSegment pathSegment, int segmentIndex)>();
            Constraints = Array.Empty<KeyValuePair<string, IRouteConstraint>>();
            Score = 0;

            Flags = CandidateFlags.None;
        }

        public Candidate(
            Endpoint endpoint,
            int score,
            KeyValuePair<string, object>[] slots,
            (string parameterName, int segmentIndex, int slotIndex)[] captures,
            in (string parameterName, int segmentIndex, int slotIndex) catchAll,
            (RoutePatternPathSegment pathSegment, int segmentIndex)[] complexSegments,
            KeyValuePair<string, IRouteConstraint>[] constraints)
        {
            Endpoint = endpoint;
            Score = score;
            Slots = slots;
            Captures = captures;
            CatchAll = catchAll;
            ComplexSegments = complexSegments;
            Constraints = constraints;

            Flags = CandidateFlags.None;
            for (var i = 0; i < slots.Length; i++)
            {
                if (slots[i].Key != null)
                {
                    Flags |= CandidateFlags.HasDefaults;
                }
            }

            if (captures.Length > 0)
            {
                Flags |= CandidateFlags.HasCaptures;
            }

            if (catchAll.parameterName != null)
            {
                Flags |= CandidateFlags.HasCatchAll;
            }

            if (complexSegments.Length > 0)
            {
                Flags |= CandidateFlags.HasComplexSegments;
            }

            if (constraints.Length > 0)
            {
                Flags |= CandidateFlags.HasConstraints;
            }
        }

        [Flags]
        public enum CandidateFlags
        {
            None = 0,
            HasDefaults = 1,
            HasCaptures = 2,
            HasCatchAll = 4,
            HasSlots = HasDefaults | HasCaptures | HasCatchAll,
            HasComplexSegments = 8,
            HasConstraints = 16,
        }
    }
}
