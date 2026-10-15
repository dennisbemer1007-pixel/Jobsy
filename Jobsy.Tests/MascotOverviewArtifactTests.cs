using System.Text;
using Jobsy.Web.Components.Brand;
using Jobsy.Web.Media;

namespace Jobsy.Tests;

public class MascotOverviewArtifactTests
{
    [Fact]
    public void Write_mascot_overview_html_for_screenshots()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("WRITE_MASCOT_OVERVIEW"), "1", StringComparison.Ordinal))
        {
            return;
        }

        var outDir = Environment.GetEnvironmentVariable("CURSOR_ARTIFACTS_DIR") ?? "/opt/cursor/artifacts";
        Directory.CreateDirectory(outDir);
        var root = TestRepo.FindRoot();
        var refPath = Path.Combine(root, "docs/brand/kreeft-set-scharen-reference.png");
        if (!File.Exists(refPath))
        {
            refPath = "/home/ubuntu/.cursor/projects/workspace/uploads/kreeft-set-scharen_34c9.png";
        }

        var sb = new StringBuilder();
        sb.Append("""
<!DOCTYPE html><html lang="nl"><head><meta charset="utf-8"/>
<title>Lobsy mascot — 6 standen</title>
<style>
body{font-family:system-ui;background:#f5f2ee;color:#122033;padding:24px}
.grid{display:grid;grid-template-columns:repeat(3,160px);gap:24px}
.card{background:#fffcfa;border-radius:12px;padding:12px;text-align:center;box-shadow:0 4px 16px #0f2d5c14}
.card img,.card svg{width:128px;height:128px;display:block;margin:0 auto}
.card h2{font-size:14px;margin:8px 0 0}
.sizes{display:flex;gap:12px;align-items:end;margin-top:32px}
.sizes figure{text-align:center;font-size:12px;color:#5a6a7d}
.avatar{background:#0a2044;border-radius:12px;padding:12px;display:inline-block}
</style></head><body>
<h1>Lobsy koraal-mascotte (getraceerd uit referentie)</h1>
<p style="max-width:900px;color:#5a6a7d">Links: Dennis-referentie · rechts: inline SVG (zelfde viewBox 128×128)</p>
<div style="display:flex;gap:24px;align-items:flex-start;margin-bottom:24px;flex-wrap:wrap">
<img src="kreeft-reference.png" alt="referentie" style="max-width:640px;height:auto;border-radius:8px"/>
</div>
<div class="grid">
""");

        var tracedDir = Path.Combine(root, "tools/brand/mascot-traced");
        var tracedOut = Path.Combine(outDir, "mascot-traced");
        Directory.CreateDirectory(tracedOut);
        foreach (MascotPose pose in Enum.GetValues<MascotPose>())
        {
            if (pose == MascotPose.Shell)
            {
                continue;
            }

            var slug = MascotAssets.PoseSlug(pose);
            var tracedSvg = Path.Combine(tracedDir, $"mascot-{slug}.svg");
            sb.Append("<div class=\"card\">");
            if (File.Exists(tracedSvg))
            {
                var dest = Path.Combine(tracedOut, $"mascot-{slug}.svg");
                File.Copy(tracedSvg, dest, overwrite: true);
                sb.Append("<img src=\"mascot-traced/mascot-").Append(slug).Append(".svg\" width=\"128\" height=\"128\" alt=\"\"/>");
            }
            else
            {
                var inner = new StringBuilder();
                LobsyMascotArt.AppendSvg(inner, pose, pose.ToString(), LobsyMascotArt.MascotVariant.Full);
                sb.Append(inner);
            }

            sb.Append("<h2>").Append(slug).Append("</h2></div>");
        }

        sb.Append("</div><div class=\"sizes\"><h2 style=\"width:100%\">Avatar (kop + scharen)</h2>");
        foreach (var px in new[] { 32, 64, 160 })
        {
            var av = new StringBuilder();
            LobsyMascotArt.AppendSvg(av, MascotPose.Rustig, "av", LobsyMascotArt.MascotVariant.Avatar);
            sb.Append("<figure><div class=\"avatar\" style=\"width:").Append(px).Append("px;height:").Append(px)
                .Append("px\">").Append(av.ToString().Replace("class=\"pub-mascot__svg\"", $"style=\"width:{px}px;height:{px}px\"", StringComparison.Ordinal))
                .Append("</div><figcaption>").Append(px).Append("px</figcaption></figure>");
        }

        sb.Append("</div></body></html>");
        var path = Path.Combine(outDir, "mascot-overview.html");
        if (File.Exists(refPath))
        {
            File.Copy(refPath, Path.Combine(outDir, "kreeft-reference.png"), overwrite: true);
        }

        var html = sb.ToString().Replace("file://REF_PATH", "kreeft-reference.png", StringComparison.Ordinal);
        File.WriteAllText(path, html);
        Assert.True(File.Exists(path));
    }
}
