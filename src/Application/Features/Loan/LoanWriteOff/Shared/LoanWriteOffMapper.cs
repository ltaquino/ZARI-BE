namespace ZARI.Application.Features.Loan.LoanWriteOffs.Shared;

using ZARI.Application.Features.Loan.LoanWriteOffs.GetAll;
using ZARI.Domain.Entities;

internal static class LoanWriteOffMapper
{
    public static LoanWriteOffResponse ToResponse(LoanWriteOff writeOff) => new(
        writeOff.Id,
        writeOff.WriteOffNo,
        writeOff.BranchId,
        writeOff.LoanAccountId,
        writeOff.LoanAccount.LoanAcctNo,
        writeOff.LoanAccount.Customer.Name,
        writeOff.LoanAccount.LoanProduct.Name,
        writeOff.WriteOffDate,
        writeOff.Amount,
        writeOff.WriteOffExpenseAccountId,
        writeOff.WriteOffExpenseAccount.Name,
        writeOff.Reason,
        writeOff.Status,
        writeOff.Remarks,
        writeOff.CancelledBy,
        writeOff.CancelledAt,
        writeOff.CancelReason,
        writeOff.CreatedAt,
        writeOff.CreatedBy);
}
