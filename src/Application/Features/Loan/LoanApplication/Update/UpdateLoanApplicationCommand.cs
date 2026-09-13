namespace ZARI.Application.Features.Loan.LoanApplications.Update;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanApplications.Create;
using ZARI.Application.Features.Loan.LoanApplications.GetAll;
using ZARI.Domain.Common;

public sealed record UpdateLoanApplicationCommand(
    Guid Id,
    string BranchId,
    Guid CustomerId,
    Guid LoanProductId,
    DateTimeOffset ApplicationDate,
    decimal RequestedPrincipal,
    int RequestedTermMonths,
    string? Purpose,
    string? Remarks,
    string? UpdatedBy,
    List<LoanCollateralInput> Collaterals,
    List<LoanCoMakerInput> CoMakers) : ICommand<Result<LoanApplicationResponse>>;
