namespace Jobsy.Tests;

public class AccountDeleteCopyTests
{
    [Fact]
    public void Unsubscribe_and_profile_copy_say_account_verwijderen()
    {
        var root = FindRepoRoot();
        var extras = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Localization", "UiStringsExtras.cs"));
        var ui = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Localization", "UiStrings.cs"));
        var privacy = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Components", "Pages", "Legal", "PrivacyData.razor"));

        Assert.Contains("[\"Unsubscribe.Title\"] = \"Account verwijderen\"", extras);
        Assert.Contains("naam, e-mail, telefoon", extras);
        Assert.Contains("[\"Profile.Unsubscribe\"] = \"Account verwijderen\"", ui);
        Assert.Contains("Account verwijderen", privacy);
        Assert.DoesNotContain("[\"Unsubscribe.Title\"] = \"Account afmelden\"", extras);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Jobsy.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Jobsy.sln not found.");
    }
}
