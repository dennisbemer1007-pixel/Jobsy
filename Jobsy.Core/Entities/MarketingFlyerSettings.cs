namespace Jobsy.Core.Entities;

/// <summary>
/// Singleton row for the admin-editable employer marketing flyer (A4 PDF).
/// </summary>
public class MarketingFlyerSettings
{
    public Guid Id { get; set; }

    public string Headline { get; set; } = string.Empty;
    public string Subheadline { get; set; } = string.Empty;
    public string Intro { get; set; } = string.Empty;

    /// <summary>One USP bullet per line.</summary>
    public string BulletPoints { get; set; } = string.Empty;

    public string PromoFreeText { get; set; } = string.Empty;
    public string PromoDiscountText { get; set; } = string.Empty;

    public string CtaTitle { get; set; } = string.Empty;
    public string CtaBody { get; set; } = string.Empty;
    public string QrCaption { get; set; } = string.Empty;

    /// <summary>Relative web path for the QR target (e.g. /register).</summary>
    public string QrPath { get; set; } = "/register";

    public string FooterNote { get; set; } = string.Empty;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
