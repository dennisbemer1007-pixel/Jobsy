using Jobsy.Web.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Jobsy.Tests;

public class EmployersSwitchTests
{
    [Fact]
    public async Task AlwaysOn_returns_enabled_On_variant()
    {
        var sw = new AlwaysOnEmployersSwitch();
        Assert.True(await sw.IsEnabledAsync());
        Assert.Equal(LandingVariant.On, await sw.VariantAsync());
    }

    [Fact]
    public async Task ForceVariant_honoured_in_Development_only()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Landing:ForceVariant"] = "zw" })
            .Build();

        var dev = new LandingVariantResolver(
            new AlwaysOnEmployersSwitch(),
            new HttpContextAccessor { HttpContext = new DefaultHttpContext() },
            new FakeEnv(Environments.Development),
            config);
        Assert.Equal(LandingVariant.Zw, await dev.GetAsync());

        var prod = new LandingVariantResolver(
            new AlwaysOnEmployersSwitch(),
            new HttpContextAccessor { HttpContext = new DefaultHttpContext() },
            new FakeEnv(Environments.Production),
            config);
        Assert.Equal(LandingVariant.On, await prod.GetAsync());
    }

    [Fact]
    public async Task Query_variant_ignored_in_Staging()
    {
        var http = new DefaultHttpContext();
        http.Request.QueryString = new QueryString("?_variant=zw");
        var resolver = new LandingVariantResolver(
            new AlwaysOnEmployersSwitch(),
            new HttpContextAccessor { HttpContext = http },
            new FakeEnv(Environments.Staging),
            new ConfigurationBuilder().Build());
        Assert.Equal(LandingVariant.On, await resolver.GetAsync());
    }

    [Fact]
    public async Task Test_double_can_force_off()
    {
        var off = new FixedEmployersSwitch(false);
        Assert.False(await off.IsEnabledAsync());
        Assert.Equal(LandingVariant.Zw, await off.VariantAsync());
        Assert.False(await EmployersGate.IsEnabledAsync(off));
    }

    private sealed class FixedEmployersSwitch(bool enabled) : IEmployersSwitch
    {
        public ValueTask<bool> IsEnabledAsync(CancellationToken ct = default)
            => ValueTask.FromResult(enabled);

        public ValueTask<LandingVariant> VariantAsync(CancellationToken ct = default)
            => ValueTask.FromResult(enabled ? LandingVariant.On : LandingVariant.Zw);
    }

    private sealed class FakeEnv(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "Jobsy.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
