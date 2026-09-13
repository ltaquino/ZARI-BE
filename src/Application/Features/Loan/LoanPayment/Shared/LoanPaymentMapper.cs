namespace ZARI.Application.Features.Loan.LoanPayments.Shared;

using ZARI.Application.Features.Loan.LoanPayments.GetAll;
using ZARI.Domain.Entities;

internal static class LoanPaymentMapper
{
    public static LoanPaymentResponse ToResponse(LoanPayment payment) => new(
        payment.Id,
        payment.PaymentNo,
        payment.BranchId,
        payment.LoanAccountId,
        payment.LoanAccount.LoanAcctNo,
        payment.LoanAccount.Customer.Name,
        payment.LoanAccount.LoanProduct.Name,
        payment.PaymentDate,
        payment.Amount,
        payment.PaymentMethodId,
        payment.PaymentMethod.Name,
        payment.ReferenceNo,
        payment.CostCenterId,
        payment.CostCenter?.Name,
        payment.Status,
        payment.Remarks,
        payment.Allocations.Sum(a => a.PrincipalApplied),
        payment.Allocations.Sum(a => a.InterestApplied),
        payment.Allocations.Sum(a => a.PenaltyApplied),
        payment.Allocations
            .OrderBy(a => a.LoanAmortizationScheduleLine.InstallmentNo)
            .Select(a => new LoanPaymentAllocationResponse(
                a.LoanAmortizationScheduleLineId, a.LoanAmortizationScheduleLine.InstallmentNo, a.PrincipalApplied, a.InterestApplied, a.PenaltyApplied))
            .ToList(),
        payment.CancelledBy,
        payment.CancelledAt,
        payment.CancelReason,
        payment.CreatedAt,
        payment.CreatedBy);
}
