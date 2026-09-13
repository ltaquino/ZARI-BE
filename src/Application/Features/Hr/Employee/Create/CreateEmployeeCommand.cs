namespace ZARI.Application.Features.Hr.Employees.Create;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.Employees.Get;
using ZARI.Domain.Common;

public sealed record CreateEmployeeCommand(
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
    string? SssNo,
    string? PhilHealthNo,
    string? PagIbigNo,
    string? Tin,
    decimal BasicSalary,
    string? BankAccountNo,
    string? BankName,
    string? BarcodeNo,
    string? RfidTagNo,
    string? ApplicationUserId) : ICommand<Result<EmployeeResponse>>;
