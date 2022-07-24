using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;

namespace BigCookieKit.AspCore.RouteSelector
{
    internal sealed class CandidateSet
    {
        internal CandidateState[] Candidates;

        public CandidateSet(Endpoint[] endpoints, RouteValueDictionary[] values, int[] scores)
        {
            if (endpoints == null)
            {
                throw new ArgumentNullException(nameof(endpoints));
            }

            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            if (scores == null)
            {
                throw new ArgumentNullException(nameof(scores));
            }

            if (endpoints.Length != values.Length || endpoints.Length != scores.Length)
            {
                throw new ArgumentException($"The provided {nameof(endpoints)}, {nameof(values)}, and {nameof(scores)} must have the same length.");
            }

            Candidates = new CandidateState[endpoints.Length];
            for (var i = 0; i < endpoints.Length; i++)
            {
                Candidates[i] = new CandidateState(endpoints[i], values[i], scores[i]);
            }
        }

        internal CandidateSet(Candidate[] candidates)
        {
            Candidates = new CandidateState[candidates.Length];
            for (var i = 0; i < candidates.Length; i++)
            {
                Candidates[i] = new CandidateState(candidates[i].Endpoint, candidates[i].Score);
            }
        }

        internal CandidateSet(CandidateState[] candidates)
        {
            Candidates = candidates;
        }

        public int Count => Candidates.Length;

        public ref CandidateState this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if ((uint)index >= Count)
                {
                    ThrowIndexArgumentOutOfRangeException();
                }

                return ref Candidates[index];
            }
        }

        public bool IsValidCandidate(int index)
        {
            if ((uint)index >= Count)
            {
                ThrowIndexArgumentOutOfRangeException();
            }

            return IsValidCandidate(ref Candidates[index]);
        }

        internal static bool IsValidCandidate(ref CandidateState candidate)
        {
            return candidate.Score >= 0;
        }

        public void SetValidity(int index, bool value)
        {
            // Friendliness for inlining
            if ((uint)index >= Count)
            {
                ThrowIndexArgumentOutOfRangeException();
            }

            ref var original = ref Candidates[index];
            SetValidity(ref original, value);
        }

        internal static void SetValidity(ref CandidateState candidate, bool value)
        {
            var originalScore = candidate.Score;
            var score = originalScore >= 0 ^ value ? ~originalScore : originalScore;
            candidate = new CandidateState(candidate.Endpoint, candidate.Values, score);
        }

        public void ReplaceEndpoint(int index, Endpoint? endpoint, RouteValueDictionary? values)
        {
            // Friendliness for inlining
            if ((uint)index >= Count)
            {
                ThrowIndexArgumentOutOfRangeException();
            }

            Candidates[index] = new CandidateState(endpoint!, values, Candidates[index].Score);

            if (endpoint == null)
            {
                SetValidity(index, false);
            }
        }

        public void ExpandEndpoint(int index, IReadOnlyList<Endpoint> endpoints, IComparer<Endpoint> comparer)
        {
            // Friendliness for inlining
            if ((uint)index >= Count)
            {
                ThrowIndexArgumentOutOfRangeException();
            }

            if (endpoints == null)
            {
                ThrowArgumentNullException(nameof(endpoints));
            }

            if (comparer == null)
            {
                ThrowArgumentNullException(nameof(comparer));
            }

            ValidateUniqueScore(index);

            switch (endpoints.Count)
            {
                case 0:
                    ReplaceEndpoint(index, null, null);
                    break;

                case 1:
                    ReplaceEndpoint(index, endpoints[0], Candidates[index].Values);
                    break;

                default:

                    var score = GetOriginalScore(index);
                    var values = Candidates[index].Values;

                    var original = Candidates;
                    var candidates = new CandidateState[original.Length - 1 + endpoints.Count];
                    Candidates = candidates;

                    for (var i = 0; i < index; i++)
                    {
                        candidates[i] = original[i];
                    }

                    var buffer = endpoints.ToArray();
                    Array.Sort<Endpoint>(buffer, comparer);

                    // Add the first new endpoint with the current score
                    candidates[index] = new CandidateState(buffer[0], values, score);

                    var scoreOffset = 0;
                    for (var i = 1; i < buffer.Length; i++)
                    {
                        var cmp = comparer.Compare(buffer[i - 1], buffer[i]);

                        // This should not be possible. This would mean that sorting is wrong.
                        Debug.Assert(cmp <= 0);
                        if (cmp == 0)
                        {
                            // Score is unchanged.
                        }
                        else if (cmp < 0)
                        {
                            // Endpoint is lower priority, higher score.
                            scoreOffset++;
                        }

                        Candidates[i + index] = new CandidateState(buffer[i], values, score + scoreOffset);
                    }

                    for (var i = index + 1; i < original.Length; i++)
                    {
                        Candidates[i + endpoints.Count - 1] = new CandidateState(original[i].Endpoint, original[i].Values, original[i].Score + scoreOffset);
                    }

                    break;
            }
        }

        private int GetOriginalScore(int index)
        {
            var score = Candidates[index].Score;
            return score >= 0 ? score : ~score;
        }

        private void ValidateUniqueScore(int index)
        {
            var score = GetOriginalScore(index);

            var count = 0;
            var candidates = Candidates;
            for (var i = 0; i < candidates.Length; i++)
            {
                if (GetOriginalScore(i) == score)
                {
                    count++;
                }
            }

            Debug.Assert(count > 0);
            if (count > 1)
            {
                var duplicates = new List<Endpoint>();
                for (var i = 0; i < candidates.Length; i++)
                {
                    if (GetOriginalScore(i) == score)
                    {
                        duplicates.Add(candidates[i].Endpoint!);
                    }
                }

                var message =
                    $"Using {nameof(ExpandEndpoint)} requires that the replaced endpoint have a unique priority. " +
                    $"The following endpoints were found with the same priority:" + Environment.NewLine +
                    string.Join(Environment.NewLine, duplicates.Select(e => e.DisplayName));
                throw new InvalidOperationException(message);
            }
        }

        [DoesNotReturn]
        private static void ThrowIndexArgumentOutOfRangeException()
        {
            throw new ArgumentOutOfRangeException("index");
        }

        [DoesNotReturn]
        private static void ThrowArgumentNullException(string parameter)
        {
            throw new ArgumentNullException(parameter);
        }
    }
}
