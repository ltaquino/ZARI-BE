namespace ZARI.Application.Features.Loan.LoanApplications.GetAll;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetAllLoanApplicationsQuery : IQuery<Result<List<LoanApplicationResponse>>>;

public sealed record LoanCollateralResponse(
    Guid Id,
    string Description,
    string CollateralType,
    decimal EstimatedValue,
    string? DocumentRef);

public sealed record LoanCoMakerResponse(
    Guid Id,
    Guid? CoMakerCustomerId,
    string? CoMakerCustomerName,
    string Name,
    string? ContactNo);

public sealed record LoanApplicationResponse(
    Guid Id,
    string ApplicationNo,
    string BranchId,
    Guid CustomerId,
    string CustomerName,
    Guid LoanProductId,
    string LoanProductCode,
    string LoanProductName,
    DateTimeOffset ApplicationDate,
    decimal RequestedPrincipal,
    int RequestedTermMonths,
    string? Purpose,
    string Status,
    string? Remarks,
    List<LoanCollateralResponse> Collaterals,
    List<LoanCoMakerResponse> CoMakers,
    string? CancelledBy,
    DateTimeOffset? CancelledAt,
    string? CancelReason,
    DateTimeOffset CreatedAt,
    string? CreatedBy);
