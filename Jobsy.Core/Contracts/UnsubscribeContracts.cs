namespace Jobsy.Core.Contracts;

public record UnsubscribeReasonOptionDto(string Code, string Label, bool RequiresOtherText);

public record RequestUnsubscribeRequest(string ReasonCode, string? ReasonOther = null);

public record ConfirmUnsubscribeRequest(string VerificationCode);

public record RequestUnsubscribeResponse(string Message, DateTime ExpiresAtUtc);
