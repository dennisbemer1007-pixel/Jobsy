-- Idempotent prep for local real-stack proactive E2E (JobsyCi + demo users).
BEGIN;

INSERT INTO "PlatformFeatureSettings" (
    "Id",
    "VacancyContentModerationEnabled",
    "AuthenticatorEnabled",
    "InactiveCompanyDays",
    "SessionInactivityTimeoutMinutes",
    "SupportAccessNotifyAdmins",
    "SupportAccessNotifySubject",
    "EmployersEnabled",
    "ProactiveMatchingEnabled",
    "UpdatedAtUtc")
VALUES (
    'aaaaaaaa-0000-0000-0000-000000000001',
    true,
    true,
    120,
    30,
    false,
    false,
    true,
    true,
    NOW() AT TIME ZONE 'UTC')
ON CONFLICT ("Id") DO UPDATE SET
    "EmployersEnabled" = EXCLUDED."EmployersEnabled",
    "ProactiveMatchingEnabled" = EXCLUDED."ProactiveMatchingEnabled",
    "UpdatedAtUtc" = EXCLUDED."UpdatedAtUtc";

-- Demo kandidaat: talentpool + profiel voor matching/shortlist
UPDATE "Users"
SET "OpenForWork" = true,
    "TalentPoolConsentAt" = COALESCE("TalentPoolConsentAt", NOW() AT TIME ZONE 'UTC'),
    "TalentPoolConsentVersion" = COALESCE("TalentPoolConsentVersion", '2026-09-26'),
    "CandidateHowToCompletedAt" = COALESCE("CandidateHowToCompletedAt", NOW() AT TIME ZONE 'UTC')
WHERE "Id" = 'aaaaaaaa-1111-1111-1111-111111111111';

-- Tweede kandidaat: wel profiel, niet beschikbaar (shortlist-filter)
UPDATE "Users"
SET "OpenForWork" = false,
    "TalentPoolConsentAt" = COALESCE("TalentPoolConsentAt", NOW() AT TIME ZONE 'UTC'),
    "TalentPoolConsentVersion" = COALESCE("TalentPoolConsentVersion", '2026-09-26')
WHERE "Id" = 'aaaaaaaa-3333-3333-3333-333333333333';

-- Den Haag kandidaat: beschikbaar + consent (extra shortlist entry)
UPDATE "Users"
SET "OpenForWork" = true,
    "TalentPoolConsentAt" = COALESCE("TalentPoolConsentAt", NOW() AT TIME ZONE 'UTC'),
    "TalentPoolConsentVersion" = COALESCE("TalentPoolConsentVersion", '2026-09-26')
WHERE "Id" = 'aaaaaaaa-2222-2222-2222-222222222222';

INSERT INTO "CandidateCompetencies" (
    "Id", "UserId", "Status",
    "SamenwerkenPercent", "ResultaatgerichtheidPercent", "StressbestendigheidPercent",
    "InnovatiePercent", "ExtraversiePercent",
    "AnswersJson", "RiasecTagsJson", "MatchTagsJson",
    "CreatedAtUtc", "UpdatedAtUtc", "CompletedAtUtc")
SELECT
    'bbbbbbbb-1111-1111-1111-111111111111',
    'aaaaaaaa-1111-1111-1111-111111111111',
    'Completed',
    88, 82, 75, 70, 78,
    '{}', '[]', '["Samenwerken","Resultaatgericht"]',
    NOW() AT TIME ZONE 'UTC', NOW() AT TIME ZONE 'UTC', NOW() AT TIME ZONE 'UTC'
WHERE NOT EXISTS (
    SELECT 1 FROM "CandidateCompetencies" WHERE "UserId" = 'aaaaaaaa-1111-1111-1111-111111111111');

INSERT INTO "CandidateCareerInterests" (
    "Id", "UserId", "Status", "HollandCode",
    "RealisticPercent", "InvestigativePercent", "ArtisticPercent",
    "SocialPercent", "EnterprisingPercent", "ConventionalPercent",
    "AnswersJson", "RiasecTagsJson", "MatchTagsJson",
    "CreatedAtUtc", "UpdatedAtUtc", "CompletedAtUtc")
SELECT
    'bbbbbbbb-2222-2222-2222-222222222222',
    'aaaaaaaa-1111-1111-1111-111111111111',
    'Completed', 'SR',
    70, 55, 40, 81, 60, 50,
    '{}', '["Realistic","Social"]', '["Realistic","Social","Mensen helpen"]',
    NOW() AT TIME ZONE 'UTC', NOW() AT TIME ZONE 'UTC', NOW() AT TIME ZONE 'UTC'
