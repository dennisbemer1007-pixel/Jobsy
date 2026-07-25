namespace Jobsy.Api.Models;

public sealed class MockInterviewRequest
{
    public Guid VacancyId { get; set; }

    /// <summary>Prior turns only (user + assistant). Empty starts a new practice session.</summary>
    public List<MockInterviewMessageDto> Messages { get; set; } = [];
}

public sealed class MockInterviewMessageDto
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public sealed record MockInterviewResponseDto(
    string Reply,
    bool UsedAi,
    string Disclaimer);
