namespace ZARI.Application.Features.Loan.LoanAccounts.Shared;

using ZARI.Application.Features.Loan.LoanAccounts.GetAll;
using ZARI.Domain.Entities;

internal static class LoanAccountMapper
{
    public static LoanAccountResponse ToResponse(LoanAccount account) => new(
        account.Id,
        account.LoanAcctNo,
        account.BranchId,
        account.CustomerId,
        account.Customer.Name,
        account.LoanProductId,
        account.LoanProduct.Code,
        account.LoanProduct.Name,
        account.LoanApplicationId,
        account.LoanApplication?.ApplicationNo,
        account.PrincipalAmount,
        account.AnnualInterestRatePct,
        account.TermMonths,
        account.RepaymentFrequency,
        account.GracePeriodDays,
        account.PenaltyRatePct,
        account.GrantDate,
        account.FirstDueDate,
        account.Status,
        account.Remarks,
        account.LoanReceivableAccountId,
        account.InterestIncomeAccountId,
        account.PenaltyIncomeAccountId,
        account.ScheduleLines.OrderBy(s => s.InstallmentNo).Select(ToScheduleLineResponse).ToList(),
        account.CancelledBy,
        account.CancelledAt,
        account.CancelReason,
        account.IsDisputed,
        account.DisputeNotes,
        account.CreatedAt,
        account.CreatedBy);

    private static LoanAmortizationScheduleLineResponse ToScheduleLineResponse(LoanAmortizationScheduleLine s) => new(
        s.Id, s.InstallmentNo, s.DueDate, s.PrincipalDue, s.InterestDue, s.TotalDue, s.OutstandingPrincipalAfter,
        s.PrincipalPaid, s.InterestPaid, s.Status);
}
