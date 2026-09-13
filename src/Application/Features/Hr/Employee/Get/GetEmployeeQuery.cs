namespace ZARI.Application.Features.Hr.Employees.Get;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetEmployeeQuery(Guid Id) : IQuery<Result<EmployeeResponse>>;

public sealed record EmployeeResponse(
    Guid Id,
    string EmployeeNo,
    string FirstName,
    string? MiddleName,
    string LastName,
    string? Suffix,
    DateTimeOffset? DateOfBirth,
    string? Sex,
    string? CivilStatus,
    string? Address,
    string BranchId,
    Guid PositionId,
    Guid DepartmentId,
    DateTimeOffset DateHired,
    string Status,
    string EmploymentStatus,
    DateTimeOffset? RegularizationDate,
    DateTimeOffset? SeparationDate,
    string? SeparationReason,
    string? SssNo,
    string? PhilHealthNo,
    string? PagIbigNo,
    string? Tin,
    decimal BasicSalary,
    string? BankAccountNo,
    string? BankName,
    string? BarcodeNo,
    string? RfidTagNo,
    string? ApplicationUserId,
    DateTimeOffset CreatedAt);
