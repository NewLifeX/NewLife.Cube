namespace BigCookieKit.AspCore.RouteSelector
{
    internal class ZeroEntryJumpTable : JumpTable
    {
        private readonly int _defaultDestination;
        private readonly int _exitDestination;

        public ZeroEntryJumpTable(int defaultDestination, int exitDestination)
        {
            _defaultDestination = defaultDestination;
            _exitDestination = exitDestination;
        }

        public override int GetDestination(string path, PathSegment segment)
        {
            return segment.Length == 0 ? _exitDestination : _defaultDestination;
        }

        public override string DebuggerToString()
        {
            return $"{{ $+: {_defaultDestination}, $0: {_exitDestination} }}";
        }
    }
}
