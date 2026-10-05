using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Options;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jobsy.Tests;

public class CvUploadHardeningTests
{
    private const string SecretMarker = "LOBSYCVSECRETMARKER";

    [Fact]
    public void Magic_bytes_accept_pdf_and_docx()
    {
        var pdf = MinimalPdf();
        Assert.True(CandidateCvFileRules.TryValidateBytes(pdf, CandidateCvFileRules.PdfContentType, out var pdfError));
        Assert.Null(pdfError);

        var docx = MinimalDocx();
        Assert.True(CandidateCvFileRules.TryValidateBytes(docx, CandidateCvFileRules.DocxContentType, out var docxError));
        Assert.Null(docxError);
    }

    [Fact]
    public void Magic_bytes_reject_exe_renamed_as_pdf()
    {
        Assert.True(CandidateCvFileRules.TryNormalize(
            "cv.exe",
            CandidateCvFileRules.PdfContentType,
            32,
            out _,
            out var type,
            out var nameError));
        Assert.Null(nameError);
        Assert.Equal(CandidateCvFileRules.PdfContentType, type);

        var exe = new byte[] { (byte)'M', (byte)'Z', 0x90, 0x00, 0x03, 0x00, 0x00, 0x00 };
        Assert.False(CandidateCvFileRules.TryValidateBytes(exe, type, out var error));
        Assert.Equal(CandidateCvFileRules.NotPdfMessage, error);
    }

    [Fact]
    public void Magic_bytes_reject_empty_truncated_and_wrong_magic()
    {
        Assert.False(CandidateCvFileRules.TryValidateBytes([], CandidateCvFileRules.PdfContentType, out var empty));
        Assert.Equal(CandidateCvFileRules.EmptyMessage, empty);

        var huge = new byte[CandidateCvFileRules.MaxBytes + 1];
        huge[0] = (byte)'%';
        Assert.False(CandidateCvFileRules.TryValidateBytes(huge, CandidateCvFileRules.PdfContentType, out var size));
        Assert.Contains("5 MB", size);

        var truncatedPdf = "%PDF-1.4\n1 0 obj<<>>endobj\n"u8.ToArray();
        Assert.False(CandidateCvFileRules.TryValidateBytes(truncatedPdf, CandidateCvFileRules.PdfContentType, out var cut));
        Assert.Equal(CandidateCvFileRules.PdfTruncatedMessage, cut);

        Assert.False(CandidateCvFileRules.TryValidateBytes(MinimalPdf(), CandidateCvFileRules.DocxContentType, out var pdfAsDocx));
        Assert.Equal(CandidateCvFileRules.NotDocxMessage, pdfAsDocx);

        Assert.False(CandidateCvFileRules.TryValidateBytes(MinimalDocx(), CandidateCvFileRules.PdfContentType, out var docxAsPdf));
        Assert.Equal(CandidateCvFileRules.NotPdfMessage, docxAsPdf);

        var zip = ZipWith("hello.txt", "geen-word"u8.ToArray());
        Assert.False(CandidateCvFileRules.TryValidateBytes(zip, CandidateCvFileRules.DocxContentType, out var notWord));
        Assert.Equal(CandidateCvFileRules.NotValidDocxMessage, notWord);

        var brokenZip = new byte[] { 0x50, 0x4B, 0x03, 0x04, 0x00, 0x00 };
        Assert.False(CandidateCvFileRules.TryValidateBytes(brokenZip, CandidateCvFileRules.DocxContentType, out var broken));
        Assert.Equal(CandidateCvFileRules.DocxTruncatedMessage, broken);
    }

    [Fact]
    public void Scan_policy_rejects_infected_and_honours_fail_closed()
    {
        Assert.False(UploadScanRules.TryAccept(
            new UploadScanResult(UploadMalwareVerdict.Infected, "Eicar-Test-Signature"),
            failClosed: false,
            out var infected));
        Assert.Equal(UploadScanRules.InfectedMessage, infected);

        Assert.False(UploadScanRules.TryAccept(UploadScanResult.Unavailable, failClosed: true, out var closed));
        Assert.Equal(UploadScanRules.UnavailableMessage, closed);

        Assert.True(UploadScanRules.TryAccept(UploadScanResult.Unavailable, failClosed: false, out var open));
        Assert.Null(open);
        Assert.True(UploadScanRules.TryAccept(UploadScanResult.Clean, failClosed: true, out _));
    }

