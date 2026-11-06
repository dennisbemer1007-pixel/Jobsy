namespace Jobsy.Core.Contracts;

public sealed record ResetCandidateProfileRequest(string ConfirmPhrase);

public sealed record ResetCandidateProfileResponse(string Message, string RedirectPath);
