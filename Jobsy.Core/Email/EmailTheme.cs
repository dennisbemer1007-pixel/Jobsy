namespace Jobsy.Core.Email;

/// <summary>Light/dark hex tokens for transactional mail. Only the renderer reads these.</summary>
public static class EmailTheme
{
    public static class Light
    {
        public const string Bg = "#f5f2ee";
        public const string Surface = "#fffcfa";
        public const string Text = "#122033";
        public const string Muted = "#5a6a7d";
        public const string Border = "#ddd5cc";
        public const string Brand = "#0f2d5c";
        public const string BrandDeep = "#0a2044";
        public const string Coral = "#f54a1b";
        public const string Peach = "#fee7df";
        public const string Sun = "#f1e5c3";
        public const string Sky = "#e7eef7";
        public const string Mint = "#ecfdf3";
        public const string BtnText = "#ffffff";
    }

    public static class Dark
    {
        public const string Bg = "#0d1726";
        public const string Surface = "#15233a";
        public const string Text = "#eef2f7";
        public const string Muted = "#a9b6c6";
        public const string Border = "#2a3a52";
        public const string Sky = "#1f3150";
        public const string Peach = "#3b2621";
        public const string Sun = "#3a3220";
        public const string Mint = "#16352a";
        public const string Btn = "#fffcfa";
        public const string BtnText = "#0f2d5c";
    }

    public const string Font =
        "Inter,-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Helvetica,Arial,sans-serif";

    public const string FontArabic =
        "'Noto Sans Arabic',Tahoma,'Segoe UI',Arial,sans-serif";

    public static string ToneBackground(Model.EmailTone tone, bool dark) => tone switch
    {
        Model.EmailTone.Peach => dark ? Dark.Peach : Light.Peach,
        Model.EmailTone.Sun => dark ? Dark.Sun : Light.Sun,
        Model.EmailTone.Mint => dark ? Dark.Mint : Light.Mint,
        _ => dark ? Dark.Sky : Light.Sky
    };

    public static string ToneClass(Model.EmailTone tone) => tone switch
    {
        Model.EmailTone.Peach => "tone-peach",
        Model.EmailTone.Sun => "tone-sun",
        Model.EmailTone.Mint => "tone-mint",
        _ => "tone-sky"
    };
}
