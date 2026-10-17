using Microsoft.AspNetCore.Mvc.Testing;

namespace Jobsy.Tests.TestSupport;

/// <summary>
/// xUnit fixture teardown calls <see cref="IDisposable.Dispose"/> unless the fixture implements
/// <see cref="IAsyncLifetime"/>; host shutdown inside <see cref="WebApplicationFactory{TEntryPoint}.Dispose"/>
/// uses sync-over-async and can deadlock under parallel tests (dotnet/runtime#39911).
/// </summary>
public abstract class ApiWebApplicationFactory : WebApplicationFactory<Jobsy.Api.ApiAssemblyMarker>, IAsyncLifetime
{
    private int _disposed;

    public virtual ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public new ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return base.DisposeAsync();
    }

    public new void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
        {
            return;
        }

        Task.Run(async () => await base.DisposeAsync().ConfigureAwait(false)).GetAwaiter().GetResult();
    }
}

public abstract class WebWebApplicationFactory : WebApplicationFactory<Jobsy.Web.WebAssemblyMarker>, IAsyncLifetime
{
    private int _disposed;

    public virtual ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public new ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return base.DisposeAsync();
    }

    public new void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
        {
            return;
        }

        Task.Run(async () => await base.DisposeAsync().ConfigureAwait(false)).GetAwaiter().GetResult();
    }
}
