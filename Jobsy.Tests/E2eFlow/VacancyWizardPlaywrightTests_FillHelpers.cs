using Jobsy.Tests.Werkgever;
using Microsoft.Playwright;

namespace Jobsy.Tests.E2eFlow;

internal static class VacancyWizardPlaywrightTests_FillHelpers
{
    public static Task FillStep1Async(IPage page, string title) =>
        Invoke(typeof(VacancyWizardPlaywrightTests), "FillStep1Async", page, title);

    public static Task FillStep2RichTextAsync(IPage page) =>
        Invoke(typeof(VacancyWizardPlaywrightTests), "FillStep2RichTextAsync", page);

    public static Task FillStep3Async(IPage page) =>
        Invoke(typeof(VacancyWizardPlaywrightTests), "FillStep3Async", page);

    public static Task FillStep4ScheduleAsync(IPage page, int width) =>
        Invoke(typeof(VacancyWizardPlaywrightTests), "FillStep4ScheduleAsync", page, width);

    private static Task Invoke(Type t, string name, params object?[] args)
    {
        var mi = t.GetMethod(name, System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        if (mi is null)
        {
            throw new MissingMethodException(t.Name, name);
        }

        var result = mi.Invoke(null, args);
        return result as Task ?? Task.CompletedTask;
    }
}
