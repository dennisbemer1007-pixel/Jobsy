using System.Globalization;
using System.Text;
using Jobsy.Web.Media;
using Microsoft.AspNetCore.Components;

namespace Jobsy.Web.Components.Brand;

/// <summary>Inline SVG Lobsy mascot — 128×128 art space, reference proportions (Dennis 2026).</summary>
public static class LobsyMascotArt
{
    public const string ViewBox = "0 0 128 128";
    /// <summary>Head + upper claws; same vertical center as full art so faces never clip in 32px UI.</summary>
    public const string AvatarViewBox = "4 4 120 92";

    public const string Coral = "#F54A1B";
    public const string CoralDark = "#D93A12";
    public const string Ink = "#0F2D5C";

    public const string JourneyBodyPath =
        "M52 58 C48 78 50 98 64 108 C78 98 80 78 76 58 Z";

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

    private readonly record struct FacePose(double LookX, double LookY, MouthKind Mouth, bool EyesWide = false);

    /// <summary>Shoulder attach (128-space), rotation deg, claw open, raised.</summary>
    private readonly record struct ArmPose(double ShoulderX, double ShoulderY, double Deg, bool Open, bool Raised);

    public static void AppendSvg(
        StringBuilder sb,
        MascotPose pose,
        string uid,
        MascotVariant variant = MascotVariant.Full,
        bool shellGhost = false)
    {
        var spec = SpecFor(pose);
        var vb = variant == MascotVariant.Avatar ? AvatarViewBox : ViewBox;
        sb.Append(CultureInfo.InvariantCulture,
            $"""<svg xmlns="http://www.w3.org/2000/svg" viewBox="{vb}" class="pub-mascot__svg" focusable="false" aria-hidden="true" overflow="visible" preserveAspectRatio="xMidYMid meet">""");
        sb.Append("<defs>");
        AppendArmClawSymbol(sb, uid);
        sb.Append("</defs>");
        if (shellGhost)
        {
            sb.Append("""<g class="pub-mascot__ghost">""");
        }

        AppendBody(sb);
        AppendArms(sb, uid, spec.Left, spec.Right);
        AppendFace(sb, spec.Face);

        if (shellGhost)
        {
            sb.Append("</g>");
        }

        sb.Append("</svg>");
    }

    public static MarkupString RenderMarkup(MascotPose pose, string uid, MascotVariant variant = MascotVariant.Full, bool shellGhost = false)
    {
        var sb = new StringBuilder(4096);
        AppendSvg(sb, pose, uid, variant, shellGhost);
        return (MarkupString)sb.ToString();
    }

    public static void AppendCharacter(StringBuilder sb, MascotPose pose, string uid)
    {
        var spec = SpecFor(pose);
        sb.Append("<defs>");
        AppendArmClawSymbol(sb, uid);
        sb.Append("</defs>");
        AppendBody(sb);
        AppendArms(sb, uid, spec.Left, spec.Right);
        AppendFace(sb, spec.Face);
    }

    private static (FacePose Face, ArmPose Left, ArmPose Right) SpecFor(MascotPose pose) => pose switch
    {
        MascotPose.Uitleggen => (
            new FacePose(0, 0, MouthKind.OpenSmall),
            new ArmPose(42, 59, -52, true, true),
            new ArmPose(86, 59, 52, false, false)),
        MascotPose.Nieuwsgierig => (
            new FacePose(-4, 0, MouthKind.Neutral),
            new ArmPose(42, 59, 14, false, false),
            new ArmPose(86, 59, -14, false, false)),
        MascotPose.GoedGedaan => (
            new FacePose(0, 0, MouthKind.SmileWide),
            new ArmPose(42, 59, -58, true, true),
            new ArmPose(86, 59, 58, false, false)),
        MascotPose.Nadenken => (
            new FacePose(3, -4, MouthKind.Pensive),
            new ArmPose(42, 59, 18, false, false),
            new ArmPose(86, 59, -18, false, false)),
        MascotPose.Juichen => (
            new FacePose(0, 0, MouthKind.OpenLarge, EyesWide: true),
            new ArmPose(42, 59, -68, true, true),
            new ArmPose(86, 59, 68, true, true)),
        MascotPose.Shell => (
            new FacePose(0, 0, MouthKind.Smile),
            new ArmPose(42, 59, 14, false, false),
            new ArmPose(86, 59, -14, false, false)),
        _ => (
            new FacePose(0, 0, MouthKind.Smile),
            new ArmPose(42, 59, 14, false, false),
            new ArmPose(86, 59, -14, false, false)),
    };