    [Fact]
    public void Fail_closed_defaults_outside_development()
    {
        var options = new UploadScanOptions();
        Assert.False(options.RejectWhenUnavailable(isDevelopment: true));
        Assert.True(options.RejectWhenUnavailable(isDevelopment: false));

        options.FailClosed = false;
        Assert.False(options.RejectWhenUnavailable(isDevelopment: false));
        options.FailClosed = true;
        Assert.True(options.RejectWhenUnavailable(isDevelopment: true));
    }

    [Fact]
    public async Task Disabled_scanner_does_not_contact_endpoint()
    {
        using var scope = CreateScanner(enabled: false, endpoint: "tcp://127.0.0.1:1", timeoutSeconds: 1);
        var started = DateTime.UtcNow;
        var result = await scope.Scanner.ScanAsync(Encoding.ASCII.GetBytes(SecretMarker));
        Assert.Equal(UploadMalwareVerdict.Clean, result.Verdict);
        Assert.True(DateTime.UtcNow - started < TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Clamd_found_is_infected_without_logging_file_bytes()
    {
        var payload = Encoding.ASCII.GetBytes("%PDF-1.4\n" + SecretMarker + "\n%%EOF\n");
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var served = ServeClamdAsync(listener, "stream: Eicar-Test-Signature FOUND\n");

        var logs = new ListLogger<UploadMalwareScanner>();
        using var scope = CreateScanner(enabled: true, endpoint: $"tcp://127.0.0.1:{port}", timeoutSeconds: 5, logs);
        var result = await scope.Scanner.ScanAsync(payload);
        await served;

        Assert.Equal(UploadMalwareVerdict.Infected, result.Verdict);
        Assert.Equal("Eicar-Test-Signature", result.ThreatName);
        Assert.DoesNotContain(SecretMarker, result.ThreatName);
        Assert.DoesNotContain(logs.Lines, line => line.Contains(SecretMarker, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Clamd_ok_is_clean_and_closed_port_is_unavailable()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var served = ServeClamdAsync(listener, "stream: OK\n");
        using (var cleanScope = CreateScanner(enabled: true, endpoint: $"tcp://127.0.0.1:{port}", timeoutSeconds: 5))
        {
            var ok = await cleanScope.Scanner.ScanAsync(MinimalPdf());
            await served;
            Assert.Equal(UploadMalwareVerdict.Clean, ok.Verdict);
        }

        listener.Stop();
        using var downScope = CreateScanner(enabled: true, endpoint: $"tcp://127.0.0.1:{port}", timeoutSeconds: 2);
        var unavailable = await downScope.Scanner.ScanAsync(MinimalPdf());
        Assert.Equal(UploadMalwareVerdict.Unavailable, unavailable.Verdict);
    }

    [Fact]
    public async Task Http_rest_infected_json_does_not_echo_file_bytes()
    {
        var payload = Encoding.ASCII.GetBytes(SecretMarker);
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """{"success":true,"data":{"result":[{"name":"upload.bin","is_infected":true,"viruses":["Win.Test.EICAR_HDB-1"]}]}}""",
                Encoding.UTF8,
                "application/json")
        });
        var logs = new ListLogger<UploadMalwareScanner>();
        using var scope = CreateScanner(
            enabled: true,
            endpoint: "http://clamav.internal/api/v1/scan",
            timeoutSeconds: 5,
            logs,
            handler);
        var result = await scope.Scanner.ScanAsync(payload);
        Assert.Equal(UploadMalwareVerdict.Infected, result.Verdict);
        Assert.Equal("Win.Test.EICAR_HDB-1", result.ThreatName);
        Assert.DoesNotContain(logs.Lines, line => line.Contains(SecretMarker, StringComparison.Ordinal));
        Assert.Equal(1, handler.Calls);
    }

    [Theory]
    [InlineData("stream: OK", UploadMalwareVerdict.Clean, null)]
    [InlineData("stream: Eicar-Test-Signature FOUND", UploadMalwareVerdict.Infected, "Eicar-Test-Signature")]
    [InlineData("Everything ok : false", UploadMalwareVerdict.Infected, "FOUND")]
    [InlineData("Everything ok : true", UploadMalwareVerdict.Clean, null)]
    [InlineData("INSTREAM size limit exceeded. ERROR", UploadMalwareVerdict.Unavailable, null)]
    public void Plain_clamd_replies_map_to_verdicts(string body, UploadMalwareVerdict verdict, string? threat)
    {
        var parsed = ClamAvScanProtocol.ParseBody(body);
        Assert.Equal(verdict, parsed.Verdict);
        Assert.Equal(threat, parsed.ThreatName);
    }

    private static ScannerScope CreateScanner(
        bool enabled,
        string endpoint,
        int timeoutSeconds,
        ILogger<UploadMalwareScanner>? logger = null,
        HttpMessageHandler? handler = null)
    {
        var options = Options.Create(new UploadScanOptions
        {
            Enabled = enabled,
            Endpoint = endpoint,
            TimeoutSeconds = timeoutSeconds,
            MaxBytes = CandidateCvFileRules.MaxBytes
        });
        return new ScannerScope(options, handler, logger ?? new ListLogger<UploadMalwareScanner>());
    }

    private static async Task ServeClamdAsync(TcpListener listener, string reply)
    {
        using var client = await listener.AcceptTcpClientAsync();
        await using var stream = client.GetStream();
        var buffer = new byte[8192];
        var acc = new MemoryStream();
        while (true)
        {
            var read = await stream.ReadAsync(buffer, CancellationToken.None);
            if (read == 0)
            {
                break;
            }

            acc.Write(buffer, 0, read);
            if (EndsWithZeroChunk(acc.ToArray()))
            {
                break;
            }
        }

        var bytes = Encoding.ASCII.GetBytes(reply);
        await stream.WriteAsync(bytes, CancellationToken.None);
    }

    private static bool EndsWithZeroChunk(byte[] data)
    {
        if (data.Length < 4)
        {
            return false;
        }

        return data[^4] == 0 && data[^3] == 0 && data[^2] == 0 && data[^1] == 0;
    }

    private static byte[] MinimalPdf()
        => Encoding.ASCII.GetBytes("%PDF-1.4\n1 0 obj<<>>endobj\ntrailer<<>>\n%%EOF\n");

    private static byte[] MinimalDocx()
    {
        var types = """
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
              <Override PartName="/word/document.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"/>
            </Types>
            """u8.ToArray();
        var document = """
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"><w:body><w:p><w:r><w:t>Test</w:t></w:r></w:p></w:body></w:document>
            """u8.ToArray();
        using var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteEntry(zip, "[Content_Types].xml", types);
            WriteEntry(zip, "word/document.xml", document);
        }

        return stream.ToArray();
    }

