using Jobsy.Tests;

// Release-built Jobsy.Web + illustrated banenkaart stubs (same as compare harness).
Console.WriteLine("Banenkaart lagen capture host starting…");
using var factory = new IllustratedPagesWebFactory();
var baseUrl = factory.ServerAddress.TrimEnd('/');
Console.WriteLine($"BANENKAART_LAGEN_BASE_URL={baseUrl}");
var outFile = Environment.GetEnvironmentVariable("BANENKAART_LAGEN_URL_FILE") ?? "/tmp/banenkaart-lagen-base-url.txt";
await File.WriteAllTextAsync(outFile, baseUrl);

using var quit = new ManualResetEventSlim(false);
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    quit.Set();
};
AppDomain.CurrentDomain.ProcessExit += (_, _) => quit.Set();
quit.Wait();
