using Jobsy.Core.Enums;
using Jobsy.Core.Scholen.MboHbo;

namespace Jobsy.Core.WerkgeverApk;

/// <summary>One forced-choice (A/B) or short check item from the CONCEPT CSV banks.</summary>
public sealed record WerkgeverApkAbItem(
    string Id,
    int Number,
    PupilMboHboBlock Block,
    int DisplayBlock,
    WerkgeverApkAbItemKind Kind,
    string Situation,
    string Heading,
    string? OptionA,
    string DimensionA,
    string? OptionB,
    string? DimensionB);