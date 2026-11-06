namespace Jobsy.Core.Enums;

public enum TrainingProviderKind
{
    NationalAffiliate = 0,
    RegionalPartner = 1
}

public enum TrainingNetwork
{
    Direct = 0,
    Awin = 1,
    Daisycon = 2
}

public enum TrainingConversionKind
{
    Lead = 0,
    Started = 1
}

public enum TrainingOfferType
{
    Opleiding = 0,
    Cursus = 1,
    Workshop = 2
}

public enum TrainingDurationUnit
{
    Hours = 0,
    Days = 1,
    Weeks = 2,
    Months = 3,
    Years = 4
}

public enum TrainingDeliveryMode
{
    Online = 0,
    OnSite = 1,
    Blended = 2
}

/// <summary>How a partner publishes offers into Lobsy.</summary>
public enum TrainingPartnerFeedKind
{
    None = 0,
    Json = 1,
    Csv = 2,
    Api = 3
}

public enum TrainingOfferSection
{
    NextStep = 0,
    Growth = 1,
    General = 2
}

public enum TrainingEnrollmentStatus
{
    Saved = 0,
    InProgress = 1,
    Completed = 2,
    Verified = 3
}

public enum TrainingCertificateVerificationSource
{
    PartnerWebhook = 0,
    AdminReview = 1,
    CandidateUpload = 2
}
