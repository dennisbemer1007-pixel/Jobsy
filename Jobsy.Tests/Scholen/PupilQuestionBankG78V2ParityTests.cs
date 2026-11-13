using Jobsy.Core.Scholen;

namespace Jobsy.Tests.Scholen;

public sealed class PupilQuestionBankG78V2ParityTests
{
    [Fact]
    public void Bank_has_60_contrast_items_9001_through_9060_with_nl_string_keys()
    {
        var bank = new PupilQuestionBankG78V2();
        var items = bank.AllItems;
        Assert.Equal(60, items.Count);
        for (var id = PupilQuestionBank.MinId; id <= PupilQuestionBank.MaxId; id++)
        {
            var item = bank.GetById(id.ToString(System.Globalization.CultureInfo.InvariantCulture));
            Assert.NotNull(item);
            Assert.True(item!.IsContrast);
            Assert.False(string.IsNullOrWhiteSpace(item.TextKey));
            Assert.False(string.IsNullOrWhiteSpace(item.TextKeyB));
            if (item.IsContrast && item.Model == Jobsy.Core.Enums.AssessmentKind.Career)
            {
                Assert.False(string.IsNullOrWhiteSpace(item.CategoryB));
            }
            Assert.True(PupilQuestionBankNlStringsG78V2.TryGet(item.TextKey, out var textA));
            Assert.True(PupilQuestionBankNlStringsG78V2.TryGet(item.TextKeyB!, out var textB));
            Assert.True(PupilQuestionBankNlStringsG78V2.TryGet(item.ImagineKey, out var stem));
            Assert.False(string.IsNullOrWhiteSpace(textA));
            Assert.False(string.IsNullOrWhiteSpace(textB));
            Assert.False(string.IsNullOrWhiteSpace(stem));
        }
    }
}
