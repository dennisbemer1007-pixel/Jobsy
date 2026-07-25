namespace Jobsy.Core.Interfaces;

public interface ISalaryService
{
    bool MeetsMinimumWage(decimal hourlyWage, int ageYears);
    decimal GetMinimumHourlyWage(int ageYears);
}
