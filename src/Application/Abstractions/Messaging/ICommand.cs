namespace ZARI.Application.Abstractions.Messaging;

using ZARI.Domain.Common;

public interface ICommand : ICommand<Result>;

public interface ICommand<TResponse>;
