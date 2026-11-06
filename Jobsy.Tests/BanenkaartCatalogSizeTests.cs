using System.IO.Compression;
using System.Text;
using Jobsy.Api.Banenkaart;
using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;

namespace Jobsy.Tests;

public sealed class BanenkaartCatalogSizeTests
{
    [Fact]
    public void Catalog_gzip_size_scales_for_acc_and_10k_estimate()
    {
        var haaglanden = BuildSyntheticRecords(225);
        var payload225 = BanenkaartCatalogSerializer.Serialize(haaglanden);
        var gzip225 = Gzip(payload225.Body);

        var sample10 = BuildSyntheticRecords(10_000);
        var payload10 = BanenkaartCatalogSerializer.Serialize(sample10);
        var gzip10 = Gzip(payload10.Body);
        var perVacancy = gzip10 / 10_000.0;

        Assert.True(gzip225 > 1000, $"225 catalog gzip should be non-trivial ({gzip225} bytes).");
        Assert.True(gzip10 < 4_000_000, $"10k catalog gzip estimate should stay under 4 MB ({gzip10} bytes).");

        var report = new
        {
            count225 = 225,
            raw225 = payload225.Body.Length,
            gzip225,
            count10k = 10_000,
            raw10k = payload10.Body.Length,
            gzip10k = gzip10,
            gzipPerVacancyEstimate = perVacancy,
            projected10kGzipKb = Math.Round(gzip10 / 1024.0, 1)
        };

        var outPath = "/opt/cursor/artifacts/banenkaart-catalog-size.json";
        Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
        File.WriteAllText(outPath, System.Text.Json.JsonSerializer.Serialize(report, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
    }

    private static int Gzip(byte[] body)
    {
        using var ms = new MemoryStream();
        using (var gz = new GZipStream(ms, CompressionLevel.SmallestSize, leaveOpen: true))
        {
            gz.Write(body, 0, body.Length);
        }

        return (int)ms.Length;
    }

    private static List<VacancyDiscoveryRecord> BuildSyntheticRecords(int count)
    {
        var list = new List<VacancyDiscoveryRecord>(count);
        for (var i = 0; i < count; i++)
        {
            var id = Guid.NewGuid();
            list.Add(new VacancyDiscoveryRecord(
                Id: id,
                Title: $"Vacature {i} retail medewerker",
                Description: "Synthetic catalog row for size test.",
                HourlyWage: 14.5m + (i % 7),
                StartDate: DateOnly.FromDateTime(DateTime.UtcNow),
                EndDate: DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(6)),
                Status: VacancyStatus.Active,
                CompanyId: Guid.NewGuid(),
                CompanyName: $"Bedrijf {(i % 40)} BV",
                CompanyAddress: "Den Haag",
                CompanyLogoUrl: null,
                ImageUrl: i % 3 == 0 ? "/api/vacancies/image-stub" : null,
                VideoUrl: null,
                Latitude: 52.07 + (i % 100) * 0.01,
                Longitude: 4.28 + (i % 100) * 0.015,
                RequiredTransport: TransportMode.Bike,
                RequiredTransportLabels: ["Fiets"],
                WorkTypes: WorkType.Winkel,
                WorkTypeLabels: "Parttime",
                WorkTypeLabelList: ["Parttime"],
                IsHighlighted: false,
                HighlightedUntil: null,
                ExtensionCount: 0,
                SalaryTableId: null,
                SalaryRates: [],
                RequiredDrivingLicense: null,
                RequiredEducation: null,
                MinimumEmployers: null,
                FulfilledByApplicationId: null,
                CreatedVia: VacancySource.Manual,
                MinHoursPerWeek: 16,
                MaxHoursPerWeek: 32,
                FlexibleTimes: false,
                ScheduleJson: null,
                LegalWorksAfter19: null,
                LegalNightShift23To06: null,
                LegalAdultSupervisorPresent: null,
                LegalHandlesMoneyOrClosing: null,
                LegalHeavyOrHazardousWork: null,
                OfferedByLabel: null,
                ShowClientAddressOnMap: false,
                IntermediaryCompanyId: null,
                Kind: VacancyKind.Regular,
                ExclusivitySettingId: null,
                ExclusivityName: null,
                ExclusivityIsOpen: true,
                ExclusivitySchoolDomain: null,
                ExclusivityEducations: [],
                CategoryId: Guid.NewGuid(),
                CategoryName: "Retail",
                CategoryColorHex: "#22c55e",
                SuitableFor65Plus: false,
                KvkNumber: null,
                Vestigingsnummer: null,
                ContentModerationPassed: true,
                RequireEmailVerification: false));
        }

        return list;
    }
}
