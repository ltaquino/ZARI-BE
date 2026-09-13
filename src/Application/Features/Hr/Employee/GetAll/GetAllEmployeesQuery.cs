namespace ZARI.Application.Features.Hr.Employees.GetAll;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.Employees.Get;
using ZARI.Domain.Common;

public sealed record GetAllEmployeesQuery : IQuery<Result<List<EmployeeResponse>>>;
