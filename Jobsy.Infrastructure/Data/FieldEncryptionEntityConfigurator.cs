using Jobsy.Core.Entities;
using Jobsy.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Data;

internal static class FieldEncryptionEntityConfigurator
{
    private const int EncryptedStringMax = 4096;

    internal static void Apply(ModelBuilder modelBuilder, IFieldEncryptionService crypto)
    {
        var enc = new FieldEncryptionConverters.EncryptedStringConverter(crypto);
        var encReq = new FieldEncryptionConverters.EncryptedRequiredStringConverter(crypto);
        var encDob = new FieldEncryptionConverters.EncryptedDateOnlyConverter(crypto);
        var encBytes = new FieldEncryptionConverters.EncryptedBytesConverter(crypto);

        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(e => e.Email).HasConversion(encReq).HasMaxLength(EncryptedStringMax);
            entity.Property(e => e.PhoneNumber).HasConversion(enc).HasMaxLength(512);
            entity.Property(e => e.PhoneVerifiedE164).HasConversion(enc).HasMaxLength(512);
            entity.Property(e => e.ParentalConsentEmail).HasConversion(enc).HasMaxLength(EncryptedStringMax);
            entity.Property(e => e.PreferencesJson).HasConversion(enc).HasMaxLength(16_000);
            entity.Property(e => e.UnsubscribeReasonOther).HasConversion(enc).HasMaxLength(EncryptedStringMax);
            entity.Property(e => e.DateOfBirth).HasConversion(encDob).HasColumnType("text");
            entity.Property(e => e.EmailLookupHash).HasMaxLength(64);
            entity.HasIndex(e => e.EmailLookupHash)
                .IsUnique()
                .HasFilter("\"EmailLookupHash\" IS NOT NULL AND \"EmailLookupHash\" <> ''");
            entity.HasIndex(e => e.Email).IsUnique(false);
        });

        modelBuilder.Entity<LocalAuthCredential>(entity =>
        {
            entity.Property(e => e.Email).HasConversion(encReq).HasMaxLength(EncryptedStringMax);
            entity.Property(e => e.EmailLookupHash).HasMaxLength(64);
            entity.HasIndex(e => e.EmailLookupHash)
                .IsUnique()
                .HasFilter("\"EmailLookupHash\" IS NOT NULL AND \"EmailLookupHash\" <> ''");
            entity.HasIndex(e => e.Email).IsUnique(false);
        });

        modelBuilder.Entity<Application>(entity =>
        {
            entity.Property(e => e.CandidateEmail).HasConversion(encReq).HasMaxLength(EncryptedStringMax);
            entity.Property(e => e.CandidateAddress).HasConversion(enc).HasMaxLength(EncryptedStringMax);
            entity.Property(e => e.SnapshotPhoneNumber).HasConversion(enc).HasMaxLength(512);
            entity.Property(e => e.SchoolEmail).HasConversion(enc).HasMaxLength(EncryptedStringMax);
            entity.Property(e => e.StudentNumber).HasConversion(enc).HasMaxLength(512);
            entity.Property(e => e.Motivation).HasConversion(enc).HasMaxLength(16_000);
            entity.Property(e => e.SnapshotDateOfBirth).HasConversion(encDob).HasColumnType("text");
            entity.Property(e => e.CandidateEmailLookupHash).HasMaxLength(64);
            entity.HasIndex(e => e.CandidateEmailLookupHash);
        });

        modelBuilder.Entity<SalesManagerProfile>(entity =>
        {
            entity.Property(e => e.Address).HasConversion(enc).HasMaxLength(EncryptedStringMax);
        });

        modelBuilder.Entity<AmbassadeurProfile>(entity =>
        {
            entity.Property(e => e.Address).HasConversion(enc).HasMaxLength(EncryptedStringMax);
        });

        modelBuilder.Entity<PartnerAffiliateProfile>(entity =>
        {
            entity.Property(e => e.Address).HasConversion(enc).HasMaxLength(EncryptedStringMax);
        });

        modelBuilder.Entity<TalentContactRequest>(entity =>
        {
            entity.Property(e => e.Message).HasConversion(encReq).HasMaxLength(16_000);
        });

        modelBuilder.Entity<CandidatePrivatePreferences>(entity =>
        {
            entity.Property(e => e.CustomDislikesJson).HasConversion(encReq).HasMaxLength(EncryptedStringMax);
        });

        modelBuilder.Entity<CandidateUploadedCv>(entity =>
        {
            entity.Property(e => e.Content).HasConversion(encBytes);
        });

        modelBuilder.Entity<ApplicationUploadedCv>(entity =>
        {
            entity.Property(e => e.Content).HasConversion(encBytes);
        });

        modelBuilder.Entity<CandidateDeepAnalysis>(entity =>
        {
            entity.Property(e => e.AnswersJson).HasConversion(encReq).HasMaxLength(512_000);
            entity.Property(e => e.ReportJson).HasConversion(encReq).HasMaxLength(512_000);
        });
    }
}
