namespace ZARI.Application.Features.Customers.Create;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Customers.Get;
using ZARI.Domain.Common;

public sealed record CreateCustomerCommand(
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
    string? MemberNo = null,
    string? Tin = null,
    string? SssOrGsisNo = null,
    DateTimeOffset? DateOfBirth = null,
    string? Sex = null,
    string? CivilStatus = null,
    int? DependentsCount = null,
    string? Employer = null,
    string? EmployerPosition = null,
    decimal? NetIncomeLastYear = null,
    DateTimeOffset? ResidenceSince = null,
    string? PriorResidenceHistory = null,
    DateTimeOffset? EmploymentSince = null,
    string? PriorEmploymentHistory = null,
    string? HousingStatus = null,
    bool OwnsVehicle = false,
    string? BankAccountInfo = null,
    string? OtherAssetsNotes = null,
    bool DataSharingConsent = false,
    DateTimeOffset? DataSharingConsentDate = null) : ICommand<Result<CustomerResponse>>;
