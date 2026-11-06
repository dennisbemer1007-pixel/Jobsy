namespace Jobsy.Tests.E2e;

/// <summary>Serializes functional E2E so one shared login/storage state and less CI stack contention.</summary>
[CollectionDefinition("FunctionalE2e", DisableParallelization = true)]
public sealed class FunctionalE2eCollection;
