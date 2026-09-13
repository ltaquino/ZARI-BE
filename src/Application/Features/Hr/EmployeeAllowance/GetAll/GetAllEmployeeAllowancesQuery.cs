namespace ZARI.Application.Features.Hr.EmployeeAllowances.GetAll;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.EmployeeAllowances.Get;
using ZARI.Domain.Common;

public sealed record GetAllEmployeeAllowancesQuery : IQuery<Result<List<EmployeeAllowanceResponse>>>;
