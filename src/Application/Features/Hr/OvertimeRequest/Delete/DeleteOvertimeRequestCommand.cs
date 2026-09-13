namespace ZARI.Application.Features.Hr.OvertimeRequests.Delete;

using ZARI.Application.Abstractions.Messaging;

public sealed record DeleteOvertimeRequestCommand(Guid Id) : ICommand;
