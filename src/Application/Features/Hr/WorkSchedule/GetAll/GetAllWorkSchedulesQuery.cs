namespace ZARI.Application.Features.Hr.WorkSchedules.GetAll;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.WorkSchedules.Get;
using ZARI.Domain.Common;

public sealed record GetAllWorkSchedulesQuery : IQuery<Result<List<WorkScheduleResponse>>>;
