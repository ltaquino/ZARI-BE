namespace ZARI.Application.Features.Hr.Employees.Update;

using ZARI.Application.Abstractions.Messaging;

public sealed record UpdateEmployeeCommand(
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
    string? ApplicationUserId) : ICommand;
