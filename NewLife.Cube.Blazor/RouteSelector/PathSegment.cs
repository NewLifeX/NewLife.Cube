
using System;

namespace BigCookieKit.AspCore.RouteSelector
{
    internal readonly struct PathSegment : IEquatable<PathSegment>
    {
        public readonly int Start;
        public readonly int Length;

        public PathSegment(int start, int length)
        {
            Start = start;
            Length = length;
        }

        public override bool Equals(object? obj)
        {
            return obj is PathSegment segment ? Equals(segment) : false;
        }

        public bool Equals(PathSegment other)
        {
            return Start == other.Start && Length == other.Length;
        }

        public override int GetHashCode()
        {
            return Start;
        }

        public override string ToString()
        {
            return $"Segment({Start}:{Length})";
        }
    }
}
