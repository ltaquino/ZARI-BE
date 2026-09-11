namespace ZARI.Application.Features.Loan.LoanRestructurings.Shared;

using ZARI.Application.Features.Loan.LoanRestructurings.GetAll;
using ZARI.Domain.Entities;

internal static class LoanRestructuringMapper
{
    public static LoanRestructuringResponse ToResponse(LoanRestructuring restructuring) => new(
        restructuring.Id,
        restructuring.RestructuringNo,
        restructuring.BranchId,
        restructuring.OldLoanAccountId,
        restructuring.OldLoanAccount.LoanAcctNo,
        restructuring.OldLoanAccount.Customer.Name,
        restructuring.OldLoanAccount.LoanProduct.Name,
        restructuring.NewLoanAccountId,
        restructuring.NewLoanAccount?.LoanAcctNo,
        restructuring.RestructureDate,
        restructuring.OldPrincipalBalance,
        restructuring.NewAnnualInterestRatePct,
        restructuring.NewTermMonths,
        restructuring.NewRepaymentFrequency,
        restructuring.NewGracePeriodDays,
        restructuring.NewPenaltyRatePct,
        restructuring.NewFirstDueDate,
        restructuring.Reason,
        restructuring.Status,
        restructuring.Remarks,
        restructuring.CancelledBy,
        restructuring.CancelledAt,
        restructuring.CancelReason,
        restructuring.CreatedAt,
        restructuring.CreatedBy);
}
