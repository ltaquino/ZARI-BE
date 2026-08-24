namespace ZARI.Application.Features.SystemModule.Branches.Delete;

using ZARI.Application.Abstractions.Messaging;

public sealed record DeleteBranchCommand(string Id) : ICommand;
