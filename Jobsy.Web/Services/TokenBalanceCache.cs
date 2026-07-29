namespace Jobsy.Web.Services;

/// <summary>
/// Circuit-scoped cache so the shell token chip does not re-hit the balances API on remount.
/// </summary>
public sealed class TokenBalanceCache
{
    private decimal? _total;
    private bool _loaded;

    public async Task<decimal?> GetOrLoadAsync(Func<Task<decimal?>> loader)
    {
        if (_loaded)
        {
            return _total;
        }

        _total = await loader();
        _loaded = true;
        return _total;
    }

    public void Invalidate()
    {
        _loaded = false;
        _total = null;
    }
}
