namespace ZARI.Application.Features.Loan.LoanPayments.GetAll;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetAllLoanPaymentsQuery : IQuery<Result<List<LoanPaymentResponse>>>;

public sealed record LoanPaymentAllocationResponse(
    Guid LoanAmortizationScheduleLineId,
    int InstallmentNo,
    decimal PrincipalApplied,
    decimal InterestApplied,
    decimal PenaltyApplied);

public sealed record LoanPaymentResponse(
    Guid Id,
    string PaymentNo,
    string BranchId,
    Guid LoanAccountId,
    string LoanAccountNo,
    string CustomerName,
    string LoanProductName,
    DateTimeOffset PaymentDate,
    decimal Amount,
    Guid PaymentMethodId,
    string PaymentMethodName,
    string? ReferenceNo,
    Guid? CostCenterId,
    string? CostCenterName,
    string Status,
    string? Remarks,
    decimal PrincipalApplied,
    decimal InterestApplied,
    decimal PenaltyApplied,
    List<LoanPaymentAllocationResponse> Allocations,
    string? CancelledBy,
    DateTimeOffset? CancelledAt,
    string? CancelReason,
    DateTimeOffset CreatedAt,
    string? CreatedBy);
