namespace ZARI.Application.Features.Customers.Shared;

using ZARI.Application.Features.Customers.Get;
using ZARI.Domain.Entities;

/// <summary>
/// Used only where a plain POCO is in hand (e.g. right after SaveChangesAsync) — GetCustomerQueryHandler
/// and GetAllCustomersQueryHandler keep their own inline `Select(c => new CustomerResponse(...))`
/// projections instead of calling this, since EF Core needs the projection expression inline to
/// translate it to SQL rather than a method call.
/// </summary>
internal static class CustomerMapper
{
    public static CustomerResponse ToResponse(Customer customer) => new(
        customer.Id, customer.Name, customer.Type, customer.Email, customer.Phone,
        customer.BranchId, customer.Status, customer.Owner, customer.Address, customer.Notes,
        customer.ArAccountId, customer.PaymentTermsDays, customer.StandingDiscountPct, customer.MemberNo,
        customer.Tin, customer.SssOrGsisNo, customer.DateOfBirth, customer.Sex, customer.CivilStatus,
        customer.DependentsCount, customer.Employer, customer.EmployerPosition, customer.NetIncomeLastYear,
        customer.ResidenceSince, customer.PriorResidenceHistory, customer.EmploymentSince, customer.PriorEmploymentHistory,
        customer.HousingStatus, customer.OwnsVehicle, customer.BankAccountInfo, customer.OtherAssetsNotes,
        customer.DataSharingConsent, customer.DataSharingConsentDate, customer.CreatedAt);
}
