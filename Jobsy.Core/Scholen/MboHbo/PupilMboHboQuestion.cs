namespace Jobsy.Core.Scholen.MboHbo;

public sealed record PupilMboHboQuestion(
    PupilMboHboBlock Block,
    int Number,
    string Id,
    string Dimension,
    string Question,
    string Scenario,
    string? EscoOccupationName,
    string? EscoUri,
    string? EscoMatch);
