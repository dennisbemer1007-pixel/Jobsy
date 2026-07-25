namespace Jobsy.Core.Exceptions;

public class ForbiddenCompanyAccessException : Exception
{
    public Guid CompanyId { get; }

    public ForbiddenCompanyAccessException(Guid companyId)
        : base($"Access denied for company '{companyId}'.")
    {
        CompanyId = companyId;
    }
}
