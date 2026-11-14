namespace Jobsy.Tests;

internal static class CoachVariantBGuardSupport
{
    internal const string DockComponent = "LobsyCoachVariantBDock";
    internal const string LobsterBtnClass = "lobsy-coach-dock__btn--lobster-b";
    internal const string LobsterImgClass = "lobsy-coach-dock__lobster";
    internal const string CoachImageAsset = "BrandImages.AbsoluteCoachB";
    internal const string CoachAvatarAsset = "BrandImages.AbsoluteCoachBAvatar";

    internal static string ReadCoachVariantCss(string root)
        => File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/css/features/lobsy-coach-variant-b.css"));
}