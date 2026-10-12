using Jobsy.Core.Options;
using Jobsy.Infrastructure.Security;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Jobsy.Tests;

public class FieldEncryptionServiceTests
{
    private static FieldEncryptionService CreateService(bool enabled = true)
    {
        var key = Convert.ToBase64String(new byte[32]);
        var options = Options.Create(new FieldEncryptionOptions
        {
            ActiveKeyId = enabled ? "v1" : null,
            Keys = enabled ? new Dictionary<string, string> { ["v1"] = key } : new()
        });
        var env = new HostEnvironmentStub();
        return new FieldEncryptionService(options, env);
    }

    [Fact]
    public void Roundtrip_string_and_bytes()
    {
        var svc = CreateService();
        var enc = svc.EncryptString("secret@example.com");
        Assert.NotNull(enc);
        Assert.True(svc.IsEncrypted(enc));
        Assert.Equal("secret@example.com", svc.DecryptString(enc));

        var bytes = svc.EncryptBytes([1, 2, 3, 4]);
        Assert.Equal([1, 2, 3, 4], svc.DecryptBytes(bytes));
    }

    [Fact]
    public void Legacy_plaintext_still_readable()
    {
        var svc = CreateService();
        Assert.Equal("plain", svc.DecryptString("plain"));
    }

    private sealed class HostEnvironmentStub : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Jobsy.Tests";
        public string ContentRootPath { get; set; } = "/";
        public IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}
