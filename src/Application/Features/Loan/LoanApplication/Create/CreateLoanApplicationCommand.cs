namespace ZARI.Application.Features.Loan.LoanApplications.Create;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanApplications.GetAll;
using ZARI.Domain.Common;

public sealed record LoanCollateralInput(string Description, string CollateralType, decimal EstimatedValue, string? DocumentRef);

public sealed record LoanCoMakerInput(Guid? CoMakerCustomerId, string Name, string? ContactNo);

public sealed record CreateLoanApplicationCommand(
    string BranchId,
    Guid CustomerId,
    Guid LoanProductId,
    DateTimeOffset ApplicationDate,
    decimal RequestedPrincipal,
    int RequestedTermMonths,
    string? Purpose,
    string? Remarks,
    string? CreatedBy,
    List<LoanCollateralInput> Collaterals,
    List<LoanCoMakerInput> CoMakers) : ICommand<Result<LoanApplicationResponse>>;
