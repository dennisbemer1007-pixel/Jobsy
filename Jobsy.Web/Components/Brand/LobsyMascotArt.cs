using System.Globalization;
using System.Text;
using Jobsy.Web.Media;
using Microsoft.AspNetCore.Components;

namespace Jobsy.Web.Components.Brand;

/// <summary>Hand-drawn inline SVG for the coral Lobsy mascot (128×128 viewBox).</summary>
public static class LobsyMascotArt
{
    public const string ViewBox = "0 0 128 128";
    public const string AvatarViewBox = "24 16 80 72";

    public const string Coral = "#F54A1B";
    public const string CoralDark = "#D93A12";
    public const string Ink = "#0F2D5C";

    /// <summary>Torso mask for journey plate shedding (256×256 journey SVG space).</summary>
    public const string JourneyBodyPath =
        "M88 118 C86 168 106 198 128 218 C150 198 170 168 168 118 Z";

    public enum MascotVariant
    {
        Full,
        Avatar,
    }

    private enum MouthKind
    {
        Smile,
        SmileWide,
        OpenSmall,
        OpenLarge,
        Neutral,
        Pensive,
    }

    private readonly record struct ArmPose(double X, double Y, double Deg, bool Open);

    private readonly record struct FacePose(
        double PupilDx,
        double PupilDy,
        MouthKind Mouth,
        bool EyesWide = false);

    public static void AppendSvg(
        StringBuilder sb,
        MascotPose pose,
        string uid,
        MascotVariant variant = MascotVariant.Full,
        bool shellGhost = false)
    {
        var spec = SpecFor(pose);
        var vb = variant == MascotVariant.Avatar ? AvatarViewBox : ViewBox;
        sb.Append(CultureInfo.InvariantCulture, $"""<svg xmlns="http://www.w3.org/2000/svg" viewBox="{vb}" class="pub-mascot__svg" focusable="false" aria-hidden="true">""");
        sb.Append("<defs>");
        AppendClawSymbol(sb, uid);
        sb.Append("</defs>");
        if (shellGhost)
        {
            sb.Append("""<g class="pub-mascot__ghost">""");
        }

        AppendBody(sb);
        AppendFace(sb, spec.Face);
        AppendArm(sb, uid, spec.Left, true);
        AppendArm(sb, uid, spec.Right, false);

        if (shellGhost)
        {
            sb.Append("</g>");
        }

        sb.Append("</svg>");
    }

    public static MarkupString RenderMarkup(MascotPose pose, string uid, MascotVariant variant = MascotVariant.Full, bool shellGhost = false)
    {
        var sb = new StringBuilder(2048);
        AppendSvg(sb, pose, uid, variant, shellGhost);
        return (MarkupString)sb.ToString();
    }

    /// <summary>Character only (no outer svg) for embedding e.g. in journey lobster.</summary>
    public static void AppendCharacter(StringBuilder sb, MascotPose pose, string uid)
    {
        var spec = SpecFor(pose);
        sb.Append("<defs>");
        AppendClawSymbol(sb, uid);
        sb.Append("</defs>");
        AppendBody(sb);
        AppendFace(sb, spec.Face);
        AppendArm(sb, uid, spec.Left, true);
        AppendArm(sb, uid, spec.Right, false);
    }

    private static (FacePose Face, ArmPose Left, ArmPose Right) SpecFor(MascotPose pose) => pose switch
    {
        MascotPose.Uitleggen => (
            new FacePose(0, 0, MouthKind.OpenSmall),
            new ArmPose(28, 62, 118, false),
            new ArmPose(100, 48, -35, true)),
        MascotPose.Nieuwsgierig => (
            new FacePose(-3, 0, MouthKind.Neutral),
            new ArmPose(28, 66, 105, false),
            new ArmPose(100, 66, 75, false)),
        MascotPose.GoedGedaan => (
            new FacePose(0, 0, MouthKind.SmileWide),
            new ArmPose(26, 44, -55, true),
            new ArmPose(102, 66, 78, false)),
        MascotPose.Nadenken => (
            new FacePose(2, -3, MouthKind.Pensive),
            new ArmPose(28, 66, 108, false),
            new ArmPose(100, 66, 72, false)),
        MascotPose.Juichen => (
            new FacePose(0, 0, MouthKind.OpenLarge, EyesWide: true),
            new ArmPose(30, 38, -42, true),
            new ArmPose(98, 38, 42, true)),
        MascotPose.Shell => (
            new FacePose(0, 0, MouthKind.Smile),
            new ArmPose(28, 66, 108, false),
            new ArmPose(100, 66, 72, false)),
        _ => (
            new FacePose(0, 0, MouthKind.Smile),
            new ArmPose(28, 66, 108, false),
            new ArmPose(100, 66, 72, false)),
    };

    private static void AppendClawSymbol(StringBuilder sb, string uid)
    {
        // One fixed pincer shape; inner finger reuses the same path via transform (never hand-like).
        sb.Append(CultureInfo.InvariantCulture, $"""<symbol id="lobsy-claw-{uid}" viewBox="0 0 24 28" overflow="visible">""");
        sb.Append(CultureInfo.InvariantCulture, $"""<path fill="{Coral}" d="M12 2 C19 2 23 9 21 16 C19 23 12 26 8 22 C4 17 5 9 12 2 Z"/>""");
        sb.Append(CultureInfo.InvariantCulture, $"""<path fill="{CoralDark}" d="M12 8 C15 9 16 14 13 17 C11 18 9 16 10 13 C10 11 11 9 12 8 Z"/>""");
        sb.Append("</symbol>");
    }

