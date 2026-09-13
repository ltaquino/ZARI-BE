namespace ZARI.Application.Features.Hr.EmployeeDeductions.GetAll;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.EmployeeDeductions.Get;
using ZARI.Domain.Common;

public sealed record GetAllEmployeeDeductionsQuery : IQuery<Result<List<EmployeeDeductionResponse>>>;
