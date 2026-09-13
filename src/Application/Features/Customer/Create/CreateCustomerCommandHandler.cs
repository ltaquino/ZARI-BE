namespace ZARI.Application.Features.Customers.Create;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Customers.Get;
using ZARI.Application.Features.Customers.Shared;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class CreateCustomerCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<CreateCustomerCommand, Result<CustomerResponse>>
{
    public async Task<Result<CustomerResponse>> HandleAsync(CreateCustomerCommand command, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionOnBranchAsync("CUSTOMERS", FormAction.Create, command.BranchId, cancellationToken))
            return Result.Failure<CustomerResponse>(Error.Forbidden("Customer.Forbidden", "You do not have permission to create customers for this branch."));

        var branchExists = await dbContext.Branches.AnyAsync(b => b.Id == command.BranchId, cancellationToken);
        if (!branchExists)
            return Result.Failure<CustomerResponse>(Error.NotFound("Branch.NotFound", $"Branch with ID '{command.BranchId}' was not found."));

        if (command.ArAccountId is not null)
        {
            var glAccountExists = await dbContext.GlAccounts.AnyAsync(a => a.Id == command.ArAccountId, cancellationToken);
            if (!glAccountExists)
                return Result.Failure<CustomerResponse>(Error.NotFound("GlAccount.NotFound", $"GL account with ID '{command.ArAccountId}' was not found."));
        }

        var customer = new Customer
        {
            Name = command.Name,
            Type = command.Type,
            Email = command.Email,
            Phone = command.Phone,
            BranchId = command.BranchId,
            Status = command.Status,
            Owner = command.Owner,
            Address = command.Address,
            Notes = command.Notes,
            ArAccountId = command.ArAccountId,
            PaymentTermsDays = command.PaymentTermsDays,
            StandingDiscountPct = command.StandingDiscountPct,
            MemberNo = command.MemberNo,
            Tin = command.Tin,
            SssOrGsisNo = command.SssOrGsisNo,
            DateOfBirth = command.DateOfBirth,
            Sex = command.Sex,
            CivilStatus = command.CivilStatus,
            DependentsCount = command.DependentsCount,
            Employer = command.Employer,
            EmployerPosition = command.EmployerPosition,
            NetIncomeLastYear = command.NetIncomeLastYear,
            ResidenceSince = command.ResidenceSince,
            PriorResidenceHistory = command.PriorResidenceHistory,
            EmploymentSince = command.EmploymentSince,
            PriorEmploymentHistory = command.PriorEmploymentHistory,
            HousingStatus = command.HousingStatus,
            OwnsVehicle = command.OwnsVehicle,
            BankAccountInfo = command.BankAccountInfo,
            OtherAssetsNotes = command.OtherAssetsNotes,
            DataSharingConsent = command.DataSharingConsent,
            DataSharingConsentDate = command.DataSharingConsent ? command.DataSharingConsentDate ?? DateTimeOffset.UtcNow : null,
            Title = command.Title,
            FirstName = command.FirstName,
            MiddleName = command.MiddleName,
            LastName = command.LastName,
            Suffix = command.Suffix,
            PlaceOfBirth = command.PlaceOfBirth,
            CountryOfBirthCode = command.CountryOfBirthCode,
            NationalityCode = command.NationalityCode,
            Resident = command.Resident,
            AddressSubdivision = command.AddressSubdivision,
            AddressBarangay = command.AddressBarangay,
            AddressCity = command.AddressCity,
            AddressProvince = command.AddressProvince,
            AddressPostalCode = command.AddressPostalCode,
            AddressCountryCode = command.AddressCountryCode,
            AddressHouseOwnerOrLessee = command.AddressHouseOwnerOrLessee,
            AddressOccupiedSince = command.AddressOccupiedSince
        };

        dbContext.Customers.Add(customer);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(CustomerMapper.ToResponse(customer));
    }
}
