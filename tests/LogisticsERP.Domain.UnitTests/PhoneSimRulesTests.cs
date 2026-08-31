using LogisticsERP.Domain.Enums;
using LogisticsERP.Domain.Telecom;
using Xunit;

namespace LogisticsERP.Domain.UnitTests;

public sealed class PhoneSimRulesTests
{
    [Theory]
    [InlineData("0555 123 456", "+966555123456")]
    [InlineData("555123456", "+966555123456")]
    [InlineData("00966555123456", "+966555123456")]
    [InlineData("٩٦٦ ٥٥٥ ١٢٣ ٤٥٦", "+966555123456")]
    [InlineData("+14155552671", "+14155552671")]
    public void PhoneNumbersNormalizeToCanonicalE164(string input, string expected)
    {
        Assert.True(PhoneSimRules.TryNormalizePhoneNumber(input, out var normalized));
        Assert.Equal(expected, normalized);
    }

    [Theory]
    [InlineData("")]
    [InlineData("12345")]
    [InlineData("+966455512345")]
    [InlineData("0555-ABC-456")]
    [InlineData("+0123456789")]
    public void InvalidPhoneNumbersAreRejected(string input)
    {
        Assert.False(PhoneSimRules.TryNormalizePhoneNumber(input, out _));
    }

    [Fact]
    public void IccidNormalizationAcceptsArabicDigitsAndFormatting()
    {
        Assert.Equal(
            "8996601234567890123",
            PhoneSimRules.NormalizeIccid("٨٩٩٦-٦٠١٢ ٣٤٥٦ ٧٨٩٠ ١٢٣"));
    }

    [Fact]
    public void AssignmentAndDirectStatusRulesKeepAssignedStatusDerived()
    {
        Assert.True(PhoneSimRules.CanAssign(PhoneSimStatus.Available, false));
        Assert.False(PhoneSimRules.CanAssign(PhoneSimStatus.Available, true));
        Assert.False(PhoneSimRules.CanAssign(PhoneSimStatus.Suspended, false));
        Assert.Equal(PhoneSimStatus.Assigned, PhoneSimRules.GetStatusAfterAssignment(PhoneSimStatus.Available, false));
        Assert.Equal(PhoneSimStatus.Available, PhoneSimRules.GetStatusAfterRelease(PhoneSimStatus.Assigned));
        Assert.False(PhoneSimRules.CanSetStatusDirectly(PhoneSimStatus.Assigned));
        Assert.True(PhoneSimRules.CanSetStatusDirectly(PhoneSimStatus.Lost));
    }
}
