namespace ZARI.Application.Features.Hr.DailyTimeRecords.GetAll;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.DailyTimeRecords.Get;
using ZARI.Domain.Common;

public sealed record GetAllDailyTimeRecordsQuery : IQuery<Result<List<DailyTimeRecordResponse>>>;