    /// <summary>One continuous arm + lower claw (shoulder at 0,0). Mirror X for right side.</summary>
    private static void AppendArmClawSymbol(StringBuilder sb, string uid)
    {
        sb.Append(CultureInfo.InvariantCulture, $"""<symbol id="lobsy-arm-{uid}" viewBox="-10 0 44 78" overflow="visible">""");
        sb.Append(CultureInfo.InvariantCulture, $"""
<path fill="{Coral}" d="M-8 2
L 8 2
L 7 14
L 6 26
C 5 36 2 44 -4 50
C -10 58 -18 64 -26 66
C -32 66 -35 60 -33 54
C -31 48 -24 44 -18 44
C -14 44 -11 47 -10 51
C -9 55 -12 59 -17 60
C -24 61 -28 55 -27 47
C -24 37 -16 26 -6 16
C -2 8 -8 2 -8 2 Z"/>
""");
        sb.Append(CultureInfo.InvariantCulture, $"""
<path fill="{CoralDark}" opacity="0.35" d="M-18 58 C-22 60 -24 56 -21 52 C-18 50 -15 53 -18 58 Z"/>
""");
        sb.Append("</symbol>");
        sb.Append(CultureInfo.InvariantCulture, $"""<symbol id="lobsy-upper-{uid}" viewBox="-24 24 16 32" overflow="visible">""");
        sb.Append(CultureInfo.InvariantCulture, $"""
<path fill="{Coral}" d="M-12 52
C-16 48 -20 42 -21 36
C-22 30 -18 26 -13 28
C-9 30 -7 34 -8 38
C-9 42 -10 46 -12 52 Z"/>
""");
        sb.Append("</symbol>");
    }

    private static void AppendBody(StringBuilder sb)
    {
        sb.Append(CultureInfo.InvariantCulture, $"""
<path fill="{Coral}" d="M64 14
C38 14 28 26 28 40
C28 52 34 58 64 58
C94 58 100 52 100 40
C100 26 90 14 64 14 Z"/>
""");
        sb.Append(CultureInfo.InvariantCulture, $"""<path fill="{Coral}" d="M30 42 L26 36 L34 38 Z M98 42 L102 36 L94 38 Z"/>""");
        sb.Append(CultureInfo.InvariantCulture, $"""
<path fill="none" stroke="{Coral}" stroke-width="3.5" stroke-linecap="round"
 d="M50 16 C48 8 46 6 44 10 M78 16 C80 8 82 6 84 10"/>
""");
        sb.Append(CultureInfo.InvariantCulture, $"""<path fill="{CoralDark}" d="M42 56 H86 C84 60 76 62 64 62 C52 62 44 60 42 56 Z"/>""");
        sb.Append(CultureInfo.InvariantCulture, $"""<path fill="{Coral}" d="M46 62 H82 L80 72 H48 Z"/>""");
        sb.Append(CultureInfo.InvariantCulture, $"""<path fill="{CoralDark}" d="M48 72 H80 L78 82 H50 Z"/>""");
        sb.Append(CultureInfo.InvariantCulture, $"""<path fill="{Coral}" d="M50 82 H78 L76 92 H52 Z"/>""");
        sb.Append(CultureInfo.InvariantCulture, $"""<path fill="{CoralDark}" d="M52 92 H76 L74 100 H54 Z"/>""");
        sb.Append(CultureInfo.InvariantCulture, $"""<path fill="{Coral}" d="M54 100 H74 L72 106 H56 Z"/>""");
        sb.Append(CultureInfo.InvariantCulture, $"""
<ellipse fill="{Coral}" cx="60" cy="112" rx="8" ry="6"/>
<ellipse fill="{Coral}" cx="68" cy="112" rx="8" ry="6"/>
""");
    }

