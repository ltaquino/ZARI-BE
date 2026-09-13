namespace ZARI.Application.Features.Loan.LoanDisbursements.Shared;

using ZARI.Application.Features.Loan.LoanDisbursements.GetAll;
using ZARI.Domain.Entities;

internal static class LoanDisbursementMapper
{
    public static LoanDisbursementResponse ToResponse(LoanDisbursement disbursement) => new(
        disbursement.Id,
        disbursement.DisbursementNo,
        disbursement.BranchId,
        disbursement.LoanAccountId,
        disbursement.LoanAccount.LoanAcctNo,
        disbursement.LoanAccount.Customer.Name,
        disbursement.LoanAccount.LoanProduct.Name,
        disbursement.DisbursementDate,
        disbursement.Amount,
        disbursement.PaymentMethodId,
        disbursement.PaymentMethod.Name,
        disbursement.ReferenceNo,
        disbursement.CostCenterId,
        disbursement.CostCenter?.Name,
        disbursement.Status,
        disbursement.Remarks,
        disbursement.CancelledBy,
        disbursement.CancelledAt,
        disbursement.CancelReason,
        disbursement.CreatedAt,
        disbursement.CreatedBy);
}
