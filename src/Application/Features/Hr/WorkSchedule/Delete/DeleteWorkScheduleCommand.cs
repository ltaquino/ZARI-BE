namespace ZARI.Application.Features.Hr.WorkSchedules.Delete;

using ZARI.Application.Abstractions.Messaging;

public sealed record DeleteWorkScheduleCommand(Guid Id) : ICommand;
