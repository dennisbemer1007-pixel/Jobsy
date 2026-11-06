namespace Jobsy.Web.Components.Candidate;

/// <summary>
/// Soft→hard shell strength from proof counts (employers, education, certificates, references, own CV).
/// Pure rules — no storage. UI localizes hint keys.
/// </summary>
public static class ProofStrengthRules
{
    public const int MaxSegments = 8;

    public enum ProofKind
    {
        Employer,
        Education,
        Certificate,
        Reference,
        OwnCv
    }

    public sealed record ProofSnapshot(
        int Employers,
        int Educations,
        int Certificates,
        int References,
        bool HasOwnCv);

    public sealed record MissingItem(ProofKind Kind, int Count);

    public sealed record StrengthView(
        int Filled,
        int Max,
        IReadOnlyList<MissingItem> Missing);

    public static int Count(ProofSnapshot s)
    {
        var total = Math.Max(0, s.Employers)
                    + Math.Max(0, s.Educations)
                    + Math.Max(0, s.Certificates)
                    + Math.Max(0, s.References)
                    + (s.HasOwnCv ? 1 : 0);
        return Math.Min(MaxSegments, total);
    }

    public static StrengthView Build(ProofSnapshot s)
    {
        var filled = Count(s);
        var missing = SuggestMissing(s, MaxSegments - filled);
        return new StrengthView(filled, MaxSegments, missing);
    }

    /// <summary>
    /// Priority for "next missing" copy: reference → certificate → employer → education → own CV.
    /// </summary>
    public static IReadOnlyList<MissingItem> SuggestMissing(ProofSnapshot s, int need)
    {
        if (need <= 0)
        {
            return [];
        }

        var result = new List<MissingItem>();
        void Take(ProofKind kind, int available)
        {
            if (need <= 0 || available <= 0)
            {
                return;
            }

            var n = Math.Min(need, available);
            result.Add(new MissingItem(kind, n));
            need -= n;
        }

        Take(ProofKind.Reference, Math.Max(0, 1 - s.References));
        Take(ProofKind.Certificate, Math.Max(0, 1 - s.Certificates));
        Take(ProofKind.Employer, Math.Max(0, 1 - s.Employers));
        Take(ProofKind.Education, Math.Max(0, 1 - s.Educations));
        Take(ProofKind.OwnCv, s.HasOwnCv ? 0 : 1);

        Take(ProofKind.Reference, Math.Max(0, 3 - Math.Max(s.References, result.Any(m => m.Kind == ProofKind.Reference) ? 1 : 0)));
        Take(ProofKind.Certificate, need);
        Take(ProofKind.Employer, need);
        Take(ProofKind.Education, need);
        Take(ProofKind.OwnCv, s.HasOwnCv ? 0 : 1);

        return result;
    }

    public static string NounKey(ProofKind kind) => kind switch
    {
        ProofKind.Reference => "Passport.Proof.Noun.Reference",
        ProofKind.Certificate => "Passport.Proof.Noun.Certificate",
        ProofKind.Employer => "Passport.Proof.Noun.Employer",
        ProofKind.Education => "Passport.Proof.Noun.Education",
        _ => "Passport.Proof.Noun.Cv"
    };

    public static string HintKey(IReadOnlyList<MissingItem> missing)
    {
        if (missing.Count == 0)
        {
            return "Passport.Proof.HintHard";
        }

        if (missing.Count == 1)
        {
            return missing[0].Kind switch
            {
                ProofKind.Reference => "Passport.Proof.HintOneReference",
                ProofKind.Certificate => "Passport.Proof.HintOneCertificate",
                ProofKind.Employer => "Passport.Proof.HintOneEmployer",
                ProofKind.Education => "Passport.Proof.HintOneEducation",
                _ => "Passport.Proof.HintOneCv"
            };
        }

        return "Passport.Proof.HintTwo";
    }
}
