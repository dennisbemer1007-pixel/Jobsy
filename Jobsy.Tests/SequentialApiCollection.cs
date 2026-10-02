namespace Jobsy.Tests;

/// <summary>
/// Shared WebApplicationFactory / in-memory DB suites that must not interleave.
/// xunit.v3 requires an explicit <see cref="CollectionDefinitionAttribute"/> for
/// <c>DisableParallelization</c> (bare <c>[Collection("SequentialApi")]</c> alone is not enough).
/// </summary>
[CollectionDefinition("SequentialApi", DisableParallelization = true)]
public sealed class SequentialApiCollection;
