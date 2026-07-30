using Jobsy.Core.Entities;
using Jobsy.Core.Rules;
using Jobsy.Core.ValueObjects;
using Xunit;

namespace Jobsy.Tests;

public class EmployerContactPreferenceRulesTests
{
    [Fact]
    public void Validate_when_direct_off_allows_empty_channels()
    {
        var error = EmployerContactPreferenceRules.Validate(
            directContactEnabled: false,
            preferMail: false,
            preferPhone: false,
            preferWhatsApp: false);
        Assert.Null(error);
    }

    [Fact]
    public void Validate_when_direct_on_requires_at_least_one_channel()
    {
        var error = EmployerContactPreferenceRules.Validate(
            directContactEnabled: true,
            preferMail: false,
            preferPhone: false,
            preferWhatsApp: false);
        Assert.Equal(EmployerContactPreferenceRules.AtLeastOneChannelRequired, error);
    }

    [Fact]
    public void Validate_when_mail_selected_requires_email()
    {
        var error = EmployerContactPreferenceRules.Validate(
            directContactEnabled: true,
            preferMail: true,
            preferPhone: false,
            preferWhatsApp: false,
            contactEmail: null);
        Assert.Equal(EmployerContactPreferenceRules.MailRequiresEmail, error);
    }

    [Fact]
    public void Validate_rejects_invalid_email()
    {
        var error = EmployerContactPreferenceRules.Validate(
            directContactEnabled: true,
            preferMail: true,
            preferPhone: false,
            preferWhatsApp: false,
            contactEmail: "not-an-email");
        Assert.Equal(EmployerContactPreferenceRules.InvalidEmail, error);
    }

    [Fact]
    public void Validate_ok_with_whatsapp_and_phone_number()
    {
        var error = EmployerContactPreferenceRules.Validate(
            directContactEnabled: true,
            preferMail: false,
            preferPhone: false,
            preferWhatsApp: true,
            contactPhone: "+31612345678");
        Assert.Null(error);
    }

    [Fact]
    public void NormalizePhoneDigits_converts_nl_06_to_country_code()
    {
        Assert.Equal("31612345678", EmployerContactPreferenceRules.NormalizePhoneDigits("06-12345678"));
        Assert.Equal("31612345678", EmployerContactPreferenceRules.NormalizePhoneDigits("+31 6 12345678"));
    }

    [Fact]
    public void NormalizePhoneDigits_rejects_non_digit_junk()
    {
        Assert.Null(EmployerContactPreferenceRules.NormalizePhoneDigits("N/A"));
        Assert.Null(EmployerContactPreferenceRules.NormalizePhoneDigits("abc"));
    }

    [Fact]
    public void Resolve_inherits_company_when_no_override()
    {
        var company = Company("org@example.com", "0612345678");
        company.DirectContactEnabled = true;
        company.ContactPreferWhatsApp = true;

        var vacancy = new Vacancy { OverrideContactPreference = false };
        var effective = EmployerContactPreferenceRules.Resolve(company, vacancy);

        Assert.True(effective.Available);
        Assert.True(effective.OfferWhatsApp);
        Assert.False(effective.OfferMail);
        Assert.Equal("https://wa.me/31612345678", effective.WhatsAppUrl);
    }

    [Fact]
    public void Resolve_vacancy_override_can_disable_direct_contact()
    {
        var company = Company("org@example.com", "0612345678");
        company.DirectContactEnabled = true;
        company.ContactPreferPhone = true;

        var vacancy = new Vacancy
        {
            OverrideContactPreference = true,
            DirectContactEnabled = false
        };

        var effective = EmployerContactPreferenceRules.Resolve(company, vacancy);
        Assert.False(effective.Available);
    }

    [Fact]
    public void Resolve_child_off_does_not_inherit_parent_toggle()
    {
        var parent = Company("parent@example.com", "0699999999");
        parent.DirectContactEnabled = true;
        parent.ContactPreferMail = true;

        var child = Company(null, null);
        child.DirectContactEnabled = false;

        var vacancy = new Vacancy { OverrideContactPreference = false };
        var effective = EmployerContactPreferenceRules.Resolve(child, vacancy, parent);

        Assert.False(effective.Available);
    }

    [Fact]
    public void Resolve_uses_parent_contact_values_when_child_enabled()
    {
        var parent = Company("parent@example.com", "0699999999");
        var child = Company(null, null);
        child.DirectContactEnabled = true;
        child.ContactPreferMail = true;

        var vacancy = new Vacancy { OverrideContactPreference = false };
        var effective = EmployerContactPreferenceRules.Resolve(child, vacancy, parent);

        Assert.True(effective.Available);
        Assert.True(effective.OfferMail);
        Assert.Equal("parent@example.com", effective.Email);
    }

    [Fact]
    public void Resolve_hides_channel_without_value()
    {
        var company = Company(null, null);
        company.DirectContactEnabled = true;
        company.ContactPreferMail = true;
        company.ContactPreferPhone = true;

        var vacancy = new Vacancy { OverrideContactPreference = false };
        var effective = EmployerContactPreferenceRules.Resolve(company, vacancy);

        Assert.False(effective.Available);
    }

    [Fact]
    public void Resolve_hides_channel_with_non_digit_phone()
    {
        var company = Company("a@b.nl", "N/A");
        company.DirectContactEnabled = true;
        company.ContactPreferPhone = true;
        company.ContactPreferWhatsApp = true;

        var vacancy = new Vacancy { OverrideContactPreference = false };
        var effective = EmployerContactPreferenceRules.Resolve(company, vacancy);

        Assert.False(effective.Available);
    }

    private static Company Company(string? email, string? phone) => new()
    {
        Id = Guid.NewGuid(),
        Name = "Test",
        KvkNumber = "123",
        Address = "Street",
        Location = new GeoPoint(52.1, 5.1),
        ContactEmail = email,
        ContactPhone = phone
    };
}