    private static byte[] ZipWith(string name, byte[] content)
    {
        using var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteEntry(zip, name, content);
        }

        return stream.ToArray();
    }

    private static void WriteEntry(ZipArchive zip, string name, byte[] content)
    {
        var entry = zip.CreateEntry(name);
        using var entryStream = entry.Open();
        entryStream.Write(content);
    }

    private sealed class ScannerScope : IDisposable
    {
        private readonly HttpClient _client;
        private readonly HttpMessageHandler _handler;

        public ScannerScope(IOptions<UploadScanOptions> options, HttpMessageHandler? handler, ILogger<UploadMalwareScanner> logger)
        {
            _handler = handler ?? new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
            _client = new HttpClient(_handler, disposeHandler: false);
            Scanner = new UploadMalwareScanner(options, new SingleClientFactory(_client), logger);
        }

        public UploadMalwareScanner Scanner { get; }

        public void Dispose()
        {
            _client.Dispose();
            _handler.Dispose();
        }
    }

    private sealed class SingleClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        public int Calls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(responder(request));
        }
    }

    private sealed class ListLogger<T> : ILogger<T>
    {
        public List<string> Lines { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Lines.Add(formatter(state, exception));
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();

            public void Dispose()
            {
            }
        }
    }
}

public class CvUploadApiTests : IClassFixture<CvUploadApiFactory>
{
    private readonly CvUploadApiFactory _factory;