    private static void AppendBody(StringBuilder sb)
    {
        sb.Append(CultureInfo.InvariantCulture, $"""<ellipse cx="64" cy="38" rx="26" ry="24" fill="{Coral}"/>""");
        sb.Append(CultureInfo.InvariantCulture, $"""<path fill="{Coral}" d="M48 36 L44 28 L52 32 Z M80 36 L84 28 L76 32 Z"/>""");
        sb.Append(CultureInfo.InvariantCulture, $"""<path fill="none" stroke="{Coral}" stroke-width="2.5" stroke-linecap="round" d="M56 18 C58 10 64 8 64 8 C64 8 70 10 72 18"/>""");
        sb.Append(CultureInfo.InvariantCulture, $"""<path fill="none" stroke="{Coral}" stroke-width="2.5" stroke-linecap="round" d="M64 8 C64 8 62 12 60 16"/>""");
        sb.Append(CultureInfo.InvariantCulture, $"""<rect x="44" y="52" width="40" height="14" rx="7" fill="{CoralDark}"/>""");
        sb.Append(CultureInfo.InvariantCulture, $"""<rect x="46" y="64" width="36" height="12" rx="6" fill="{Coral}"/>""");
        sb.Append(CultureInfo.InvariantCulture, $"""<rect x="48" y="74" width="32" height="11" rx="5.5" fill="{CoralDark}"/>""");
        sb.Append(CultureInfo.InvariantCulture, $"""<rect x="50" y="83" width="28" height="10" rx="5" fill="{Coral}"/>""");
        sb.Append(CultureInfo.InvariantCulture, $"""<path fill="{Coral}" d="M50 92 C50 102 58 108 64 108 C70 108 78 102 78 92 C74 96 54 96 50 92 Z"/>""");
    }

    private static void AppendFace(StringBuilder sb, FacePose face)
    {
        var eyeRy = face.EyesWide ? 5.5 : 4.5;
        sb.Append(CultureInfo.InvariantCulture, $"""<ellipse cx="52" cy="36" rx="7" ry="{eyeRy.ToString(CultureInfo.InvariantCulture)}" fill="{Ink}"/>""");
        sb.Append(CultureInfo.InvariantCulture, $"""<ellipse cx="76" cy="36" rx="7" ry="{eyeRy.ToString(CultureInfo.InvariantCulture)}" fill="{Ink}"/>""");
        sb.Append(CultureInfo.InvariantCulture,
            $"""<circle cx="{(52 + face.PupilDx).ToString(CultureInfo.InvariantCulture)}" cy="{(36 + face.PupilDy).ToString(CultureInfo.InvariantCulture)}" r="2" fill="{Coral}"/>""");
        sb.Append(CultureInfo.InvariantCulture,
            $"""<circle cx="{(76 + face.PupilDx).ToString(CultureInfo.InvariantCulture)}" cy="{(36 + face.PupilDy).ToString(CultureInfo.InvariantCulture)}" r="2" fill="{Coral}"/>""");

        switch (face.Mouth)
        {
            case MouthKind.Smile:
                sb.Append(CultureInfo.InvariantCulture,
                    $"""<path fill="none" stroke="{Ink}" stroke-width="2" stroke-linecap="round" d="M58 46 Q64 49 70 46"/>""");
                break;
            case MouthKind.SmileWide:
                sb.Append(CultureInfo.InvariantCulture,
                    $"""<path fill="none" stroke="{Ink}" stroke-width="2.2" stroke-linecap="round" d="M56 45 Q64 51 72 45"/>""");
                break;
            case MouthKind.OpenSmall:
                sb.Append(CultureInfo.InvariantCulture, $"""<path fill="{Ink}" d="M60 45 C60 49 68 49 68 45 C68 42 60 42 60 45 Z"/>""");
                break;
            case MouthKind.OpenLarge:
                sb.Append(CultureInfo.InvariantCulture, $"""<path fill="{Ink}" d="M56 44 C56 52 72 52 72 44 C72 40 56 40 56 44 Z"/>""");
                break;
            case MouthKind.Pensive:
                sb.Append(CultureInfo.InvariantCulture,
                    $"""<path fill="none" stroke="{Ink}" stroke-width="2" stroke-linecap="round" d="M58 48 Q64 46 70 48"/>""");
                break;
            default:
                sb.Append(CultureInfo.InvariantCulture,
                    $"""<path fill="none" stroke="{Ink}" stroke-width="2" stroke-linecap="round" d="M58 47 H70"/>""");
                break;
        }
    }

    private static void AppendArm(StringBuilder sb, string uid, ArmPose arm, bool left)
    {
        sb.Append(CultureInfo.InvariantCulture,
            $"""<g transform="translate({arm.X.ToString(CultureInfo.InvariantCulture)},{arm.Y.ToString(CultureInfo.InvariantCulture)}) rotate({arm.Deg.ToString(CultureInfo.InvariantCulture)})">""");
        sb.Append(CultureInfo.InvariantCulture, $"""<path fill="{Coral}" d="M0 0 C-6 4 -8 14 -4 20 C-2 22 2 22 4 20 C6 14 4 4 0 0 Z"/>""");
        sb.Append(CultureInfo.InvariantCulture, $"""<use href="#lobsy-claw-{uid}" x="-12" y="16" width="24" height="28"/>""");
        if (arm.Open)
        {
            var rot = left ? -26d : 26d;
            sb.Append(CultureInfo.InvariantCulture,
                $"""<use href="#lobsy-claw-{uid}" width="24" height="28" transform="rotate({rot.ToString(CultureInfo.InvariantCulture)} 0 24) translate(-10 10) scale(0.58)"/>""");
        }

        sb.Append("</g>");
    }

    public static string PoseCssSlug(MascotPose pose) => MascotAssets.PoseSlug(pose);
}
