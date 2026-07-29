namespace Jobsy.Core.Interfaces;

public interface IAboutPageSettingsService
{
    Task<AboutPageSnapshot> GetAsync(CancellationToken cancellationToken = default);

    Task<AboutPageSnapshot> UpdateAsync(
        AboutPageUpdate update,
        CancellationToken cancellationToken = default);
}

public sealed record AboutPageSnapshot(
    string Title,
    string Lead,
    string BodyHtml,
    DateTime? UpdatedAtUtc);

public sealed record AboutPageUpdate(
    string Title,
    string? Lead,
    string BodyHtml);
