using Jobsy.Core.Enums;

namespace Jobsy.Core.Interfaces;

public enum RaamflyerFormat
{
    A4 = 0,
    A3 = 1
}

public enum RaamflyerScope
{
    /// <summary>Single vestiging / filiaal.</summary>
    Branch = 0,
    /// <summary>All accessible branches under an organisation or region.</summary>
    Overview = 1
}

public interface IEmployerRaamflyerService
{
    /// <summary>
    /// Resolves the QR target for a branch: 1 vacancy → detail page; 2+ → map with company focus.
    /// </summary>
    Task<RaamflyerQrTarget> ResolveBranchQrTargetAsync(
        Guid companyId,
        CancellationToken cancellationToken = default);

    Task<byte[]> RenderBranchFlyerAsync(
        Guid companyId,
        RaamflyerFormat format = RaamflyerFormat.A4,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Overview flyer for multiple companies (regional / enterprise holding).
    /// QR opens the map filtered to the organisation / listed branches.
    /// </summary>
    Task<byte[]> RenderOverviewFlyerAsync(
        IReadOnlyList<Guid> companyIds,
        string title,
        RaamflyerFormat format = RaamflyerFormat.A4,
        CancellationToken cancellationToken = default);
}

public sealed record RaamflyerQrTarget(
    string AbsoluteUrl,
    string ShortDisplayUrl,
    int ActiveVacancyCount,
    Guid? SingleVacancyId,
    RaamflyerQrKind Kind);

public enum RaamflyerQrKind
{
    VacancyDetail = 0,
    MapCompanyCluster = 1,
    MapEmptyBranch = 2
}
