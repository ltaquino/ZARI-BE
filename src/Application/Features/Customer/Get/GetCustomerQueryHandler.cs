namespace ZARI.Application.Features.Customers.Get;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class GetCustomerQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetCustomerQuery, Result<CustomerResponse>>
{
    public async Task<Result<CustomerResponse>> HandleAsync(GetCustomerQuery query, CancellationToken cancellationToken = default)
    {
        var customer = await dbContext.Customers
            .Where(c => c.Id == query.Id)
            .Select(c => new CustomerResponse(c.Id, c.Name, c.Type, c.Email, c.Phone, c.BranchId, c.Status, c.Owner, c.Address, c.Notes,
                c.ArAccountId, c.PaymentTermsDays, c.StandingDiscountPct, c.MemberNo,
                c.Tin, c.SssOrGsisNo, c.DateOfBirth, c.Sex, c.CivilStatus, c.DependentsCount, c.Employer, c.EmployerPosition,
                c.NetIncomeLastYear, c.ResidenceSince, c.PriorResidenceHistory, c.EmploymentSince, c.PriorEmploymentHistory,
                c.HousingStatus, c.OwnsVehicle, c.BankAccountInfo, c.OtherAssetsNotes, c.DataSharingConsent, c.DataSharingConsentDate,
                c.Title, c.FirstName, c.MiddleName, c.LastName, c.Suffix, c.PlaceOfBirth, c.CountryOfBirthCode, c.NationalityCode, c.Resident,
                c.AddressSubdivision, c.AddressBarangay, c.AddressCity, c.AddressProvince, c.AddressPostalCode, c.AddressCountryCode,
                c.AddressHouseOwnerOrLessee, c.AddressOccupiedSince,
                c.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (customer is null)
            return Result.Failure<CustomerResponse>(Error.NotFound("Customer.NotFound", $"Customer with ID '{query.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("CUSTOMERS", FormAction.View, customer.BranchId, cancellationToken))
            return Result.Failure<CustomerResponse>(Error.Forbidden("Customer.Forbidden", "You do not have permission to view customers for this branch."));

        return Result.Success(customer);
    }
}
