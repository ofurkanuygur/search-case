using Hangfire.Dashboard;

namespace SearchCase.HangfireWorker.Configuration;

/// <summary>
/// Development-only authorization filter that allows all access to Hangfire Dashboard.
/// WARNING: Do NOT use in production! Implement proper authentication for production.
/// </summary>
public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    private readonly bool _allowAnonymous;

    public HangfireDashboardAuthorizationFilter(bool allowAnonymous = false)
    {
        _allowAnonymous = allowAnonymous;
    }

    public bool Authorize(DashboardContext context)
    {
        // In development, allow all access
        // In production, this should check for proper authentication
        return _allowAnonymous;
    }
}
