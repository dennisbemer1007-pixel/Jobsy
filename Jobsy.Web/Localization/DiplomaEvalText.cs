using Jobsy.Core.Rules;

namespace Jobsy.Web.Localization;

public static class DiplomaEvalText
{
    public static string Attribution(CultureState culture, string? issuingBody, string? other)
    {
        if (issuingBody is DiplomaEvaluationRules.BodyNuffic or DiplomaEvaluationRules.BodySbb)
        {
            return culture["DiplomaEval.Attribution.Official"];
        }

        var name = string.IsNullOrWhiteSpace(other) ? culture["DiplomaEval.Body.Other"] : other.Trim();
        return culture.Format("DiplomaEval.Attribution.Other", name);
    }

    public static string Level(CultureState culture, string? code)
        => string.IsNullOrWhiteSpace(code) ? "" : culture["DiplomaEval.Level." + code];
}
