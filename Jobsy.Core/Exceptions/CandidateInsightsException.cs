namespace Jobsy.Core.Exceptions;

public sealed class CandidateInsightsException : Exception
{
    public CandidateInsightsException(string code, string message, int statusCode = 400)
        : base(message)
    {
        Code = code;
        StatusCode = statusCode;
    }

    public string Code { get; }
    public int StatusCode { get; }
}
