# ADR 0006: School roles and pupil codes

**Status:** Accepted (Dennis, 2026-09-29)  
**Context:** Lobsy voor scholen — separate school product inside Lobsy.

## Decision

- **D1.** Roles `SchoolAdmin` (8, nl: Schoolbeheerder) and `Teacher` (9, nl: Leraar). No Decaan role. Both are in `MfaPolicy.IsRequired` (mandatory 2FA; Microsoft/Google school login counts as 2FA per ADR 0005). Neither is an employer role.
- **D2.** Teachers see only classes assigned via `TeacherClassAssignment`. A SchoolAdmin may also be assigned as teacher of a class.
- **D3.** No pupil name/contact fields anywhere. Codes only; school keeps the name list outside Lobsy.
- **D4.** `SchoolPerCodeResultsEnabled` (default true) gates schoolbeheerder per-code short results; teachers unaffected.
- **D5.** Pupil codes: 6 chars from 30-char alphabet (no I/L/O/0/1), display `K7Q-M2P`, HMAC lookup + Data Protection for reprint.
- **D6.** Parental information confirmed per class before first test window.
- **D7.** Test window opened/closed by schoolbeheerder (all classes) or teacher (own classes).
- **D8.** Individual pupil data retained max one school year; aggregates (k ≥ 5) kept separately.
- **D9.** No AI / partner links / vacancies on pupil data.
- **D10.** School = controller, Lobsy = processor (verwerkersovereenkomst recorded by admin).

## Consequences

- Feature switch `SchoolsEnabled` (default false); retention always runs.
- Authorization is server-first via `ISchoolScopeService` and the rights matrix tests.
