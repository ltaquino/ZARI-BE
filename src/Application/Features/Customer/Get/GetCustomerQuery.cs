namespace ZARI.Application.Features.Customers.Get;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetCustomerQuery(Guid Id) : IQuery<Result<CustomerResponse>>;

public sealed record CustomerResponse(
    Guid Id,
    string Name,
    string Type,
    string Email,
    string Phone,
    string BranchId,
    string Status,
    string Owner,
    string Address,
    string? Notes,
    Guid? ArAccountId,
    int? PaymentTermsDays,
    decimal? StandingDiscountPct,
    string? MemberNo,
    string? Tin,
    string? SssOrGsisNo,
    DateTimeOffset? DateOfBirth,
    string? Sex,
    string? CivilStatus,
    int? DependentsCount,
    string? Employer,
    string? EmployerPosition,
    decimal? NetIncomeLastYear,
    DateTimeOffset? ResidenceSince,
    string? PriorResidenceHistory,
    DateTimeOffset? EmploymentSince,
    string? PriorEmploymentHistory,
    string? HousingStatus,
    bool OwnsVehicle,
    string? BankAccountInfo,
    string? OtherAssetsNotes,
    bool DataSharingConsent,
    DateTimeOffset? DataSharingConsentDate,
    DateTimeOffset CreatedAt);
