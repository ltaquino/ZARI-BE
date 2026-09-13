namespace ZARI.Application.Features.Hr.ThirteenthMonthPayRuns.Shared;

using ZARI.Application.Features.Hr.ThirteenthMonthPayRuns.Get;
using ZARI.Domain.Entities;

internal static class ThirteenthMonthPayRunMapper
{
    public static ThirteenthMonthPayRunResponse ToResponse(ThirteenthMonthPayRun run) => new(
        run.Id,
        run.RunNo,
        run.BranchId,
        run.Year,
        run.PayDate,
        run.Status,
        run.CreatedAt,
        run.Lines.Select(ToLineResponse).ToList());

    private static ThirteenthMonthPayLineResponse ToLineResponse(ThirteenthMonthPayLine line) => new(
        line.Id,
        line.EmployeeId,
        line.Employee.EmployeeNo,
        $"{line.Employee.FirstName} {line.Employee.LastName}",
        line.TotalBasicPay,
        line.ThirteenthMonthPay);
}