WHERE NOT EXISTS (
    SELECT 1 FROM "CandidateCareerInterests" WHERE "UserId" = 'aaaaaaaa-1111-1111-1111-111111111111');

INSERT INTO "CandidateCompetencies" (
    "Id", "UserId", "Status",
    "SamenwerkenPercent", "ResultaatgerichtheidPercent", "StressbestendigheidPercent",
    "InnovatiePercent", "ExtraversiePercent",
    "AnswersJson", "MatchTagsJson",
    "CreatedAtUtc", "UpdatedAtUtc", "CompletedAtUtc")
SELECT
    'bbbbbbbb-3333-3333-3333-333333333333',
    'aaaaaaaa-2222-2222-2222-222222222222',
    'Completed', 80, 78, 72, 68, 74, '{}', '["Samenwerken"]',
    NOW() AT TIME ZONE 'UTC', NOW() AT TIME ZONE 'UTC', NOW() AT TIME ZONE 'UTC'
WHERE NOT EXISTS (
    SELECT 1 FROM "CandidateCompetencies" WHERE "UserId" = 'aaaaaaaa-2222-2222-2222-222222222222');

INSERT INTO "CandidateCareerInterests" (
    "Id", "UserId", "Status", "HollandCode",
    "RealisticPercent", "SocialPercent",
    "AnswersJson", "RiasecTagsJson", "MatchTagsJson",
    "CreatedAtUtc", "UpdatedAtUtc", "CompletedAtUtc")
SELECT
    'bbbbbbbb-4444-4444-4444-444444444444',
    'aaaaaaaa-2222-2222-2222-222222222222',
    'Completed', 'RS', 72, 78, '{}', '["Realistic","Social"]', '["Realistic","Social"]',
    NOW() AT TIME ZONE 'UTC', NOW() AT TIME ZONE 'UTC', NOW() AT TIME ZONE 'UTC'
WHERE NOT EXISTS (
    SELECT 1 FROM "CandidateCareerInterests" WHERE "UserId" = 'aaaaaaaa-2222-2222-2222-222222222222');

-- Niet-beschikbare kandidaat heeft wél profiel (mag niet in shortlist)
INSERT INTO "CandidateCompetencies" (
    "Id", "UserId", "Status",
    "SamenwerkenPercent", "ResultaatgerichtheidPercent", "StressbestendigheidPercent",
    "InnovatiePercent", "ExtraversiePercent",
    "AnswersJson", "MatchTagsJson",
    "CreatedAtUtc", "UpdatedAtUtc", "CompletedAtUtc")
SELECT
    'bbbbbbbb-5555-5555-5555-555555555555',
    'aaaaaaaa-3333-3333-3333-333333333333',
    'Completed', 90, 85, 80, 75, 70, '{}', '["Samenwerken"]',
    NOW() AT TIME ZONE 'UTC', NOW() AT TIME ZONE 'UTC', NOW() AT TIME ZONE 'UTC'
WHERE NOT EXISTS (
    SELECT 1 FROM "CandidateCompetencies" WHERE "UserId" = 'aaaaaaaa-3333-3333-3333-333333333333');

-- E2E herhaalbaar: schone proactieve tips + inbox voor demo-kandidaat
DELETE FROM "ProactiveMatchSuggestions"
WHERE "CandidateUserId" = 'aaaaaaaa-1111-1111-1111-111111111111';

DELETE FROM "UserNotifications"
WHERE "UserId" = 'aaaaaaaa-1111-1111-1111-111111111111'
  AND "Category" IN ('ProactiveMatch', 'TalentContact');

DELETE FROM "TalentContactRequests"
WHERE "CandidateUserId" = 'aaaaaaaa-1111-1111-1111-111111111111'
  AND "CompanyId" = '11111111-1111-1111-1111-111111111111';

UPDATE "CandidateProactiveMatchingSettings"
SET "LastProcessedAtUtc" = NULL,
    "Enabled" = true,
    "ConsentAcceptedAtUtc" = COALESCE("ConsentAcceptedAtUtc", NOW() AT TIME ZONE 'UTC'),
    "ConsentVersion" = COALESCE("ConsentVersion", 'proactive-matching-2026-01')
WHERE "UserId" = 'aaaaaaaa-1111-1111-1111-111111111111';

COMMIT;
