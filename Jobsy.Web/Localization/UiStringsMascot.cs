using Jobsy.Web.Media;

namespace Jobsy.Web.Localization;

public static class UiStringsMascot
{
    public static void MergeAll(
        Dictionary<string, string> nl, Dictionary<string, string> en,
        Dictionary<string, string> pl, Dictionary<string, string> ro,
        Dictionary<string, string> ar)
    {
        void Add(string key, string nlText, string enText, string plText, string roText, string arText)
        {
            nl[key] = nlText;
            en[key] = enText;
            pl[key] = plText;
            ro[key] = roText;
            ar[key] = arText;
        }

        Add("Mascot.Aria.Rustig",
            "Lobsy, rustig", "Lobsy, calm", "Lobsy, spokojnie", "Lobsy, calm", "لوبسي، هادئ");
        Add("Mascot.Aria.Uitleggen",
            "Lobsy legt uit", "Lobsy explaining", "Lobsy wyjaśnia", "Lobsy explică", "لوبسي يشرح");
        Add("Mascot.Aria.Nieuwsgierig",
            "Lobsy is nieuwsgierig", "Lobsy is curious", "Lobsy jest ciekawy", "Lobsy e curios", "لوبسي فضولي");
        Add("Mascot.Aria.GoedGedaan",
            "Lobsy: goed gedaan", "Lobsy: well done", "Lobsy: dobra robota", "Lobsy: bravo", "لوبسي: أحسنت");
        Add("Mascot.Aria.Nadenken",
            "Lobsy denkt na", "Lobsy is thinking", "Lobsy się zastanawia", "Lobsy se gândește", "لوبسي يفكر");
        Add("Mascot.Aria.Juichen",
            "Lobsy juicht", "Lobsy is cheering", "Lobsy wiwatuje", "Lobsy aplaudă", "لوبسي يهلّل");
        Add("Mascot.Aria.Shell",
            "Lobsy oude schild", "Lobsy's old shell", "Stara skorupa Lobsy", "Cochilia veche a lui Lobsy", "قشرة لوبسي القديمة");
        Add("Mascot.Aria.Avatar",
            "Lobsy", "Lobsy", "Lobsy", "Lobsy", "لوبسي");
    }

    public static string AriaKey(MascotPose pose) => pose switch
    {
        MascotPose.Uitleggen => "Mascot.Aria.Uitleggen",
        MascotPose.Nieuwsgierig => "Mascot.Aria.Nieuwsgierig",
        MascotPose.GoedGedaan => "Mascot.Aria.GoedGedaan",
        MascotPose.Nadenken => "Mascot.Aria.Nadenken",
        MascotPose.Juichen => "Mascot.Aria.Juichen",
        MascotPose.Shell => "Mascot.Aria.Shell",
        _ => "Mascot.Aria.Rustig",
    };
}
