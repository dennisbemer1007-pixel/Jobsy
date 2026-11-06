using Jobsy.Core.Rules;

namespace Jobsy.Web.KandidaatBanen;

/// <summary>View model for <see cref="Components.KandidaatBanen.KbFitPill"/>.</summary>
public sealed record KbFitView(bool GateOpen, int? Percent, KbFitBand? Band);
