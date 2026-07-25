namespace Jobsy.Web.Auth;

public class AuthOptions
{
    public const string SectionName = "Authentication";

    public EntraOptions Entra { get; set; } = new();
    public GoogleOptions Google { get; set; } = new();
    public List<DemoUserOptions> DemoUsers { get; set; } = [];
}

public class EntraOptions
{
    public string? TenantId { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string CallbackPath { get; set; } = "/signin-entra";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret);
}

public class GoogleOptions
{
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string CallbackPath { get; set; } = "/signin-google";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret);
}

public class DemoUserOptions
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = "Candidate";
    public string? CompanyId { get; set; }
    public string? CompanyIds { get; set; }
}
