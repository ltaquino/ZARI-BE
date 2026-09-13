namespace ZARI.Application.Features.Hr.Departments.GetAll;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.Departments.Get;
using ZARI.Domain.Common;

public sealed record GetAllDepartmentsQuery : IQuery<Result<List<DepartmentResponse>>>;
