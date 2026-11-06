using Jobsy.Web.Localization;

namespace Jobsy.Tests;

public class AdminRun8FollowUpTests
{
    [Theory]
    [InlineData("nl", "Klikken")]
    [InlineData("en", "Clicks")]
    [InlineData("pl", "Kliknięcia")]
    [InlineData("ro", "Clicuri")]
    [InlineData("ar", "النقرات")]
    public void Vacancy_clicks_header_is_plain_language(string language, string expected)
        => Assert.Equal(expected, UiStrings.Get("AdminVacancy.Col.Clicks", language));

    [Theory]
    [InlineData("nl", "Sluitdatum")]
    [InlineData("en", "Closing date")]
    [InlineData("pl", "Data zakończenia")]
    [InlineData("ro", "Data închiderii")]
    [InlineData("ar", "تاريخ الإغلاق")]
    public void Vacancy_closing_header_is_plain_language(string language, string expected)
        => Assert.Equal(expected, UiStrings.Get("AdminVacancy.Col.Closes", language));

    [Theory]
    [InlineData("nl", "Geef een reden van 5 tot 500 tekens.")]
    [InlineData("en", "Give a reason of 5 to 500 characters.")]
    [InlineData("pl", "Podaj powód od 5 do 500 znaków.")]
    [InlineData("ro", "Dă un motiv de 5 până la 500 de caractere.")]
    [InlineData("ar", "اكتب سبباً من 5 إلى 500 حرفاً.")]
    public void Reset_reason_error_is_translated(string language, string expected)
        => Assert.Equal(expected, UiStrings.Get("AdminUsers.ResetTestsReasonInvalid", language));

    [Theory]
    [InlineData("nl", "Alleen een testaccount kan zo worden gereset.")]
    [InlineData("en", "Only a test account can be reset this way.")]
    [InlineData("pl", "Tylko konto testowe można tak zresetować.")]
    [InlineData("ro", "Doar un cont de test poate fi resetat așa.")]
    [InlineData("ar", "يمكن إعادة الضبط بهذه الطريقة لحساب اختبار فقط.")]
    public void Reset_forbidden_error_is_translated(string language, string expected)
        => Assert.Equal(expected, UiStrings.Get("AdminUsers.ResetTestsNotTest", language));
}
