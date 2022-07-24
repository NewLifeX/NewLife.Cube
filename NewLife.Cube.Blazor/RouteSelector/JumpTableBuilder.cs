using Microsoft.AspNetCore.Routing;

using System;

namespace BigCookieKit.AspCore.RouteSelector
{
    internal static class JumpTableBuilder
    {
        public const int InvalidDestination = -1;

        public static JumpTable Build(int defaultDestination, int exitDestination, (string text, int destination)[] pathEntries)
        {
            if (defaultDestination == InvalidDestination)
            {
                var message = $"{nameof(defaultDestination)} is not set. Please report this as a bug.";
                throw new InvalidOperationException(message);
            }

            if (exitDestination == InvalidDestination)
            {
                var message = $"{nameof(exitDestination)} is not set. Please report this as a bug.";
                throw new InvalidOperationException(message);
            }

            if (pathEntries == null || pathEntries.Length == 0)
            {
                return new ZeroEntryJumpTable(defaultDestination, exitDestination);
            }

            if (pathEntries.Length == 1 && Ascii.IsAscii(pathEntries[0].text))
            {
                var entry = pathEntries[0];
                return new SingleEntryAsciiJumpTable(defaultDestination, exitDestination, entry.text, entry.destination);
            }

            // We have a fallback that works for non-ASCII
            if (pathEntries.Length == 1)
            {
                var entry = pathEntries[0];
                return new SingleEntryJumpTable(defaultDestination, exitDestination, entry.text, entry.destination);
            }

            var threshold = IntPtr.Size == 8 ? 100 : 50;
            if (pathEntries.Length >= threshold)
            {
                return new DictionaryJumpTable(defaultDestination, exitDestination, pathEntries);
            }

            JumpTable fallback;

            if (pathEntries.Length <= 10)
            {
                fallback = new LinearSearchJumpTable(defaultDestination, exitDestination, pathEntries);
            }
            else
            {
                fallback = new DictionaryJumpTable(defaultDestination, exitDestination, pathEntries);
            }

            return fallback;
        }

        public interface IParameterLiteralNodeMatchingPolicy : IParameterPolicy
        {
            bool MatchesLiteral(string parameterName, string literal);
        }
    }
}