    private static void AppendArms(StringBuilder sb, string uid, ArmPose left, ArmPose right)
    {
        AppendArmUse(sb, uid, left, mirror: false);
        AppendArmUse(sb, uid, right, mirror: true);
    }

    private static void AppendArmUse(StringBuilder sb, string uid, ArmPose arm, bool mirror)
    {
        var sx = arm.ShoulderX.ToString(CultureInfo.InvariantCulture);
        var sy = arm.ShoulderY.ToString(CultureInfo.InvariantCulture);
        var deg = arm.Deg.ToString(CultureInfo.InvariantCulture);
        var mirrorScale = mirror ? "-1" : "1";
        sb.Append(CultureInfo.InvariantCulture,
            $"""<g transform="translate({sx},{sy}) scale({mirrorScale},1) rotate({deg})">""");
        sb.Append(CultureInfo.InvariantCulture, $"""<use href="#lobsy-arm-{uid}" width="44" height="78" x="0" y="0"/>""");
        var upperRot = arm.Open ? (arm.Raised ? "-28" : "-18") : "0";
        sb.Append(CultureInfo.InvariantCulture,
            $"""<use href="#lobsy-upper-{uid}" width="44" height="78" x="0" y="0" transform="rotate({upperRot} -12 52)"/>""");

        sb.Append("</g>");
    }

    private static void AppendFace(StringBuilder sb, FacePose face)
    {
        AppendAlmondEye(sb, 50 + face.LookX, 36 + face.LookY, left: true, face.EyesWide);
        AppendAlmondEye(sb, 78 + face.LookX, 36 + face.LookY, left: false, face.EyesWide);

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
                sb.Append(CultureInfo.InvariantCulture, $"""<path fill="{Ink}" d="M60 44 C60 48 68 48 68 44 C68 41 60 41 60 44 Z"/>""");
                break;
            case MouthKind.OpenLarge:
                sb.Append(CultureInfo.InvariantCulture, $"""<path fill="{Ink}" d="M56 43 C56 51 72 51 72 43 C72 39 56 39 56 43 Z"/>""");
                break;
            case MouthKind.Pensive:
                sb.Append(CultureInfo.InvariantCulture,
                    $"""<path fill="none" stroke="{Ink}" stroke-width="2" stroke-linecap="round" d="M58 47 Q64 45 70 47"/>""");
                break;
            default:
                sb.Append(CultureInfo.InvariantCulture,
                    $"""<path fill="none" stroke="{Ink}" stroke-width="2" stroke-linecap="round" d="M58 46 H70"/>""");
                break;
        }
    }

    private static void AppendAlmondEye(StringBuilder sb, double cx, double cy, bool left, bool wide)
    {
        var w = wide ? 6.5 : 5.5;
        var h = wide ? 5.0 : 4.0;
        if (left)
        {
            sb.Append(CultureInfo.InvariantCulture, $"""
<path fill="{Ink}" d="M{cx - w:0.#} {cy + 1:0.#} L{cx + w - 0.5:0.#} {cy - 2:0.#} L{cx + w:0.#} {cy + h - 2:0.#} Q{cx:0.#} {cy + h + 0.5:0.#} {cx - w:0.#} {cy + h - 1:0.#} Z"/>
""");
        }
        else
        {
            sb.Append(CultureInfo.InvariantCulture, $"""
<path fill="{Ink}" d="M{cx + w:0.#} {cy + 1:0.#} L{cx - w + 0.5:0.#} {cy - 2:0.#} L{cx - w:0.#} {cy + h - 2:0.#} Q{cx:0.#} {cy + h + 0.5:0.#} {cx + w:0.#} {cy + h - 1:0.#} Z"/>
""");
        }
    }

    public static string PoseCssSlug(MascotPose pose) => MascotAssets.PoseSlug(pose);
}