    public CvUploadApiTests(CvUploadApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Infected_scan_returns_400_and_does_not_replace_stored_cv()
    {
        await ResetStoredCvAsync(new byte[] { 1, 2, 3, 4 });
        Arm(new UploadScanResult(UploadMalwareVerdict.Infected, "Eicar-Test-Signature"));

        using var client = Authed();
        var response = await PostCvAsync(client, "cv.pdf", CandidateCvFileRules.PdfContentType, MinimalPdf());
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(UploadScanRules.InfectedMessage, body, StringComparison.Ordinal);
        Assert.DoesNotContain("Eicar", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("LOBSY", body, StringComparison.Ordinal);

        var stored = await ReadStoredCvAsync();
        Assert.Equal(new byte[] { 1, 2, 3, 4 }, stored);
    }

    [Fact]
    public async Task Unavailable_scan_is_400_when_fail_closed()
    {
        await ResetStoredCvAsync(null);
        Arm(UploadScanResult.Unavailable);

        using var client = Authed();
        var response = await PostCvAsync(client, "cv.pdf", CandidateCvFileRules.PdfContentType, MinimalPdf());
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(UploadScanRules.UnavailableMessage, body, StringComparison.Ordinal);
        Assert.Null(await ReadStoredCvAsync());
    }

    [Fact]
    public async Task Exe_disguised_as_pdf_is_400_before_the_scanner()
    {
        await ResetStoredCvAsync(null);
        Arm(UploadScanResult.Clean, throwIfCalled: true);

        using var client = Authed();
        var exe = new byte[] { (byte)'M', (byte)'Z', 0x90, 0x00, 0x03, 0x00, 0x00, 0x00, 0x04 };
        var response = await PostCvAsync(client, "cv.pdf", "application/pdf", exe);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("geen PDF", body, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, _factory.Scanner.Calls);
        Assert.Null(await ReadStoredCvAsync());
    }

    [Fact]
    public async Task Clean_pdf_is_stored()
    {
        await ResetStoredCvAsync(null);
        Arm(UploadScanResult.Clean);
        var pdf = MinimalPdf();

        using var client = Authed();
        var response = await PostCvAsync(client, "cv.pdf", "application/pdf", pdf);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(pdf, await ReadStoredCvAsync());
        Assert.True(_factory.Scanner.Calls >= 1);
    }

    private void Arm(UploadScanResult next, bool throwIfCalled = false)
    {
        _factory.Scanner.Next = next;
        _factory.Scanner.ThrowIfCalled = throwIfCalled;
        _factory.Scanner.Calls = 0;
    }

    private HttpClient Authed()
    {
        var client = _factory.CreateClient();
        JobsyTestAuth.Authorize(client, _factory.CandidateId);
        return client;
    }

    private static async Task<HttpResponseMessage> PostCvAsync(HttpClient client, string fileName, string contentType, byte[] bytes)
    {
        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        form.Add(file, "file", fileName);
        return await client.PostAsync("api/me/cv", form);
    }

    private async Task ResetStoredCvAsync(byte[]? content)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var rows = await db.CandidateUploadedCvs.Where(c => c.UserId == _factory.CandidateId).ToListAsync();
        db.CandidateUploadedCvs.RemoveRange(rows);
        if (content is not null)
        {
            db.CandidateUploadedCvs.Add(new CandidateUploadedCv
            {
                Id = Guid.NewGuid(),
                UserId = _factory.CandidateId,
                FileName = "oud.pdf",
                ContentType = CandidateCvFileRules.PdfContentType,
                Content = content,
                SizeBytes = content.Length,
                UploadedAtUtc = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync();
    }

    private async Task<byte[]?> ReadStoredCvAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var row = await db.CandidateUploadedCvs.AsNoTracking().SingleOrDefaultAsync(c => c.UserId == _factory.CandidateId);
        return row?.Content;
    }

    private static byte[] MinimalPdf()
        => Encoding.ASCII.GetBytes("%PDF-1.4\n1 0 obj<<>>endobj\ntrailer<<>>\n%%EOF\n");
}

public sealed class CvUploadApiFactory : WebApplicationFactory<Jobsy.Api.ApiAssemblyMarker>
{
    public Guid CandidateId { get; } = Guid.Parse("c0ffee00-0000-0000-0000-0000000000c1");

    public ScriptedScanner Scanner { get; } = new();

    private readonly string _dbName = "CvUpload-" + Guid.NewGuid();
    private readonly object _seedGate = new();
    private bool _seeded;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        JobsyTestAuth.ApplyStandardAuthSettings(builder);
        builder.UseSetting("Seed:Enabled", "false");
        builder.UseSetting("Swagger:Enabled", "false");
        builder.UseSetting("UploadScan:Enabled", "true");
        builder.UseSetting("UploadScan:FailClosed", "true");
        builder.UseSetting(
            "ConnectionStrings:JobsyDb",
            "Host=127.0.0.1;Port=5432;Database=JobsyTest;Username=postgres;Password=postgres");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IHostedService>();
            var efDescriptors = services
                .Where(d =>
                    d.ServiceType == typeof(JobsyDbContext)
                    || d.ServiceType == typeof(DbContextOptions<JobsyDbContext>)
                    || (d.ServiceType.IsGenericType
                        && d.ServiceType.GetGenericTypeDefinition().Name.Contains("DbContext", StringComparison.Ordinal))
                    || (d.ImplementationType?.FullName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true)
                    || (d.ServiceType.FullName?.Contains("EntityFrameworkCore", StringComparison.Ordinal) == true
                        && d.ServiceType.FullName.Contains("JobsyDbContext", StringComparison.Ordinal)))
                .ToList();
            foreach (var d in efDescriptors)
            {
                services.Remove(d);
            }

            foreach (var d in services.Where(d =>
                         d.ServiceType.IsGenericType
                         && d.ServiceType.GetGenericTypeDefinition() == typeof(IDbContextOptionsConfiguration<>)
                         && d.ServiceType.GenericTypeArguments[0] == typeof(JobsyDbContext)).ToList())
            {
                services.Remove(d);
            }

            services.AddDbContext<JobsyDbContext>(options => options.UseInMemoryDatabase(_dbName));
            services.RemoveAll<IUploadMalwareScanner>();
            services.AddSingleton<IUploadMalwareScanner>(Scanner);
        });
    }

    protected override void ConfigureClient(HttpClient client)
    {
        EnsureSeeded();
        base.ConfigureClient(client);
    }

    private void EnsureSeeded()
    {
        lock (_seedGate)
        {
            if (_seeded)
            {
                return;
            }

            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
            db.Users.Add(new User
            {
                Id = CandidateId,
                Email = "cv-upload@example.com",
                FullName = "Cv Tester",
                Role = UserRole.Candidate,
                IsActive = true,
                DateOfBirth = new DateOnly(1992, 4, 1)
            });
            db.SaveChanges();
            _seeded = true;
        }
    }

    public sealed class ScriptedScanner : IUploadMalwareScanner
    {
        public UploadScanResult Next { get; set; } = UploadScanResult.Clean;

        public bool ThrowIfCalled { get; set; }

        public int Calls { get; set; }

        public Task<UploadScanResult> ScanAsync(ReadOnlyMemory<byte> content, CancellationToken cancellationToken = default)
        {
            if (ThrowIfCalled)
            {
                throw new InvalidOperationException("Scanner mocht niet starten.");
            }

            Calls++;
            return Task.FromResult(Next);
        }
    }
}
