namespace ZARI.Application.Features.Loan.LoanApplications.Shared;

using ZARI.Application.Features.Loan.LoanApplications.GetAll;
using ZARI.Domain.Entities;

internal static class LoanApplicationMapper
{
    public static LoanApplicationResponse ToResponse(LoanApplication application) => new(
        application.Id,
        application.ApplicationNo,
        application.BranchId,
        application.CustomerId,
        application.Customer.Name,
        application.LoanProductId,
        application.LoanProduct.Code,
        application.LoanProduct.Name,
        application.ApplicationDate,
        application.RequestedPrincipal,
        application.RequestedTermMonths,
        application.Purpose,
        application.Status,
        application.Remarks,
        application.Collaterals.Select(ToCollateralResponse).ToList(),
        application.CoMakers.Select(ToCoMakerResponse).ToList(),
        application.CancelledBy,
        application.CancelledAt,
        application.CancelReason,
        application.CreatedAt,
        application.CreatedBy);

    private static LoanCollateralResponse ToCollateralResponse(LoanCollateral c) => new(
        c.Id, c.Description, c.CollateralType, c.EstimatedValue, c.DocumentRef);

    private static LoanCoMakerResponse ToCoMakerResponse(LoanCoMaker c) => new(
        c.Id, c.CoMakerCustomerId, c.CoMakerCustomer?.Name, c.Name, c.ContactNo);
}
