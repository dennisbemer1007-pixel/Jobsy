using Jobsy.Web.Localization;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;

namespace Jobsy.Tests.Errors;

/// <summary>E7: the mapping table, and the guarantee that no exception text ever comes back.</summary>
public class UserFacingErrorTests
{
    private const string Secret = "Secret boom detail from the database connection string.";

    [Theory]
    [InlineData(ApiErrorException.RateLimited, "Common.Error.RateLimited")]
    [InlineData(ApiErrorException.NotFound, "Common.Error.NotFound")]
    [InlineData(ApiErrorException.Forbidden, "Common.Error.Forbidden")]
    [InlineData(ApiErrorException.Validation, "Common.Error.Validation")]
    [InlineData(ApiErrorException.Maintenance, "Status.Maintenance.Short")]
    [InlineData(ApiErrorException.Unknown, "Common.Error.TryAgain")]
    [InlineData("something_new", "Common.Error.TryAgain")]
    public void Api_error_codes_map_to_their_key(string code, string expectedKey)
        => Assert.Equal(expectedKey, UserFacingError.MessageKeyFor(new ApiErrorException(code)));

    [Fact]
    public void Network_and_timeout_map_to_the_network_key()
    {
        Assert.Equal("Common.Error.Network", UserFacingError.MessageKeyFor(new HttpRequestException(Secret)));
        Assert.Equal("Common.Error.Network", UserFacingError.MessageKeyFor(new TaskCanceledException(Secret)));
        Assert.Equal("Common.Error.Network", UserFacingError.MessageKeyFor(new TimeoutException(Secret)));
    }

    [Fact]
    public void Anything_else_maps_to_try_again()
        => Assert.Equal(
            "Common.Error.TryAgain",
            UserFacingError.MessageKeyFor(new InvalidOperationException(Secret)));

    [Fact]
    public void Describe_never_returns_the_exception_message()
    {
        var helper = Create();

        foreach (var ex in Exceptions())
        {
            var text = helper.Describe(ex);
            Assert.DoesNotContain(Secret, text, StringComparison.Ordinal);
            Assert.DoesNotContain("Exception", text, StringComparison.OrdinalIgnoreCase);
            Assert.False(string.IsNullOrWhiteSpace(text));
        }
    }

    [Fact]
    public void Describe_speaks_the_visitors_language()
    {
        Assert.Equal(
            UiStrings.Get("Common.Error.Network", "nl"),
            Create("nl").Describe(new HttpRequestException(Secret)));
        Assert.Equal(
            UiStrings.Get("Common.Error.Network", "ar"),
            Create("ar").Describe(new HttpRequestException(Secret)));
        Assert.NotEqual(
            UiStrings.Get("Common.Error.Network", "nl"),
            UiStrings.Get("Common.Error.Network", "ar"));
    }

    [Fact]
    public void From_returns_a_support_code_and_keeps_the_one_the_api_minted()
    {
        var helper = Create();

        var generated = helper.From(new InvalidOperationException(Secret));
        Assert.True(Jobsy.Web.Diagnostics.SupportCode.IsValid(generated.SupportCode));
        Assert.Null(generated.ApiUserMessage);

        var fromApi = helper.From(new ApiErrorException(ApiErrorException.RateLimited, 429, "LB-7Q3K", 42));
        Assert.Equal("LB-7Q3K", fromApi.SupportCode);
        Assert.Equal("Common.Error.RateLimited", fromApi.MessageKey);
    }

    [Fact]
    public void A_validation_message_written_for_users_is_shown_as_is()
    {
        var helper = Create();
        var error = new ApiErrorException(ApiErrorException.Validation, 400, userMessage: "Vul je postcode in.");

        Assert.Equal("Vul je postcode in.", helper.Describe(error));
    }

    [Fact]
    public void From_logs_the_exception_once_with_the_support_code()
    {
        var provider = new RecordingLoggerProvider();
        using var factory = LoggerFactory.Create(builder => builder.AddProvider(provider));
        var helper = new UserFacingError(
            factory.CreateLogger<UserFacingError>(),
            CultureStateFor("nl"));

        var message = helper.From(new InvalidOperationException(Secret));

        Assert.Contains(
            provider.Messages,
            line => line.Contains(message.SupportCode!, StringComparison.Ordinal));
    }

    private static IEnumerable<Exception> Exceptions()
    {
        yield return new InvalidOperationException(Secret);
        yield return new HttpRequestException(Secret);
        yield return new TaskCanceledException(Secret);
        yield return new ApiErrorException(ApiErrorException.RateLimited, 429, "LB-7Q3K", 30);
        yield return new ApiErrorException("weird_code", 500);
    }

    private static UserFacingError Create(string language = "nl")
        => new(NullLogger<UserFacingError>.Instance, CultureStateFor(language));

    private static CultureState CultureStateFor(string language)
    {
        var state = new CultureState(
            new NoopJsRuntime(),
            new EmptyServiceProvider(),
            new AnonymousAuthStateProvider());
        state.InitializeFromLanguage(language);
        return state;
    }

    private sealed class NoopJsRuntime : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => ValueTask.FromResult<TValue>(default!);

        public ValueTask<TValue> InvokeAsync<TValue>(
            string identifier,
            CancellationToken cancellationToken,
            object?[]? args)
            => ValueTask.FromResult<TValue>(default!);
    }

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }

    private sealed class AnonymousAuthStateProvider : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new System.Security.Claims.ClaimsPrincipal()));
    }
}
