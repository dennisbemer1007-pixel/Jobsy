using Jobsy.Core.Entities;
using Jobsy.Core.Entities.Scholen;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

/// <summary>
/// EF metadata catalog of foreign keys to <see cref="User"/> that profile reset / account cleanup must handle.
/// </summary>
internal static class CandidateUserRelationCatalog
{
    internal enum HandlingKind
    {
        CleanupRequired,
        KeepForLogin,
        ExcludeNonCandidate
    }

    internal sealed record Relation(
        Type EntityClrType,
        string ForeignKeyProperty,
        HandlingKind Kind);

    private static readonly HashSet<Type> KeepForLoginTypes =
    [
        typeof(LocalAuthCredential),
        typeof(UserExternalLogin)
    ];

    private static readonly HashSet<Type> ExcludeTypes =
    [
        typeof(UserCompany),
        typeof(AmbassadeurProfile),
        typeof(SalesManagerProfile),
        typeof(PartnerAffiliateProfile),
        typeof(SalesSelfBillingConsent),
        typeof(SalesIbanChangePending),
        typeof(TokenRequest),
        typeof(CandidateInsightsUnlock),
        typeof(CandidateInsightsUnlockRequest),
        typeof(CompanyRegistration),
        typeof(CompanyManualVerificationRequest),
        typeof(CompanyVerificationDecision),
        typeof(CompanyAccessRequest),
        typeof(EstablishmentTakeoverRequest),
        typeof(AdminAuditEvent),
        typeof(TeacherClassAssignment),
        typeof(SchoolStaffInvite),
        typeof(School),
        typeof(SchoolClass)
    ];

    internal static IReadOnlyList<Relation> Discover(JobsyDbContext db)
    {
        var userType = db.Model.FindEntityType(typeof(User))
                      ?? throw new InvalidOperationException("User entity missing from model.");

        var relations = new List<Relation>();
        foreach (var entityType in db.Model.GetEntityTypes())
        {
            if (entityType.ClrType == typeof(User))
            {
                continue;
            }

            foreach (var fk in entityType.GetForeignKeys())
            {
                if (!ReferenceEquals(fk.PrincipalEntityType, userType))
                {
                    continue;
                }

                if (fk.Properties.Count != 1)
                {
                    continue;
                }

                var propertyName = fk.Properties[0].Name;
                relations.Add(new Relation(
                    entityType.ClrType,
                    propertyName,
                    Classify(entityType.ClrType, propertyName)));
            }
        }

        return relations
            .OrderBy(r => r.EntityClrType.Name, StringComparer.Ordinal)
            .ThenBy(r => r.ForeignKeyProperty, StringComparer.Ordinal)
            .ToList();
    }

    internal static IReadOnlyList<Relation> CandidateScopedRelations(JobsyDbContext db)
        => Discover(db).Where(r => r.Kind == HandlingKind.CleanupRequired).ToList();

    private static HandlingKind Classify(Type entityType, string fkProperty)
    {
        if (KeepForLoginTypes.Contains(entityType))
        {
            return HandlingKind.KeepForLogin;
        }

        if (ExcludeTypes.Contains(entityType))
        {
            return HandlingKind.ExcludeNonCandidate;
        }

        if (fkProperty is "UserId"
            or "CandidateUserId"
            or "SenderUserId"
            or "ViewerUserId"
            or "SubjectUserId"
            or "DismissedByUserId"
            or "ReporterUserId")
        {
            return HandlingKind.CleanupRequired;
        }

        if (entityType == typeof(PersonalDataAccessLog) && fkProperty == "ActorUserId")
        {
            return HandlingKind.CleanupRequired;
        }

        return HandlingKind.ExcludeNonCandidate;
    }

    /// <summary>Entity types touched by <see cref="CandidateOwnedDataCleanup"/> (for regression tests).</summary>
    internal static HashSet<Type> CleanupCoverageEntityTypes { get; } =
    [
        typeof(AfterMatchReminderPreference),
        typeof(Application),
        typeof(ApplicationMessage),
        typeof(AssessmentResearchAttempt),
        typeof(CandidateActionToken),
        typeof(CandidateAssessmentAdjustment),
        typeof(CandidateAssessmentAttempt),
        typeof(CandidateCareerGeneration),
        typeof(CandidateCareerInterest),
        typeof(CandidateCareerPlan),
        typeof(CandidateCareerStepProgress),
        typeof(CandidateCompetency),
        typeof(CandidateCulturePersonalityProfile),
        typeof(CandidateDeepAnalysis),
        typeof(CandidateDiplomaEvaluation),
        typeof(CandidateExternalVacancy),
        typeof(CandidateKandidatenkaartSettings),
        typeof(CandidateMatchSnapshot),
        typeof(CandidateOnboarding),
        typeof(CandidatePrivatePreferences),
        typeof(CandidateProactiveMatchingSettings),
        typeof(CandidateReference),
        typeof(CandidateReminderPreference),
        typeof(CandidateRoleFitCheck),
        typeof(CandidateTestReflection),
        typeof(CandidateUploadedCv),
        typeof(CandidateVacancyCultureFit),
        typeof(CandidateValuesProfile),
        typeof(CandidateWhoAmIProfile),
        typeof(ComebackReminderLog),
        typeof(ConsumerPurchaseInvoice),
        typeof(ContentReport),
        typeof(DeepAnalysisCheckout),
        typeof(DeviceLoginHandoff),
        typeof(DismissedVestigingSuggestion),
        typeof(InterviewReminder),
        typeof(KandidatenkaartInviteDelivery),
        typeof(MfaTrustedDevice),
        typeof(OneTimeLink),
        typeof(PassportAccessLog),
        typeof(PassportPartnerCandidateLink),
        typeof(PersonalDataAccessLog),
        typeof(PhoneVerificationChallenge),
        typeof(PlatformFeedback),
        typeof(ProactiveMatchSuggestion),
        typeof(ReferenceConfirmation),
        typeof(ReferenceConfirmationConsentLog),
        typeof(ReferenceMisuseReport),
        typeof(SiteVisit),
        typeof(TalentContactRequest),
        typeof(TrainingClick),
        typeof(TrainingEnrollment),
        typeof(UserDeviceSession),
        typeof(UserNotification),
        typeof(VacancyClick),
        typeof(VacancyLike),
        typeof(VacancySearchImpression),
        typeof(VacancyShare),
        typeof(WebPushSubscription),
        typeof(WorkExperienceReferenceRequest),
        typeof(WorkInitiative),
        typeof(WorkInitiativeDraft),
        typeof(SupportAccessGrant)
    ];
}
