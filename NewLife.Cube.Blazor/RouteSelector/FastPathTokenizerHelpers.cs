using System;

namespace BigCookieKit.AspCore.RouteSelector
{
    internal static class FastPathTokenizerHelpers
    {
        public static int Tokenize(string path, Span<PathSegment> segments)
        {
            if (string.IsNullOrEmpty(path))
            {
                return 0;
            }

            int count = 0;
            int start = 1;
            int end;
            var span = path.AsSpan(start);
            while ((end = span.IndexOf('/')) >= 0 && count < segments.Length)
            {
                segments[count++] = new PathSegment(start, end);
                start += end + 1;
                span = path.AsSpan(start);
            }

            var length = span.Length;
            if (length > 0 && count < segments.Length)
            {
                segments[count++] = new PathSegment(start, length);
            }

            return count;
        }
    }
}