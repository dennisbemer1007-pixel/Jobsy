-- Geanonimiseerde export voor psychometrische analyse (PostgreSQL / Npgsql).
-- Geen e-mail, naam, adres of UserId — alleen een eenmalige pseudocode per rij.
-- Alleen uitvoeren op acceptatie of een gecontroleerde kopie; NOOIT ongeanonimiseerd delen.
--
-- Gebruik:
--   psql "$DATABASE_URL" -f export_anonymized_responses.sql -A -F',' > lobsy_assessment_responses.csv
--
-- CSV-kolommen sluiten aan op analyze_responses.py (long format).

\set ON_ERROR_STOP on

WITH users AS (
    SELECT
        u."Id" AS user_id,
        -- Stable pseudonym: rotate yearly so re-linking across exports is harder
        encode(digest(u."Id"::text || ':lobsy-psych-export-v1:' || date_trunc('year', now())::text, 'sha256'), 'hex') AS respondent_hash,
        -- UI-taal staat niet op Users; vul aan via sessie/log indien beschikbaar. Default nl.
        COALESCE(
            NULLIF(lower(trim(u."PreferencesJson"::jsonb ->> 'language')), ''),
            'unknown'
        ) AS language,
        'unknown'::text AS gender,
        CASE
            WHEN u."DateOfBirth" IS NULL THEN 'unknown'
            WHEN date_part('year', age(u."DateOfBirth")) < 25 THEN '18-24'
            WHEN date_part('year', age(u."DateOfBirth")) < 35 THEN '25-34'
            WHEN date_part('year', age(u."DateOfBirth")) < 45 THEN '35-44'
            WHEN date_part('year', age(u."DateOfBirth")) < 55 THEN '45-54'
            ELSE '55+'
        END AS age_band
    FROM "Users" u
    WHERE u."Role" = 0  -- UserRole.Candidate
),
competence_items AS (
    SELECT
        u.respondent_hash,
        u.language,
        u.gender,
        u.age_band,
        'competence_quickscan'::text AS test_key,
        (kv.key)::int AS item_id,
        (kv.value)::int AS response_raw,
        c."Status" AS status,
        c."CompletedAtUtc" AS completed_at
    FROM users u
    INNER JOIN "CandidateCompetencies" c ON c."UserId" = u.user_id
    CROSS JOIN LATERAL jsonb_each_text(c."AnswersJson"::jsonb) AS kv(key, value)
    WHERE c."AnswersJson" IS NOT NULL AND c."AnswersJson" <> '{}'
),
career_items AS (
    SELECT
        u.respondent_hash,
        u.language,
        u.gender,
        u.age_band,
        'career_quickscan'::text AS test_key,
        (kv.key)::int AS item_id,
        (kv.value)::int AS response_raw,
        c."Status" AS status,
        c."CompletedAtUtc" AS completed_at
    FROM users u
    INNER JOIN "CandidateCareerInterests" c ON c."UserId" = u.user_id
    CROSS JOIN LATERAL jsonb_each_text(c."AnswersJson"::jsonb) AS kv(key, value)
    WHERE c."AnswersJson" IS NOT NULL AND c."AnswersJson" <> '{}'
),
culture_items AS (
    SELECT
        u.respondent_hash,
        u.language,
        u.gender,
        u.age_band,
        'culture_quickscan'::text AS test_key,
        (kv.key)::int AS item_id,
        (kv.value)::int AS response_raw,
        c."Status" AS status,
        c."CompletedAtUtc" AS completed_at
    FROM users u
    INNER JOIN "CandidateCulturePersonalityProfiles" c ON c."UserId" = u.user_id
    CROSS JOIN LATERAL jsonb_each_text(c."AnswersJson"::jsonb) AS kv(key, value)
    WHERE c."AnswersJson" IS NOT NULL AND c."AnswersJson" <> '{}'
),
values_items AS (
    SELECT
        u.respondent_hash,
        u.language,
        u.gender,
        u.age_band,
        'values_quickscan'::text AS test_key,
        (kv.key)::int AS item_id,
        (kv.value)::int AS response_raw,
        c."Status" AS status,
        c."CompletedAtUtc" AS completed_at
    FROM users u
    INNER JOIN "CandidateValuesProfiles" c ON c."UserId" = u.user_id
    CROSS JOIN LATERAL jsonb_each_text(c."AnswersJson"::jsonb) AS kv(key, value)
    WHERE c."AnswersJson" IS NOT NULL AND c."AnswersJson" <> '{}'
),
deep_items AS (
    SELECT
        u.respondent_hash,
        u.language,
        u.gender,
        u.age_band,
        ('deep_' || lower(d."AssessmentKind"::text)) AS test_key,
        (kv.key)::int AS item_id,
        (kv.value)::int AS response_raw,
        d."Status" AS status,
        d."CompletedAtUtc" AS completed_at
    FROM users u
    INNER JOIN "CandidateDeepAnalyses" d ON d."UserId" = u.user_id
    CROSS JOIN LATERAL jsonb_each_text(d."AnswersJson"::jsonb) AS kv(key, value)
    WHERE d."AnswersJson" IS NOT NULL AND d."AnswersJson" <> '{}'
),
all_rows AS (
    SELECT * FROM competence_items
    UNION ALL SELECT * FROM career_items
    UNION ALL SELECT * FROM culture_items
    UNION ALL SELECT * FROM values_items
    UNION ALL SELECT * FROM deep_items
)
SELECT
    respondent_hash,
    language,
    gender,
    age_band,
    test_key,
    item_id,
    response_raw,
    status,
    completed_at
FROM all_rows
ORDER BY test_key, respondent_hash, item_id;
