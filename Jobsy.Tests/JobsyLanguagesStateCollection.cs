namespace Jobsy.Tests;

/// <summary>
/// Tests that mutate <see cref="Jobsy.Core.Localization.JobsyLanguages"/> static enabled cultures must not run in parallel.
/// </summary>
[CollectionDefinition("JobsyLanguagesState", DisableParallelization = true)]
public sealed class JobsyLanguagesStateCollection;
