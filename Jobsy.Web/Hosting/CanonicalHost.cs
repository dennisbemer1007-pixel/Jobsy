namespace Jobsy.Web.Hosting;

public static class CanonicalHost
{
    public static bool TryStripWww(string host, out string canonical)
    {
        if (host.StartsWith("www.", StringComparison.OrdinalIgnoreCase) && host.Length > 4)
        {
            canonical = host[4..];
            return true;
        }

        canonical = host;
        return false;
    }

    public static bool IsLoopback(string host)
        => host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
           || host.StartsWith("127.", StringComparison.Ordinal)
           || host.Equals("::1", StringComparison.Ordinal);
}
