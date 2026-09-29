using Jobsy.Core.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace Jobsy.Infrastructure.Scholen;

/// <summary>Requires scheme Pupil + required claims + live SessionVersion + active school.</summary>
public sealed class PupilSessionRequirement : IAuthorizationRequirement;
