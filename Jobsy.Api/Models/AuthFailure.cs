namespace Jobsy.Api.Models;

public sealed record AuthFailure(string Code, DateTime? RetryAtUtc = null);
