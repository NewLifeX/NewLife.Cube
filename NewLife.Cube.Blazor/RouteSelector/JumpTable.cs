namespace BigCookieKit.AspCore.RouteSelector
{
    internal abstract class JumpTable
    {
        public abstract int GetDestination(string path, PathSegment segment);

        public virtual string DebuggerToString()
        {
            return GetType().Name;
        }
    }
}
